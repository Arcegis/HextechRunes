using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
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

	private static void InstallIllusoryWeaponHooks(Harmony harmony)
	{
		KunaiAttacksPlayedThisTurnProperty = RequireProperty(typeof(Kunai), "AttacksPlayedThisTurn");
		ShurikenAttacksPlayedThisTurnProperty = RequireProperty(typeof(Shuriken), "AttacksPlayedThisTurn");
		OrnamentalFanAttacksPlayedThisTurnProperty = RequireProperty(typeof(OrnamentalFan), "AttacksPlayedThisTurn");
		PenNibAttackToDoubleProperty = RequireProperty(typeof(PenNib), "AttackToDouble");

		NunchakuDoActivateVisualsMethod = RequireMethod(typeof(Nunchaku), "DoActivateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
		KunaiDoActivateVisualsMethod = RequireMethod(typeof(Kunai), "DoActivateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
		ShurikenDoActivateVisualsMethod = RequireMethod(typeof(Shuriken), "DoActivateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
		OrnamentalFanDoActivateVisualsMethod = RequireMethod(typeof(OrnamentalFan), "DoActivateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);

		harmony.Patch(
			RequireGetter(typeof(Finisher), "CanonicalVars"),
			postfix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(FinisherCanonicalVarsPostfix)));
		harmony.Patch(
			RequireMethod(typeof(Nunchaku), nameof(Nunchaku.AfterCardPlayed), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(NunchakuAfterCardPlayedPrefix)));
		harmony.Patch(
			RequireMethod(typeof(Kunai), nameof(Kunai.AfterCardPlayed), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(KunaiAfterCardPlayedPrefix)));
		harmony.Patch(
			RequireMethod(typeof(Shuriken), nameof(Shuriken.AfterCardPlayed), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(ShurikenAfterCardPlayedPrefix)));
		harmony.Patch(
			RequireMethod(typeof(OrnamentalFan), nameof(OrnamentalFan.AfterCardPlayed), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(OrnamentalFanAfterCardPlayedPrefix)));
		harmony.Patch(
			RequireMethod(typeof(PenNib), nameof(PenNib.BeforeCardPlayed), BindingFlags.Instance | BindingFlags.Public, typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(PenNibBeforeCardPlayedPrefix)));
		harmony.Patch(
			RequireMethod(typeof(PenNib), nameof(PenNib.AfterCardPlayed), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(PenNibAfterCardPlayedPrefix)));
	}

	private static PropertyInfo RequireProperty(Type type, string name)
	{
		return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Could not find required property {type.FullName}.{name}.");
	}

	private static void FinisherCanonicalVarsPostfix(ref IEnumerable<DynamicVar> __result)
	{
		__result = __result.Select(static dynamicVar =>
			dynamicVar.Name == FinisherCalculatedHitsKey
				? new CalculatedVar(FinisherCalculatedHitsKey).WithMultiplier(CountFinisherAttackCardsPlayedThisTurn)
				: dynamicVar);
	}

	private static decimal CountFinisherAttackCardsPlayedThisTurn(CardModel card, Creature? _)
	{
			return HextechCombatHistoryHelper.CountOwnedAttackCardsPlayedThisTurn(
				card.Owner,
				card.CombatState as CombatState,
				firstInSeriesOnly: false,
				includeAutoPlay: true);
	}

	private static bool NunchakuAfterCardPlayedPrefix(Nunchaku __instance, CardPlay cardPlay, ref Task __result)
	{
		if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner))
		{
			return true;
		}

		__result = ResolveIllusoryWeaponNunchaku(__instance);
		return false;
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

	private static bool KunaiAfterCardPlayedPrefix(Kunai __instance, CardPlay cardPlay, ref Task __result)
	{
		if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
		{
			return true;
		}

		__result = ResolveIllusoryWeaponKunai(__instance);
		return false;
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

	private static bool ShurikenAfterCardPlayedPrefix(Shuriken __instance, CardPlay cardPlay, ref Task __result)
	{
		if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
		{
			return true;
		}

		__result = ResolveIllusoryWeaponShuriken(__instance);
		return false;
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

	private static bool OrnamentalFanAfterCardPlayedPrefix(OrnamentalFan __instance, CardPlay cardPlay, ref Task __result)
	{
		if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner) || !CombatManager.Instance.IsInProgress)
		{
			return true;
		}

		__result = ResolveIllusoryWeaponOrnamentalFan(__instance);
		return false;
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

	private static bool PenNibBeforeCardPlayedPrefix(PenNib __instance, CardPlay cardPlay, ref Task __result)
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

	private static bool PenNibAfterCardPlayedPrefix(PenNib __instance, CardPlay cardPlay, ref Task __result)
	{
		if (!ShouldHandleIllusoryWeaponSkill(cardPlay, __instance.Owner)
			|| !IsPenNibTracking(__instance, cardPlay.Card))
		{
			return true;
		}

		__result = Task.CompletedTask;
		return false;
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
}
