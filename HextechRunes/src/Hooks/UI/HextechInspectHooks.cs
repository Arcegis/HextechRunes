using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

namespace HextechRunes;

internal static class HextechInspectHooks
{
	// 检视界面改写依赖的原版私有成员;任一缺失时两个检视补丁都不安装(缺失项已进启动摘要)。
	private static readonly InspectScreenMembers? Members = InspectScreenMembers.TryResolve();

	private readonly record struct InspectOpenState(RelicModel? RequestedRelic);

	/// <summary>原版 <c>NInspectRelicScreen</c> 的私有成员(0.107.1 / 0.110.0 / 0.111.0 同名同签名)。</summary>
	private sealed record InspectScreenMembers(
		FieldInfo UnlockedRelics,
		FieldInfo Relics,
		FieldInfo Index,
		FieldInfo NameLabel,
		FieldInfo RarityLabel,
		FieldInfo Description,
		FieldInfo Flavor,
		FieldInfo Image,
		FieldInfo HoverTipRect,
		MethodInfo UpdateRelicDisplay,
		MethodInfo SetRelic,
		MethodInfo SetRarityVisuals)
	{
		internal static InspectScreenMembers? TryResolve()
		{
			Type type = typeof(NInspectRelicScreen);
			const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
			FieldInfo? unlockedRelics = HextechHookReflection.TryGetField(type, "_allUnlockedRelics");
			FieldInfo? relics = HextechHookReflection.TryGetField(type, "_relics");
			FieldInfo? index = HextechHookReflection.TryGetField(type, "_index");
			FieldInfo? nameLabel = HextechHookReflection.TryGetField(type, "_nameLabel");
			FieldInfo? rarityLabel = HextechHookReflection.TryGetField(type, "_rarityLabel");
			FieldInfo? description = HextechHookReflection.TryGetField(type, "_description");
			FieldInfo? flavor = HextechHookReflection.TryGetField(type, "_flavor");
			FieldInfo? image = HextechHookReflection.TryGetField(type, "_relicImage");
			FieldInfo? hoverTipRect = HextechHookReflection.TryGetField(type, "_hoverTipRect");
			MethodInfo? updateRelicDisplay = HextechHookReflection.TryGetMethod(type, "UpdateRelicDisplay", PrivateInstance);
			MethodInfo? setRelic = HextechHookReflection.TryGetMethod(type, "SetRelic", PrivateInstance, typeof(int));
			MethodInfo? setRarityVisuals = HextechHookReflection.TryGetMethod(type, "SetRarityVisuals", PrivateInstance, typeof(RelicRarity));
			if (unlockedRelics == null
				|| relics == null
				|| index == null
				|| nameLabel == null
				|| rarityLabel == null
				|| description == null
				|| flavor == null
				|| image == null
				|| hoverTipRect == null
				|| updateRelicDisplay == null
				|| setRelic == null
				|| setRarityVisuals == null)
			{
				return null;
			}

			return new InspectScreenMembers(
				unlockedRelics,
				relics,
				index,
				nameLabel,
				rarityLabel,
				description,
				flavor,
				image,
				hoverTipRect,
				updateRelicDisplay,
				setRelic,
				setRarityVisuals);
		}
	}

	internal static bool ShouldHandleInspectRequest(RelicModel relic)
	{
		return HextechCatalog.IsHextechCustomRelic(relic);
	}

	internal static IReadOnlyList<RelicModel> MergeRequestedInspectRelic(
		IReadOnlyList<RelicModel> relics,
		RelicModel requestedRelic,
		out int requestedIndex)
	{
		for (int index = 0; index < relics.Count; index++)
		{
			RelicModel candidate = relics[index];
			if (candidate != null
				&& (ReferenceEquals(candidate, requestedRelic) || candidate.Id == requestedRelic.Id))
			{
				requestedIndex = index;
				return relics;
			}
		}

		List<RelicModel> merged = relics.ToList();
		merged.Add(requestedRelic);
		requestedIndex = merged.Count - 1;
		return merged;
	}

	/// <summary>
	/// 把海克斯遗物的规范实例补进检视界面的"已解锁"集合。只改检视界面自己的集合,不回写遗物模型:
	/// 可变副本缺规范实例时按 ID 取规范模型放进集合即可(海克斯遗物的显示本就由 <see cref="UpdateRelicDisplayPatch"/> 接管)。
	/// </summary>
	private static void EnsureInspectRelicsUnlocked(InspectScreenMembers members, NInspectRelicScreen screen, IReadOnlyList<RelicModel> relics)
	{
		if (members.UnlockedRelics.GetValue(screen) is not HashSet<RelicModel> unlockedRelics)
		{
			return;
		}

		foreach (RelicModel canonicalRelic in HextechCatalog.GetCanonicalVisibleCustomRelics())
		{
			unlockedRelics.Add(canonicalRelic);
		}

		foreach (RelicModel relic in relics)
		{
			if (HextechCatalog.IsHextechCustomRelic(relic))
			{
				unlockedRelics.Add(relic.CanonicalInstance ?? ModelDb.GetById<RelicModel>(relic.Id));
			}
		}
	}

	/// <summary>按原版"已解锁且已发现"分支渲染海克斯遗物(名称、稀有度、描述、风味、大图、提示)。</summary>
	private static void RenderHextechInspect(InspectScreenMembers members, NInspectRelicScreen screen, RelicModel relic)
	{
		if (members.NameLabel.GetValue(screen) is not MegaLabel nameLabel
			|| members.RarityLabel.GetValue(screen) is not MegaLabel rarityLabel
			|| members.Description.GetValue(screen) is not MegaRichTextLabel description
			|| members.Flavor.GetValue(screen) is not MegaRichTextLabel flavor
			|| members.Image.GetValue(screen) is not TextureRect image
			|| members.HoverTipRect.GetValue(screen) is not Control hoverTipRect)
		{
			return;
		}

		nameLabel.SetTextAutoSize(relic.Title.GetFormattedText());
		LocString rarityText = new("gameplay_ui", "RELIC_RARITY." + relic.Rarity.ToString().ToUpperInvariant());
		rarityLabel.SetTextAutoSize(rarityText.GetFormattedText());
		image.SelfModulate = Colors.White;
		description.SetTextAutoSize(relic.DynamicDescription.GetFormattedText());
		flavor.SetTextAutoSize(relic.Flavor.GetFormattedText());
		members.SetRarityVisuals.Invoke(screen, [relic.Rarity]);
		image.Texture = relic.BigIcon;

		NHoverTipSet.Clear();
		NHoverTipSet? hoverTipSet = NHoverTipSet.CreateAndShow(screen, relic.HoverTipsExcludingRelic);
		hoverTipSet?.SetAlignment(hoverTipRect, HoverTip.GetHoverTipAlignment(screen));
	}

	[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.Relics), MethodType.Getter)]
	[HextechPatch("ui.inspect.unlock-state-relics", "遗物检视界面", Optional = true)]
	private static class UnlockStateRelicsPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ref IEnumerable<RelicModel> __result)
		{
			__result = __result.Concat(HextechCatalog.GetCanonicalVisibleCustomRelics()).Distinct();
		}
	}

	[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.IsRelicSeen), typeof(RelicModel))]
	[HextechPatch("ui.inspect.relic-seen", "遗物检视界面", Optional = true)]
	private static class IsRelicSeenPatch
	{
		[HarmonyPostfix]
		private static void Postfix(RelicModel relic, ref bool __result)
		{
			if (HextechCatalog.IsHextechCustomRelic(relic))
			{
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPrefix), typeof(AbstractModel))]
	[HextechPatch("ui.inspect.energy-icon-prefix", "遗物检视界面", Optional = true)]
	private static class EnergyIconPrefixPatch
	{
		[HarmonyPostfix]
		private static void Postfix(AbstractModel model, ref string __result)
		{
			if (model is RelicModel relic && HextechCatalog.IsHextechCustomRelic(relic))
			{
				__result = "red";
			}
		}
	}

	[HarmonyPatch(typeof(NInspectRelicScreen), nameof(NInspectRelicScreen.Open), typeof(IReadOnlyList<RelicModel>), typeof(RelicModel))]
	[HextechPatch("ui.inspect.open", "遗物检视界面", Optional = true)]
	private static class OpenPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => Members != null;

		[HarmonyPrefix]
		[HarmonyPriority(Priority.Last)]
		private static void Prefix(ref IReadOnlyList<RelicModel> relics, ref RelicModel relic, out InspectOpenState __state)
		{
			__state = default;
			if (!ShouldHandleInspectRequest(relic))
			{
				return;
			}

			RelicModel requestedRelic = relic;
			IReadOnlyList<RelicModel> correctedRelics = MergeRequestedInspectRelic(relics, requestedRelic, out int correctedIndex);
			relics = correctedRelics;
			relic = correctedRelics[correctedIndex];
			__state = new InspectOpenState(requestedRelic);
		}

		[HarmonyPostfix]
		[HarmonyPriority(Priority.Last)]
		private static void Postfix(NInspectRelicScreen __instance, InspectOpenState __state)
		{
			if (Members is not { } members
				|| __state.RequestedRelic == null
				|| members.Relics.GetValue(__instance) is not IReadOnlyList<RelicModel> finalRelics)
			{
				return;
			}

			IReadOnlyList<RelicModel> mergedRelics = MergeRequestedInspectRelic(
				finalRelics,
				__state.RequestedRelic,
				out int requestedIndex);
			EnsureInspectRelicsUnlocked(members, __instance, mergedRelics);
			if (!ReferenceEquals(mergedRelics, finalRelics))
			{
				members.Relics.SetValue(__instance, mergedRelics);
			}

			members.SetRelic.Invoke(__instance, [requestedIndex]);
			members.UpdateRelicDisplay.Invoke(__instance, null);
		}
	}

	/// <summary>检视界面当前是海克斯遗物时,按"已解锁且已发现"渲染它。</summary>
	/// <remarks>
	/// 跳过型前缀:原版私有 <c>NInspectRelicScreen.UpdateRelicDisplay</c> 按 <c>_allUnlockedRelics.Contains(relic.CanonicalInstance)</c>
	/// 与 <c>SaveManager.IsRelicSeen</c> 硬分"未解锁/未发现/正常"三支,没有 Hook 能改这个分支判定;
	/// 海克斯的隐藏遗物(不进 UnlockState)与缺规范实例的可变副本会落进"未解锁"分支。
	/// 替换体逐项复刻原版"正常"分支(名称、稀有度、描述、风味、稀有度视觉、大图、提示)。
	/// 激活条件:当前索引的遗物属于海克斯(<see cref="HextechCatalog.IsHextechCustomRelic"/>);其它遗物交给原版。
	/// 版本:0.107.1 / 0.110.0 / 0.111.0 原方法一致,已进原版拷贝守卫;<see cref="Priority.Low"/> 让他人前缀先跑。
	/// </remarks>
	[HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
	[HextechPatch("ui.inspect.update-display", "遗物检视界面", Optional = true)]
	private static class UpdateRelicDisplayPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => Members != null;

		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(NInspectRelicScreen __instance)
		{
			if (Members is { } members
				&& members.Relics.GetValue(__instance) is IReadOnlyList<RelicModel> relics
				&& members.Index.GetValue(__instance) is int index
				&& index >= 0
				&& index < relics.Count)
			{
				RelicModel relic = relics[index];
				if (HextechCatalog.IsHextechCustomRelic(relic))
				{
					RenderHextechInspect(members, __instance, relic);
					return false;
				}
			}

			return true;
		}
	}
}
