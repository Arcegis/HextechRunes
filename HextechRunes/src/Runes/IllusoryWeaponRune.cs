using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class IllusoryWeaponRune : HextechRelicBase
{
	// 单机稳定随机的本地序号（见 ConsumeCombatProcOrdinal）：不在战斗开始清零，跨战斗累加、读档归零；
	// 联机改用 Mayhem 的每场计数。改成每场清零会改变单机的随机结果，按现状保留。
	private int _localDamageTargetOrdinal;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(2m, ValueProp.Move)
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !HextechCardEffectTypes.IsOriginalOwnedSkill(cardPlay.Card, Owner)
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		try
		{
			int targetOrdinal = ConsumeCombatProcOrdinal(nameof(IllusoryWeaponRune), ref _localDamageTargetOrdinal);
			Creature? target = HextechRuneTargeting.PickRandomHittableEnemy(
				Owner,
				combatState,
				"illusory-weapon",
				combatState.RoundNumber.ToString(),
				targetOrdinal.ToString(),
				cardPlay.Card.Id.Entry);
			if (target == null)
			{
				return;
			}

			Flash([target]);
			await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
				.FromCardCompat(cardPlay.Card)
				.Targeting(target)
				.WithNoAttackerAnim()
				.Execute(choiceContext);
		}
		finally
		{
			HextechPlayerRuneHooks.ClearIllusoryWeaponPendingPenNib(Owner, cardPlay.Card);
		}
	}

	// 分类逻辑在 HextechCardEffectTypes；这里保留转发给尚未迁移的调用方。
	internal static bool ShouldTreatSkillAsAttack(Player? owner) => HextechCardEffectTypes.ShouldTreatSkillAsAttack(owner);

	internal static bool IsOriginalOwnedSkill(CardModel? card, Player owner) => HextechCardEffectTypes.IsOriginalOwnedSkill(card, owner);

	internal static bool IsAttackForEffects(CardModel? card, Player? owner) => HextechCardEffectTypes.IsAttackForEffects(card, owner);

	internal static bool IsSkillForEffects(CardModel? card) => HextechCardEffectTypes.IsSkillForEffects(card);

	[HarmonyPatch(typeof(Finisher), "CanonicalVars", MethodType.Getter)]
	[HextechPatch("rune.illusory-weapon.finisher", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class FinisherCanonicalVarsPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => HextechPlayerRuneHooks.IllusoryWeaponReflectionReady;

		[HarmonyPostfix]
		private static void Postfix(ref IEnumerable<DynamicVar> __result)
		{
			__result = __result.Select(static dynamicVar =>
				dynamicVar.Name == HextechPlayerRuneHooks.FinisherCalculatedHitsKey
					? new CalculatedVar(HextechPlayerRuneHooks.FinisherCalculatedHitsKey).WithMultiplier(HextechPlayerRuneHooks.CountFinisherAttackCardsPlayedThisTurn)
					: dynamicVar);
		}
	}

	[HarmonyPatch(typeof(Nunchaku), nameof(Nunchaku.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.nunchaku", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class NunchakuPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => HextechPlayerRuneHooks.IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(Nunchaku __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!HextechPlayerRuneHooks.ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner))
			{
				return true;
			}

			__result = HextechPlayerRuneHooks.ResolveIllusoryWeaponNunchaku(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(Kunai), nameof(Kunai.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.kunai", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class KunaiPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => HextechPlayerRuneHooks.IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(Kunai __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!HextechPlayerRuneHooks.ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
			{
				return true;
			}

			__result = HextechPlayerRuneHooks.ResolveIllusoryWeaponKunai(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(Shuriken), nameof(Shuriken.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.shuriken", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class ShurikenPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => HextechPlayerRuneHooks.IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(Shuriken __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!HextechPlayerRuneHooks.ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
			{
				return true;
			}

			__result = HextechPlayerRuneHooks.ResolveIllusoryWeaponShuriken(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(OrnamentalFan), nameof(OrnamentalFan.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.ornamental-fan", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class OrnamentalFanPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => HextechPlayerRuneHooks.IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(OrnamentalFan __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!HextechPlayerRuneHooks.ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
			{
				return true;
			}

			__result = HextechPlayerRuneHooks.ResolveIllusoryWeaponOrnamentalFan(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(PenNib), nameof(PenNib.BeforeCardPlayed), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.pen-nib-before", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class PenNibBeforeCardPlayedPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => HextechPlayerRuneHooks.IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(PenNib __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!HextechPlayerRuneHooks.ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner))
			{
				return true;
			}

			__instance.NotifyAttackPlayed();
			if (__instance.AttacksPlayed == 0)
			{
				HextechPlayerRuneHooks.SetPenNibAttackToDouble(__instance, cardPlay.Card);
			}
			__result = Task.CompletedTask;
			return false;
		}
	}

	[HarmonyPatch(typeof(PenNib), nameof(PenNib.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.pen-nib-after", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class PenNibAfterCardPlayedPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => HextechPlayerRuneHooks.IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(PenNib __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!HextechPlayerRuneHooks.ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner)
				|| !HextechPlayerRuneHooks.IsPenNibTracking(__instance, cardPlay.Card))
			{
				return true;
			}

			__result = Task.CompletedTask;
			return false;
		}
	}
}
