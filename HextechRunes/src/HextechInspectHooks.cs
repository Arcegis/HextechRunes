using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

namespace HextechRunes;

internal static class HextechInspectHooks
{
	private static readonly FieldInfo InspectRelicScreenUnlockedRelicsField = RequireField(typeof(NInspectRelicScreen), "_allUnlockedRelics");
	private static readonly FieldInfo InspectRelicScreenRelicsField = RequireField(typeof(NInspectRelicScreen), "_relics");
	private static readonly FieldInfo InspectRelicScreenIndexField = RequireField(typeof(NInspectRelicScreen), "_index");
	private static readonly FieldInfo RelicCanonicalInstanceField = RequireField(typeof(RelicModel), "_canonicalInstance");
	private static readonly MethodInfo InspectRelicScreenUpdateRelicDisplayMethod = RequireMethod(typeof(NInspectRelicScreen), "UpdateRelicDisplay", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly MethodInfo InspectRelicScreenSetRelicMethod = RequireMethod(typeof(NInspectRelicScreen), "SetRelic", BindingFlags.Instance | BindingFlags.NonPublic, typeof(int));
	private static readonly FieldInfo InspectRelicScreenNameLabelField = RequireField(typeof(NInspectRelicScreen), "_nameLabel");
	private static readonly FieldInfo InspectRelicScreenRarityLabelField = RequireField(typeof(NInspectRelicScreen), "_rarityLabel");
	private static readonly FieldInfo InspectRelicScreenDescriptionField = RequireField(typeof(NInspectRelicScreen), "_description");
	private static readonly FieldInfo InspectRelicScreenFlavorField = RequireField(typeof(NInspectRelicScreen), "_flavor");
	private static readonly FieldInfo InspectRelicScreenImageField = RequireField(typeof(NInspectRelicScreen), "_relicImage");
	private static readonly FieldInfo InspectRelicScreenHoverTipRectField = RequireField(typeof(NInspectRelicScreen), "_hoverTipRect");
	private static readonly MethodInfo InspectRelicScreenSetRarityVisualsMethod = RequireMethod(typeof(NInspectRelicScreen), "SetRarityVisuals", BindingFlags.Instance | BindingFlags.NonPublic, typeof(RelicRarity));

	private readonly record struct InspectOpenState(IReadOnlyList<RelicModel> CorrectedRelics, int CorrectedIndex);

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			typeof(UnlockState).GetProperty(nameof(UnlockState.Relics), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetMethod!,
			postfix: new HarmonyMethod(typeof(HextechInspectHooks), nameof(GetUnlockStateRelicsPostfix)));
		harmony.Patch(
			RequireMethod(typeof(SaveManager), nameof(SaveManager.IsRelicSeen), BindingFlags.Instance | BindingFlags.Public, typeof(RelicModel)),
			postfix: new HarmonyMethod(typeof(HextechInspectHooks), nameof(IsRelicSeenPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NInspectRelicScreen), nameof(NInspectRelicScreen.Open), BindingFlags.Instance | BindingFlags.Public, typeof(IReadOnlyList<RelicModel>), typeof(RelicModel)),
			prefix: new HarmonyMethod(typeof(HextechInspectHooks), nameof(InspectRelicScreenOpenPrefix)),
			postfix: new HarmonyMethod(typeof(HextechInspectHooks), nameof(InspectRelicScreenOpenPostfix)));
		harmony.Patch(
			InspectRelicScreenUpdateRelicDisplayMethod,
			prefix: new HarmonyMethod(typeof(HextechInspectHooks), nameof(InspectRelicScreenUpdateRelicDisplayPrefix)));
		harmony.Patch(
			RequireMethod(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPrefix), BindingFlags.Static | BindingFlags.Public, typeof(AbstractModel)),
			postfix: new HarmonyMethod(typeof(HextechInspectHooks), nameof(EnergyIconHelperGetPrefixPostfix)));
	}

	private static void GetUnlockStateRelicsPostfix(ref IEnumerable<RelicModel> __result)
	{
		__result = __result.Concat(ModInfo.GetCanonicalVisibleCustomRelics()).Distinct();
	}

	private static void IsRelicSeenPostfix(RelicModel relic, ref bool __result)
	{
		if (ModInfo.IsHextechCustomRelic(relic))
		{
			__result = true;
		}
	}

	private static void InspectRelicScreenOpenPrefix(ref IReadOnlyList<RelicModel> relics, ref RelicModel relic, out InspectOpenState __state)
	{
		List<RelicModel> correctedRelics = relics.ToList();
		RelicModel requestedRelic = relic;
		int correctedIndex = correctedRelics.FindIndex(candidate => ReferenceEquals(candidate, requestedRelic) || candidate.Id == requestedRelic.Id);
		if (correctedIndex < 0)
		{
			correctedRelics.Add(relic);
			correctedIndex = correctedRelics.Count - 1;
		}

		relics = correctedRelics;
		relic = correctedRelics[correctedIndex];
		__state = new InspectOpenState(correctedRelics, correctedIndex);
	}

	private static void InspectRelicScreenOpenPostfix(NInspectRelicScreen __instance, InspectOpenState __state)
	{
		EnsureInspectRelicsUnlocked(__instance, __state.CorrectedRelics);
		InspectRelicScreenRelicsField.SetValue(__instance, __state.CorrectedRelics);
		InspectRelicScreenSetRelicMethod.Invoke(__instance, [__state.CorrectedIndex]);
		InspectRelicScreenUpdateRelicDisplayMethod.Invoke(__instance, null);
	}

	private static bool InspectRelicScreenUpdateRelicDisplayPrefix(NInspectRelicScreen __instance)
	{
		if (InspectRelicScreenRelicsField.GetValue(__instance) is IReadOnlyList<RelicModel> relics
			&& InspectRelicScreenIndexField.GetValue(__instance) is int index
			&& index >= 0
			&& index < relics.Count)
		{
			RelicModel relic = relics[index];
			if (ModInfo.IsHextechCustomRelic(relic))
			{
				RenderHextechInspect(__instance, relic);
				return false;
			}
		}

		return true;
	}

	private static void EnergyIconHelperGetPrefixPostfix(AbstractModel model, ref string __result)
	{
		if (model is RelicModel relic && ModInfo.IsHextechCustomRelic(relic))
		{
			__result = "red";
		}
	}

	private static void EnsureInspectRelicsUnlocked(NInspectRelicScreen screen, IReadOnlyList<RelicModel> relics)
	{
		if (InspectRelicScreenUnlockedRelicsField.GetValue(screen) is not HashSet<RelicModel> unlockedRelics)
		{
			return;
		}

		foreach (RelicModel canonicalRelic in ModInfo.GetCanonicalVisibleCustomRelics())
		{
			unlockedRelics.Add(canonicalRelic);
		}

		foreach (RelicModel relic in relics)
		{
			if (!ModInfo.IsHextechCustomRelic(relic))
			{
				continue;
			}

			unlockedRelics.Add(EnsureCanonicalInstance(relic));
		}
	}

	private static RelicModel EnsureCanonicalInstance(RelicModel relic)
	{
		if (relic.CanonicalInstance != null)
		{
			return relic.CanonicalInstance;
		}

		RelicModel canonical = ModelDb.GetById<RelicModel>(relic.Id);
		RelicCanonicalInstanceField.SetValue(relic, canonical);
		return canonical;
	}

	private static void RenderHextechInspect(NInspectRelicScreen screen, RelicModel relic)
	{
		MegaLabel nameLabel = (MegaLabel)InspectRelicScreenNameLabelField.GetValue(screen)!;
		MegaLabel rarityLabel = (MegaLabel)InspectRelicScreenRarityLabelField.GetValue(screen)!;
		MegaRichTextLabel description = (MegaRichTextLabel)InspectRelicScreenDescriptionField.GetValue(screen)!;
		MegaRichTextLabel flavor = (MegaRichTextLabel)InspectRelicScreenFlavorField.GetValue(screen)!;
		TextureRect image = (TextureRect)InspectRelicScreenImageField.GetValue(screen)!;
		Control hoverTipRect = (Control)InspectRelicScreenHoverTipRectField.GetValue(screen)!;

		nameLabel.SetTextAutoSize(relic.Title.GetFormattedText());
		LocString rarityText = new("gameplay_ui", "RELIC_RARITY." + relic.Rarity.ToString().ToUpperInvariant());
		rarityLabel.SetTextAutoSize(rarityText.GetFormattedText());
		image.SelfModulate = Colors.White;
		description.SetTextAutoSize(relic.DynamicDescription.GetFormattedText());
		flavor.SetTextAutoSize(relic.Flavor.GetFormattedText());
		InspectRelicScreenSetRarityVisualsMethod.Invoke(screen, [relic.Rarity]);
		image.Texture = relic.BigIcon;

		NHoverTipSet.Clear();
		NHoverTipSet? hoverTipSet = NHoverTipSet.CreateAndShow(screen, relic.HoverTipsExcludingRelic);
		hoverTipSet?.SetAlignment(hoverTipRect, HoverTip.GetHoverTipAlignment(screen));
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}

	private static FieldInfo RequireField(Type type, string name)
	{
		return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Could not find required field {type.FullName}.{name}.");
	}
}
