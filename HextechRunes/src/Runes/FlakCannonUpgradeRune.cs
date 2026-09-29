using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

public sealed class FlakCannonUpgradeRune : CardUpgradeRuneBase<FlakCannon>
{
	protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. base.ExtraHoverTips,
		HoverTipFactory.FromCard<Fuel>()
	];

	internal static CardModel[] GetStatuses(PlayerCombatState playerCombatState) => playerCombatState.AllCards
		.Where(card => card.Type == CardType.Status && card.Pile?.Type != PileType.Exhaust).ToArray();

	internal static async Task PlayUpgraded(PlayerChoiceContext context, FlakCannon card, CardPlay play)
	{
		if (card.CombatState is not { } combatState || card.Owner.PlayerCombatState is not { } playerCombatState)
		{
			return;
		}

		CardModel[] statuses = GetStatuses(playerCombatState);
		int hits = (int)((CalculatedVar)card.DynamicVars["CalculatedHits"]).Calculate(play.Target);
		// 命中数仍由出牌时的状态牌总数决定；不可变化的状态牌遵守原版变化限制并留在原处。
		foreach (CardModel status in statuses.Where(status => status.IsTransformable))
		{
			CardModel fuel = combatState.CreateCard<Fuel>(card.Owner);
			await CardCmd.Transform([new CardTransformation(status, fuel)], null, CardPreviewStyle.None);
			await CardPileCmd.Add(fuel, PileType.Discard, CardPilePosition.Bottom);
		}

		await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue).WithHitCount(hits)
			.FromCardCompat(card, play).TargetingRandomOpponents(combatState)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3").Execute(context);
	}

	[HarmonyPatch(typeof(FlakCannon), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.flak-cannon.fuel", "升级散射炮", Rune = typeof(FlakCannonUpgradeRune))]
	private static class FlakCannonFuelPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(FlakCannon __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (__instance.Owner?.GetRelic<FlakCannonUpgradeRune>() == null)
			{
				return true;
			}

			__result = PlayUpgraded(choiceContext, __instance, cardPlay);
			return false;
		}
	}
}
