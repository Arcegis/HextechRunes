using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Relics;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechPlayerRuneHooks
{
	private const string FinisherCalculatedHitsKey = "CalculatedHits";

	private static PropertyInfo? KunaiAttacksPlayedThisTurnProperty;
	private static PropertyInfo? ShurikenAttacksPlayedThisTurnProperty;
	private static PropertyInfo? OrnamentalFanAttacksPlayedThisTurnProperty;
	private static PropertyInfo? PenNibAttackToDoubleProperty;

	private static MethodInfo? NunchakuDoActivateVisualsMethod;
	private static MethodInfo? KunaiDoActivateVisualsMethod;
	private static MethodInfo? ShurikenDoActivateVisualsMethod;
	private static MethodInfo? OrnamentalFanDoActivateVisualsMethod;
	private static bool? _illusoryWeaponReflectionReady;

	/// <summary>
	/// 幻影武器要改写五个原版遗物的私有计数与视觉方法;任一缺失就整组停用并把符文标为本运行时不可用。
	/// 七个补丁类共用这一次解析。
	/// </summary>
	private static bool IllusoryWeaponReflectionReady
	{
		get
		{
			if (_illusoryWeaponReflectionReady is bool cached)
			{
				return cached;
			}

			try
			{
				KunaiAttacksPlayedThisTurnProperty = RequireProperty(typeof(Kunai), "AttacksPlayedThisTurn");
				ShurikenAttacksPlayedThisTurnProperty = RequireProperty(typeof(Shuriken), "AttacksPlayedThisTurn");
				OrnamentalFanAttacksPlayedThisTurnProperty = RequireProperty(typeof(OrnamentalFan), "AttacksPlayedThisTurn");
				PenNibAttackToDoubleProperty = RequireProperty(typeof(PenNib), "AttackToDouble");

				NunchakuDoActivateVisualsMethod = RequireMethod(typeof(Nunchaku), "DoActivateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
				KunaiDoActivateVisualsMethod = RequireMethod(typeof(Kunai), "DoActivateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
				ShurikenDoActivateVisualsMethod = RequireMethod(typeof(Shuriken), "DoActivateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
				OrnamentalFanDoActivateVisualsMethod = RequireMethod(typeof(OrnamentalFan), "DoActivateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
				_illusoryWeaponReflectionReady = true;
			}
			catch (Exception ex)
			{
				HextechRuntimeRuneCompatibility.MarkPlayerRuneHookFailed<IllusoryWeaponRune>("illusory weapon attack counters", ex);
				_illusoryWeaponReflectionReady = false;
			}

			return _illusoryWeaponReflectionReady.Value;
		}
	}

	private static PropertyInfo RequireProperty(Type type, string name)
	{
		return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Could not find required property {type.FullName}.{name}.");
	}


	private static decimal CountFinisherAttackCardsPlayedThisTurn(CardModel card, Creature? _)
	{
			return HextechCombatHistoryHelper.CountOwnedAttackCardsPlayedThisTurn(
				card.Owner,
				card.CombatState as CombatState,
				firstInSeriesOnly: false,
				includeAutoPlay: true);
	}


	private static async Task ResolveIllusoryWeaponNunchaku(Nunchaku nunchaku)
	{
		nunchaku.AttacksPlayed++;
		int cardsNeeded = nunchaku.DynamicVars.Cards.IntValue;
		if (cardsNeeded <= 0 || !CombatManager.Instance.IsInProgress || nunchaku.AttacksPlayed % cardsNeeded != 0)
		{
			return;
		}

		_ = TaskHelper.RunSafely(InvokePrivateRelicVisuals(nunchaku, NunchakuDoActivateVisualsMethod, nameof(Nunchaku)));
		await PlayerCmd.GainEnergy(nunchaku.DynamicVars.Energy.BaseValue, nunchaku.Owner);
	}


	private static async Task ResolveIllusoryWeaponKunai(Kunai kunai)
	{
		int attacksPlayed = IncrementIntProperty(kunai, KunaiAttacksPlayedThisTurnProperty);
		int cardsNeeded = kunai.DynamicVars.Cards.IntValue;
		if (cardsNeeded <= 0 || attacksPlayed % cardsNeeded != 0)
		{
			return;
		}

		_ = TaskHelper.RunSafely(InvokePrivateRelicVisuals(kunai, KunaiDoActivateVisualsMethod, nameof(Kunai)));
		await PowerCmd.Apply<DexterityPower>(kunai.Owner.Creature, kunai.DynamicVars.Dexterity.BaseValue, kunai.Owner.Creature, null);
	}


	private static async Task ResolveIllusoryWeaponShuriken(Shuriken shuriken)
	{
		int attacksPlayed = IncrementIntProperty(shuriken, ShurikenAttacksPlayedThisTurnProperty);
		int cardsNeeded = shuriken.DynamicVars.Cards.IntValue;
		if (cardsNeeded <= 0 || attacksPlayed % cardsNeeded != 0)
		{
			return;
		}

		_ = TaskHelper.RunSafely(InvokePrivateRelicVisuals(shuriken, ShurikenDoActivateVisualsMethod, nameof(Shuriken)));
		await PowerCmd.Apply<StrengthPower>(shuriken.Owner.Creature, shuriken.DynamicVars.Strength.BaseValue, shuriken.Owner.Creature, null);
	}


	private static async Task ResolveIllusoryWeaponOrnamentalFan(OrnamentalFan ornamentalFan)
	{
		int attacksPlayed = IncrementIntProperty(ornamentalFan, OrnamentalFanAttacksPlayedThisTurnProperty);
		int cardsNeeded = ornamentalFan.DynamicVars.Cards.IntValue;
		if (cardsNeeded <= 0 || attacksPlayed % cardsNeeded != 0)
		{
			return;
		}

		_ = TaskHelper.RunSafely(InvokePrivateRelicVisuals(ornamentalFan, OrnamentalFanDoActivateVisualsMethod, nameof(OrnamentalFan)));
		await CreatureCmd.GainBlock(ornamentalFan.Owner.Creature, ornamentalFan.DynamicVars.Block, null);
	}


	internal static void ClearIllusoryWeaponPendingPenNib(Player? owner, CardModel card)
	{
		PenNib? penNib = owner?.GetRelic<PenNib>();
		if (penNib == null || !IsPenNibTracking(penNib, card))
		{
			return;
		}

		SetPenNibAttackToDouble(penNib, null);
	}

	private static bool ShouldHandleIllusoryWeaponSkill(CardPlay cardPlay, Player? owner)
	{
		return owner != null
			&& cardPlay.Card.Type != CardType.Attack
			&& cardPlay.Card.Owner == owner
			&& IllusoryWeaponRune.IsAttackForEffects(cardPlay.Card, owner);
	}

	private static int IncrementIntProperty(object instance, PropertyInfo? property)
	{
		int value = property?.GetValue(instance) is int current ? current : 0;
		value++;
		property?.SetValue(instance, value);
		return value;
	}

	private static bool IsPenNibTracking(PenNib penNib, CardModel card)
	{
		return ReferenceEquals(PenNibAttackToDoubleProperty?.GetValue(penNib), card);
	}

	private static void SetPenNibAttackToDouble(PenNib penNib, CardModel? card)
	{
		PenNibAttackToDoubleProperty?.SetValue(penNib, card);
	}

	private static Task InvokePrivateRelicVisuals(RelicModel relic, MethodInfo? method, string relicName)
	{
		if (method == null)
		{
			return Task.CompletedTask;
		}

		try
		{
			return method.Invoke(relic, null) as Task ?? Task.CompletedTask;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][IllusoryWeapon] Failed to run {relicName} activation visuals: {ex.GetType().Name}: {ex.Message}");
			relic.Flash();
			return Task.CompletedTask;
		}
	}

	[HarmonyPatch(typeof(Finisher), "CanonicalVars", MethodType.Getter)]
	[HextechPatch("rune.illusory-weapon.finisher", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class FinisherCanonicalVarsPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => IllusoryWeaponReflectionReady;

		[HarmonyPostfix]
		private static void Postfix(ref IEnumerable<DynamicVar> __result)
		{
			__result = __result.Select(static dynamicVar =>
				dynamicVar.Name == FinisherCalculatedHitsKey
					? new CalculatedVar(FinisherCalculatedHitsKey).WithMultiplier(CountFinisherAttackCardsPlayedThisTurn)
					: dynamicVar);
		}
	}

	[HarmonyPatch(typeof(Nunchaku), nameof(Nunchaku.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.nunchaku", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class NunchakuPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		private static bool Prefix(Nunchaku __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner))
			{
				return true;
			}

			__result = ResolveIllusoryWeaponNunchaku(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(Kunai), nameof(Kunai.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.kunai", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class KunaiPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		private static bool Prefix(Kunai __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
			{
				return true;
			}

			__result = ResolveIllusoryWeaponKunai(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(Shuriken), nameof(Shuriken.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.shuriken", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class ShurikenPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		private static bool Prefix(Shuriken __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
			{
				return true;
			}

			__result = ResolveIllusoryWeaponShuriken(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(OrnamentalFan), nameof(OrnamentalFan.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.ornamental-fan", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class OrnamentalFanPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		private static bool Prefix(OrnamentalFan __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
			{
				return true;
			}

			__result = ResolveIllusoryWeaponOrnamentalFan(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(PenNib), nameof(PenNib.BeforeCardPlayed), typeof(CardPlay))]
	[HextechPatch("rune.illusory-weapon.pen-nib-before", "幻影武器", Rune = typeof(IllusoryWeaponRune))]
	private static class PenNibBeforeCardPlayedPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		private static bool Prefix(PenNib __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner))
			{
				return true;
			}

			__instance.NotifyAttackPlayed();
			if (__instance.AttacksPlayed == 0)
			{
				SetPenNibAttackToDouble(__instance, cardPlay.Card);
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
		private static bool Prepare() => IllusoryWeaponReflectionReady;

		[HarmonyPrefix]
		private static bool Prefix(PenNib __instance, CardPlay cardPlay, ref Task __result)
		{
			if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner)
				|| !IsPenNibTracking(__instance, cardPlay.Card))
			{
				return true;
			}

			__result = Task.CompletedTask;
			return false;
		}
	}
}
