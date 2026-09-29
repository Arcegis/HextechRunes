using MegaCrit.Sts2.Core.Hooks;

namespace HextechRunes;

internal readonly record struct HextechCardPlayResourceSpend(decimal Energy, decimal Stars);

internal static partial class HextechCombatHooks
{
	internal static bool TryGetActivePlayEnergyValue(CardModel? card, out decimal energyValue)
	{
		energyValue = 0m;
		if (card == null)
		{
			return false;
		}

		if (!ActivePlayEnergyValues.TryGetValue(card, out Stack<int>? energyValues) || energyValues.Count == 0)
		{
			return false;
		}

		energyValue = energyValues.Peek();
		return true;
	}

	internal static decimal GetEnergyCostForCurrentCardPlay(CardModel card)
	{
		return TryGetActivePlayEnergyValue(card, out decimal energyValue)
			? energyValue
			: card.EnergyCost.GetAmountToSpend();
	}

	// 本次出牌实付的能量与辉星，取自随出牌动作同步的 CardPlay.Resources：两端执行同一动作，值必然一致。
	// 不能读本机静态记账栈：栈缺值时只能退回牌面费用，自动打出（实付 0）的牌就会在一端多退能量，
	// 联机校验和分叉（一呼百应连打 + 最万用的瞄准镜返还）。星尘保留的辉星上报为已花费但并未扣除，不退。
	internal static HextechCardPlayResourceSpend GetResourceSpend(CardPlay cardPlay)
	{
		ResourceInfo resources = cardPlay.Resources;
		int stars = StardustUpgradeRune.ShouldPreserveStars(cardPlay.Card) ? 0 : resources.StarsSpent;
		return new HextechCardPlayResourceSpend(Math.Max(0, resources.EnergySpent), Math.Max(0, stars));
	}

	// 逐步对照原版 CardModel.SpendResources/SpendEnergy（0.111.0），只把"扣辉星"换成保留辉星并闪烁星尘符文。
	// 原版同样假定出牌时 Owner/CombatState/PlayerCombatState 都存在，缺失即是调用方契约错误。
	private static async Task<ValueTuple<int, int>> SpendResourcesPreservingStars(CardModel card)
	{
		Player owner = card.Owner ?? throw new InvalidOperationException($"{card.Id.Entry} has no owner while spending resources.");
		HextechCombatState combatState = card.CombatState ?? throw new InvalidOperationException($"{card.Id.Entry} is not in combat while spending resources.");
		PlayerCombatState playerCombatState = owner.PlayerCombatState ?? throw new InvalidOperationException($"{card.Id.Entry} owner has no combat state while spending resources.");
		int energy = playerCombatState.Energy;
		int energyToSpend = card.EnergyCost.GetAmountToSpend();
		int starsToSpend = Math.Max(0, card.GetStarCostWithModifiers());
		if (energyToSpend > energy && Hook.ShouldPayExcessEnergyCostWithStars(combatState, owner))
		{
			starsToSpend += (energyToSpend - energy) * 2;
			energyToSpend = energy;
		}

		if (card.EnergyCost.CostsX)
		{
			card.EnergyCost.CapturedXValue = energyToSpend;
		}

		if (energyToSpend > 0)
		{
			CombatManager.Instance.History.EnergySpent(combatState, energyToSpend, owner);
			playerCombatState.LoseEnergy(Math.Max(0, energyToSpend));
		}

		await Hook.AfterEnergySpent(combatState, card, energyToSpend);
		card.LastStarsSpent = starsToSpend;
		owner.GetRelic<StardustUpgradeRune>()?.Flash();
		return new ValueTuple<int, int>(energyToSpend, starsToSpend);
	}

	private static void PushActivePlayEnergyValue(CardModel card, int energyValue)
	{
		if (!ActivePlayEnergyValues.TryGetValue(card, out Stack<int>? energyValues))
		{
			energyValues = new Stack<int>();
			ActivePlayEnergyValues[card] = energyValues;
		}

		energyValues.Push(Math.Max(0, energyValue));
	}

	private static async Task PopActivePlayEnergyValueWhenDone(CardModel card, PlayerChoiceContext choiceContext, Task task)
	{
		try
		{
			await task;
			await EnsureTransformingSkillLeavesPlayPileInMultiplayer(card, choiceContext);
		}
		finally
		{
			PopActivePlayEnergyValue(card);
		}
	}

	private static Task EnsureTransformingSkillLeavesPlayPileInMultiplayer(CardModel card, PlayerChoiceContext choiceContext)
	{
		if (!HextechRelicBase.IsNetworkMultiplayerRun()
			|| !IsCleanupSensitiveTransformingSkill(card)
			|| card.Owner?.Creature.IsDead == true
			|| card.Pile?.Type != PileType.Play)
		{
			return Task.CompletedTask;
		}

		if (ShouldForceExhaustStuckPlayCard(card))
		{
			return CardCmd.Exhaust(choiceContext, card, false, true);
		}

		return CardPileCmd.Add(card, PileType.Discard);
	}

	private static bool IsCleanupSensitiveTransformingSkill(CardModel card)
	{
		return card is Begone
			or Charge
			or Compact
			or Guards
			or PrimalForce
			or Seance;
	}

	private static bool ShouldForceExhaustStuckPlayCard(CardModel card)
	{
		return card.ExhaustOnNextPlay
			|| card.Keywords.Contains(CardKeyword.Exhaust)
			|| card.Owner?.GetRelic<EightPennyGateRune>() != null;
	}

	private static void PopActivePlayEnergyValue(CardModel card)
	{
		if (!ActivePlayEnergyValues.TryGetValue(card, out Stack<int>? energyValues) || energyValues.Count == 0)
		{
			return;
		}

		energyValues.Pop();
		if (energyValues.Count == 0)
		{
			ActivePlayEnergyValues.Remove(card);
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources), new Type[0])]
	[HextechPatch("combat.spend-resources", "出牌费用记账")]
	private static class SpendResourcesPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(CardModel __instance, ref Task<ValueTuple<int, int>> __result)
		{
			PendingManualPlayEnergyValues[__instance] = __instance.EnergyCost.GetAmountToSpend();
			if (!StardustUpgradeRune.ShouldPreserveStars(__instance))
			{
				return true;
			}

			__result = SpendResourcesPreservingStars(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper), typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool))]
	[HextechPatch("combat.on-play-wrapper", "出牌费用记账")]
	private static class OnPlayWrapperPatch
	{
		[HarmonyPrefix]
		private static void Prefix(CardModel __instance, ResourceInfo resources)
		{
			int energyValue = resources.EnergyValue;
			if (PendingManualPlayEnergyValues.Remove(__instance, out int pendingEnergyValue))
			{
				energyValue = pendingEnergyValue;
			}

			PushActivePlayEnergyValue(__instance, energyValue);
		}

		[HarmonyPostfix]
		private static void Postfix(CardModel __instance, PlayerChoiceContext choiceContext, ref Task __result)
		{
			__result = PopActivePlayEnergyValueWhenDone(__instance, choiceContext, __result);
		}
	}
}
