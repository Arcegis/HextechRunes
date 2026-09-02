using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HextechRunes;

internal static partial class HextechPlayerRuneHooks
{


	private static async Task JuggernautUpgradeAfterBlockGained(JuggernautPower power, Creature creature, decimal amount)
	{
		if (amount <= 0m || creature != power.Owner)
		{
			return;
		}

		List<Creature> targets = power.CombatState.HittableEnemies.ToList();
		if (targets.Count == 0)
		{
			return;
		}

		power.Owner.Player?.GetRelic<JuggernautUpgradeRune>()?.Flash(targets);
		await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), targets, power.Amount, ValueProp.Unpowered, power.Owner);
	}


	// 形参按游戏真实签名用 HextechCombatState(0.104+ 为 ICombatState);helper 需要具体 CombatState,
	// 拿不到时放行原版(与旧行为一致,不吞小刀)。


	private static async Task PlayFanOfKnivesSovereignBlade(PlayerChoiceContext choiceContext, SovereignBlade card)
	{
		if (card.CombatState is not CombatState combatState)
		{
			return;
		}

		var attack = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
			.FromCardCompat(card)
			.WithHitCount(card.DynamicVars.Repeat.IntValue)
			.WithAttackerAnim("Cast", card.Owner.Character.AttackAnimDelay)
			.WithAttackerFx(null, "event:/sfx/characters/regent/regent_sovereign_blade")
			.TargetingAllOpponents(combatState)
			.WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3");

		await attack.Execute(choiceContext);
	}


	private static List<CardModel>? TryApplyEnemyManipulateRealityStatusDoubling(IReadOnlyList<CardModel> cards, bool addedByPlayer)
	{
		if (addedByPlayer)
		{
			return null;
		}

		List<CardModel>? rewritten = null;
		for (int i = 0; i < cards.Count; i++)
		{
			CardModel card = cards[i];
			if (!ShouldDoubleEnemyGeneratedStatusCard(card))
			{
				rewritten?.Add(card);
				continue;
			}

			rewritten ??= cards.Take(i).ToList();
			rewritten.Add(card);
			if (TryCreateManipulateRealityStatusCopy(card, out CardModel copy))
			{
				rewritten.Add(copy);
			}
		}

		return rewritten;
	}

	private static bool ShouldDoubleEnemyGeneratedStatusCard(CardModel card)
	{
		return card.Type == CardType.Status
			&& card.Owner?.Creature.Side == CombatSide.Player
			&& card.Owner.Creature.CombatState?.RunState == card.Owner.RunState
			&& card.Owner.RunState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault()?.HasActiveMonsterHex(MonsterHexKind.ManipulateReality) == true;
	}

	private static bool TryCreateManipulateRealityStatusCopy(CardModel card, out CardModel copy)
	{
		copy = null!;
		try
		{
			if (card.Owner?.Creature.CombatState is not HextechCombatState combatState)
			{
				return false;
			}

			copy = combatState.CloneCard(card);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Failed to duplicate enemy generated status card for Manipulate Reality: card={card.Id.Entry} error={ex.GetType().Name}: {ex.Message}");
			return false;
		}
	}


	private static Player? TryGetMutableCardOwner(CardModel card)
	{
		try
		{
			return card.Owner;
		}
		catch (CanonicalModelException)
		{
			return null;
		}
	}

	[HarmonyPatch(typeof(CreativeAiPower), nameof(CreativeAiPower.BeforeHandDraw), typeof(Player), typeof(PlayerChoiceContext), typeof(HextechCombatState))]
	[HextechPatch("rune.creative-ai", "升级创意AI", Rune = typeof(CreativeAiUpgradeRune))]
	private static class CreativeAiPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(CreativeAiPower __instance, Player player, ref Task __result)
		{
			if (!CreativeAiUpgradeRune.ShouldUseUpgradedGeneration(__instance, player))
			{
				return true;
			}

			__result = CreativeAiUpgradeRune.GenerateUpgradedPowerCards(__instance, player);
			return false;
		}
	}

	[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicDescription), MethodType.Getter)]
	[HextechPatch("rune.flying-kick.description", "飞踢", Rune = typeof(FlyingKickRune))]
	private static class FlyingKickDescriptionPatch
	{
		[HarmonyPrefix]
		private static void Prefix(RelicModel __instance)
		{
			if (__instance is FlyingKickRune flyingKickRune)
			{
				flyingKickRune.RefreshExecutePercentFromOwner();
			}
		}
	}

	[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim), typeof(bool))]
	[HextechPatch("rune.flying-kick.corpse-launch", "飞踢尸体击飞视觉")]
	private static class FlyingKickCorpseLaunchPatch
	{
		[HarmonyPrepare]
		private static bool Prepare()
		{
			if (HextechRuntimeRuneCompatibility.IsAndroidRuntime)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem][Compat] Flying Kick corpse launch visual hook skipped on Android runtime.");
				return false;
			}

			return true;
		}

		[HarmonyPostfix]
		private static void Postfix(NCreature __instance, bool shouldRemove)
		{
			if (!FlyingKickCorpseLaunchDriver.TryConsumePending(__instance.Entity))
			{
				return;
			}

			if (!shouldRemove
				|| __instance.Entity == null
				|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(__instance.Entity))
			{
				return;
			}

			FlyingKickCorpseLaunchDriver.TryAttach(__instance);
		}
	}

	[HarmonyPatch(typeof(Survivor), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.survivor", "升级幸存者", Rune = typeof(SurvivorUpgradeRune))]
	private static class SurvivorPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Survivor __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (!SurvivorUpgradeRune.ShouldUseUpgradedPlay(__instance))
			{
				return true;
			}

			__result = SurvivorUpgradeRune.PlayUpgraded(choiceContext, __instance, cardPlay);
			return false;
		}
	}

	[HarmonyPatch(typeof(Compact), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.compact", "升级压缩", Rune = typeof(CompactUpgradeRune))]
	private static class CompactPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Compact __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (!CompactUpgradeRune.ShouldUseUpgradedPlay(__instance))
			{
				return true;
			}

			__result = CompactUpgradeRune.PlayUpgraded(choiceContext, __instance, cardPlay);
			return false;
		}
	}

	[HarmonyPatch(typeof(JuggernautPower), nameof(JuggernautPower.AfterBlockGained), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel))]
	[HextechPatch("rune.juggernaut", "升级主宰", Rune = typeof(JuggernautUpgradeRune))]
	private static class JuggernautPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(JuggernautPower __instance, Creature creature, decimal amount, ValueProp props, CardModel? cardSource, ref Task __result)
		{
			if (__instance.Owner?.Player?.GetRelic<JuggernautUpgradeRune>() == null)
			{
				return true;
			}

			__result = JuggernautUpgradeAfterBlockGained(__instance, creature, amount);
			return false;
		}
	}

	[HarmonyPatch(typeof(HiddenGem), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.hidden-gem", "升级未掘宝石", Rune = typeof(HiddenGemUpgradeRune))]
	private static class HiddenGemPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(HiddenGem __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (!HiddenGemUpgradeRune.ShouldUseUpgradedPlay(__instance))
			{
				return true;
			}

			__result = HiddenGemUpgradeRune.PlayUpgraded(choiceContext, __instance, cardPlay);
			return false;
		}
	}

	[HarmonyPatch(typeof(AutomationPower), nameof(AutomationPower.AfterCardDrawn), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool))]
	[HextechPatch("rune.automation", "升级自动化", Rune = typeof(AutomationUpgradeRune))]
	private static class AutomationPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(AutomationPower __instance, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw, ref Task __result)
		{
			if (!AutomationUpgradeRune.ShouldUseUpgradedDraw(__instance, card))
			{
				return true;
			}

			__result = AutomationUpgradeRune.AfterCardDrawnUpgraded(choiceContext, __instance, card, fromHandDraw);
			return false;
		}
	}

	[HarmonyPatch(typeof(Jackpot), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.jackpot", "升级大奖", Rune = typeof(JackpotUpgradeRune))]
	private static class JackpotPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Jackpot __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (!JackpotUpgradeRune.ShouldUseUpgradedPlay(__instance))
			{
				return true;
			}

			__result = JackpotUpgradeRune.OnPlayUpgraded(__instance, choiceContext, cardPlay);
			return false;
		}
	}

	[HarmonyPatch(typeof(Voltaic), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.voltaic", "升级伏特", Rune = typeof(VoltaicUpgradeRune))]
	private static class VoltaicPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Voltaic __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (!VoltaicUpgradeRune.ShouldUseUpgradedPlay(__instance))
			{
				return true;
			}

			__result = VoltaicUpgradeRune.PlayUpgraded(choiceContext, __instance, cardPlay);
			return false;
		}
	}

	[HarmonyPatch(typeof(GrandFinale), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	// 升级压轴无视"抽牌堆为空"的出牌条件:只补这张牌自己的 IsPlayable,不碰全局 CanPlay。
	[HarmonyPatch(typeof(GrandFinale), "IsPlayable", MethodType.Getter)]
	[HextechPatch("rune.grand-finale.playable", "升级压轴", Rune = typeof(GrandFinaleUpgradeRune))]
	private static class GrandFinalePlayablePatch
	{
		[HarmonyPostfix]
		private static void Postfix(GrandFinale __instance, ref bool __result)
		{
			if (!__result && GrandFinaleUpgradeRune.AllowsPlaying(__instance))
			{
				__result = true;
			}
		}
	}

	[HextechPatch("rune.grand-finale", "升级压轴", Rune = typeof(GrandFinaleUpgradeRune))]
	private static class GrandFinalePatch
	{
		[HarmonyPrefix]
		private static bool Prefix(GrandFinale __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (!GrandFinaleUpgradeRune.AllowsPlaying(__instance))
			{
				return true;
			}

			__result = GrandFinaleUpgradeRune.PlayUpgradedSafely(choiceContext, __instance);
			return false;
		}
	}

	#if STS2_109_OR_NEWER
	[HarmonyPatch(typeof(Shiv), nameof(Shiv.CreateInHand), typeof(Player), typeof(HextechCombatState), typeof(Player))]
	#else
	[HarmonyPatch(typeof(Shiv), nameof(Shiv.CreateInHand), typeof(Player), typeof(HextechCombatState))]
	#endif
	[HextechPatch("rune.big-knife.shiv-one", "大刀", Rune = typeof(BigKnifeRune))]
	private static class ShivCreateOnePatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Player owner, HextechCombatState combatState, ref Task<CardModel?> __result)
		{
			if (owner.GetRelic<BigKnifeRune>() == null || combatState is not CombatState concreteState)
			{
				return true;
			}

			__result = HextechKnifeHelper.CreateOneBigKnifeBladeInHand(owner, concreteState);
			return false;
		}
	}

	#if STS2_109_OR_NEWER
	[HarmonyPatch(typeof(Shiv), nameof(Shiv.CreateInHand), typeof(Player), typeof(int), typeof(HextechCombatState), typeof(Player))]
	#else
	[HarmonyPatch(typeof(Shiv), nameof(Shiv.CreateInHand), typeof(Player), typeof(int), typeof(HextechCombatState))]
	#endif
	[HextechPatch("rune.big-knife.shiv-many", "大刀", Rune = typeof(BigKnifeRune))]
	private static class ShivCreateManyPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(Player owner, int count, HextechCombatState combatState, ref Task<IEnumerable<CardModel>> __result)
		{
			if (owner.GetRelic<BigKnifeRune>() == null || combatState is not CombatState concreteState)
			{
				return true;
			}

			__result = HextechKnifeHelper.CreateBigKnifeBladesInHand(owner, count, concreteState);
			return false;
		}
	}

	[HarmonyPatch(typeof(SovereignBlade), nameof(SovereignBlade.TargetType), MethodType.Getter)]
	[HextechPatch("rune.big-knife.sovereign-blade-target", "大刀", Rune = typeof(BigKnifeRune))]
	private static class SovereignBladeTargetTypePatch
	{
		[HarmonyPostfix]
		private static void Postfix(SovereignBlade __instance, ref TargetType __result)
		{
			if (HextechKnifeHelper.ShouldFanOfKnivesAffectSovereignBlade(__instance))
			{
				__result = TargetType.AllEnemies;
			}
		}
	}

	[HarmonyPatch(typeof(SovereignBlade), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.big-knife.sovereign-blade-play", "大刀", Rune = typeof(BigKnifeRune))]
	private static class SovereignBladeOnPlayPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(SovereignBlade __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (!HextechKnifeHelper.ShouldFanOfKnivesAffectSovereignBlade(__instance) || __instance.CombatState is not CombatState)
			{
				return true;
			}

			__result = PlayFanOfKnivesSovereignBlade(choiceContext, __instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat), typeof(IEnumerable<CardModel>), typeof(PileType), typeof(Player), typeof(CardPilePosition))]
	[HextechPatch("rune.big-knife.generated-cards", "大刀", Rune = typeof(BigKnifeRune))]
	private static class GeneratedCardsPatch
	{
		[HarmonyPrefix]
		private static void Prefix(ref IEnumerable<CardModel> cards, Player? creator)
		{
			// 整体兜底:本 prefix 在"敌人塞状态牌/生成卡进战斗"的必经路径上,任何异常都会让
			// 整个 AddGeneratedCardsToCombat 调用中断、上层塞牌任务链卡死(游戏卡住)。
			// 枚举外部传入的 cards(可能已被其他模组的 hook 改写为脆弱的惰性序列)是主要风险点;
			// 出错时放行原始参数、放弃本次改写(大刀替换/操控现实翻倍),绝不让塞牌流程断掉。
			try
			{
				List<CardModel> originals = cards.ToList();
				if (originals.Count == 0)
				{
					return;
				}

				bool addedByPlayer = creator != null;
				List<CardModel>? rewritten = null;
				for (int i = 0; i < originals.Count; i++)
				{
					CardModel card = originals[i];
					if (!HextechKnifeHelper.TryCreateBigKnifeReplacement(card, out CardModel replacement))
					{
						rewritten?.Add(card);
						continue;
					}

					if (rewritten == null)
					{
						rewritten = originals.Take(i).ToList();
					}
					rewritten.Add(replacement);
				}

				List<CardModel>? realityRewritten = TryApplyEnemyManipulateRealityStatusDoubling(rewritten ?? originals, addedByPlayer);
				if (realityRewritten != null)
				{
					cards = realityRewritten;
				}
				else if (rewritten != null)
				{
					cards = rewritten;
				}
			}
			catch (Exception ex)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] AddGeneratedCardsToCombat prefix failed; passing cards through unmodified: {ex.GetType().Name}: {ex.Message}");
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.ResolveEnergyXValue), new Type[0])]
	[HextechPatch("rune.whirlwind", "升级旋风斩", Rune = typeof(WhirlwindUpgradeRune))]
	private static class WhirlwindXValuePatch
	{
		[HarmonyPostfix]
		private static void Postfix(CardModel __instance, ref int __result)
		{
			WhirlwindUpgradeRune.TryDoubleResolvedX(__instance, ref __result);
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.Tags), MethodType.Getter)]
	[HextechPatch("rune.card-tags", "卡牌标签", Runes = [typeof(DeviantCognitionRune), typeof(BigKnifeRune)])]
	private static class CardTagsPatch
	{
		[HarmonyPostfix]
		private static void Postfix(CardModel __instance, ref IEnumerable<CardTag> __result)
		{
			Player? owner = TryGetMutableCardOwner(__instance);
			if (!__result.Contains(CardTag.Shiv) && HextechKnifeHelper.ShouldTreatSovereignBladeAsShiv(__instance, owner))
			{
				__result = __result.Append(CardTag.Shiv);
			}

			if (__result.Contains(CardTag.Strike)
				|| owner?.GetRelic<DeviantCognitionRune>() == null
				|| !IllusoryWeaponRune.IsAttackForEffects(__instance, owner))
			{
				return;
			}

			__result = __result.Append(CardTag.Strike);
		}
	}
}
