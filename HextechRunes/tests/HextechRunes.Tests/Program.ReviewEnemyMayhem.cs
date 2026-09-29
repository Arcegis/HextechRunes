using System.Reflection;
using HextechRunes;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes.Tests;

internal static partial class Program
{
	// 改名只改 C# 标识符：沃格莫特之灵的抽牌计数仍写旧 JSON 键；删掉的奥术重击字段出现在旧档里也能正常读。
	[HextechTest]
	private static void ReviewCombatTrackingSnapshotKeepsLegacyJsonKeys()
	{
		HextechMayhemCombatTrackingState state = new();
		state.WarmogsSpiritPlayerCardsDrawnThisCombat[1] = 5;
		string json = state.Serialize();
		Expect(json.Contains("\"PlayerCardsDrawnThisCombat\"", StringComparison.Ordinal), "Warmog draw counter keeps the legacy JSON key");
		Expect(!json.Contains("WarmogsSpirit", StringComparison.Ordinal), "the C# rename does not leak into the JSON");

		const string legacyJson = "{\"PlayerCardsDrawnThisCombat\":{\"1\":7},\"ArcanePunchPlayerAttackCardsPlayed\":3,\"EscapePlanTriggered\":[4,9]}";
		HextechMayhemCombatTrackingState restored = new();
		restored.Restore(legacyJson);
		Equal(7, restored.WarmogsSpiritPlayerCardsDrawnThisCombat[1], "legacy Warmog counter restores into the renamed field");
		Expect(restored.EscapePlanTriggered.SetEquals([4u, 9u]), "HashSet fields restore through the cached accessor");

		restored.DevilsDanceTriggeredThisTurn.Add(3);
		restored.PreparePlayerSideTurnStart();
		Equal(0, restored.DevilsDanceTriggeredThisTurn.Count, "per-turn HashSet fields clear on the player turn start phase");
		Equal(2, restored.EscapePlanTriggered.Count, "combat-long HashSet fields survive a turn boundary");

		restored.Reset();
		Equal(0, restored.WarmogsSpiritPlayerCardsDrawnThisCombat.Count, "reset clears dictionaries");
		Equal(0, restored.EscapePlanTriggered.Count, "reset clears HashSets");
		Equal("", restored.Serialize(), "a cleared tracking state serializes as empty");
	}

	// TXT 同步脚本按字面量读描述阈值，效果类的常量必须与之一致。
	[HextechTest]
	private static void ReviewEnemyMaxHpStepThresholdsMatchCatalogLiterals()
	{
		var thresholds = (System.Collections.IDictionary)typeof(MonsterHexCatalog)
			.GetField("PlayerCountScaledThresholds", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
		Equal(("HpPerPercent", HeavyHitterEnemyHex.HpPerPercentPerPlayer), ((string, int))thresholds[MonsterHexKind.HeavyHitter]!, "Heavy Hitter threshold");
		Equal(("HpPerPercent", VitalitySurgeEnemyHex.HpPerPercentPerPlayer), ((string, int))thresholds[MonsterHexKind.VitalitySurge]!, "Vitality Surge threshold");
		Equal(("HpPerPercent", ProteinShakeEnemyHex.HpPerPercentPerPlayer), ((string, int))thresholds[MonsterHexKind.ProteinShake]!, "Protein Shake threshold");

		Equal(1.30m, HeavyHitterEnemyHex.ResolveMultiplier(10000m, 3), "capped multipliers stay capped for any party size");
		Equal(1.02m, VitalitySurgeEnemyHex.ResolveMultiplier(120m, 3), "Vitality Surge threshold scales with the party");
		Equal(1m, ProteinShakeEnemyHex.ResolveMultiplier(4m, 1), "Protein Shake below the first step");
		Equal(16, HextechEnemyHexContext.ClampScalingPlayerCount(40), "scaling player count caps at sixteen");
		Equal(1, HextechEnemyHexContext.ClampScalingPlayerCount(0), "scaling player count never drops below one");
	}

	// 原来 if 链补的能力提示并入表后仍然出现（顺序：表内能力 → 灼烧 → 卡牌/关键词）。
	[HextechTest]
	private static void ReviewEnemyHexHoverTipTablesCoverFormerIfChain()
	{
		SequenceEqual(new[] { typeof(HextechNextTurnDamagePower) }, MonsterHexCatalog.GetEnemyHexPowerHoverTipTypes(MonsterHexKind.Compensation), "Compensation tip");
		SequenceEqual(new[] { typeof(HextechGalvanicPower) }, MonsterHexCatalog.GetEnemyHexPowerHoverTipTypes(MonsterHexKind.SolidTime), "Solid Time tip");
		SequenceEqual(new[] { typeof(SuckPower) }, MonsterHexCatalog.GetEnemyHexPowerHoverTipTypes(MonsterHexKind.FossilStalker), "Fossil Stalker tip");
		SequenceEqual(new[] { typeof(HextechPlayerSlowPower) }, MonsterHexCatalog.GetEnemyHexPowerHoverTipTypes(MonsterHexKind.AncientStatue), "Ancient Statue tip");
		SequenceEqual(new[] { typeof(HextechPlayerSlowPower) }, MonsterHexCatalog.GetEnemyHexPowerHoverTipTypes(MonsterHexKind.HundredRefinements), "Hundred Refinements tip");
	}

	[HextechTest]
	private static void ReviewLoadedAssemblyLookupFindsLoadedAndMissesUnknown()
	{
		HextechLoadedAssemblyLookup own = new(typeof(HextechLoadedAssemblyLookup).Assembly.GetName().Name!, StringComparison.Ordinal);
		Expect(own.Find() == typeof(HextechLoadedAssemblyLookup).Assembly, "a loaded assembly is found by name");
		Expect(own.Find() == typeof(HextechLoadedAssemblyLookup).Assembly, "the cached result is stable");
		HextechLoadedAssemblyLookup missing = new("HextechRunes.Tests.NoSuchAssembly", StringComparison.Ordinal);
		Expect(missing.Find() == null, "an absent soft dependency resolves to null");
		Expect(missing.Find() == null, "the negative result is cached until another assembly loads");
	}

	// 单机（或非联机战斗）时，Power 的「每回合 1 次」退回调用方自己的本地标记。
	[HextechTest]
	private static void ReviewOwnerTurnProcFallsBackToLocalFlagOutsideNetworkCombat()
	{
		bool triggered = false;
		Expect(!HextechCombatProcTracker.HasOwnerTurnProcTriggered(null, "review-proc", triggered), "not triggered yet");
		Expect(HextechCombatProcTracker.TryConsumeOwnerTurnProc(null, "review-proc", ref triggered), "first consume succeeds");
		Expect(triggered, "the local flag records the trigger");
		Expect(HextechCombatProcTracker.HasOwnerTurnProcTriggered(null, "review-proc", triggered), "triggered after consuming");
		Expect(!HextechCombatProcTracker.TryConsumeOwnerTurnProc(null, "review-proc", ref triggered), "second consume in the same turn fails");
	}

	[HextechTest]
	private static void ReviewModelHooksLiveOnTheirOwnModels()
	{
		const BindingFlags declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
		Expect(typeof(WhiteHoleCard).GetMethod(nameof(CardModel.AfterCardDrawn), declared) != null, "White Hole listens to its own draw like vanilla Void");
		Expect(typeof(StormUpgradeRune).GetMethod(nameof(RelicModel.BeforeCombatStart), declared) != null
			&& typeof(StormUpgradeRune).GetMethod(nameof(RelicModel.AfterCombatEnd), declared) != null,
			"Storm upgrade clears its per-card lightning records around combat");
		Expect(typeof(HextechAttackReplayPower).IsSubclassOf(typeof(HextechPowerBase))
			&& typeof(HextechDragonSoulPower).IsSubclassOf(typeof(HextechPowerBase))
			&& typeof(HextechCloudDragonSoulPower).IsSubclassOf(typeof(HextechPowerBase)),
			"own powers use the shared safe Flash");
		Expect(typeof(HextechModifierBase).GetMethod(nameof(HextechModifierBase.AfterSideTurnStartForParticipants), declared) != null
			&& typeof(HextechModifierBase).GetMethod(nameof(HextechModifierBase.AfterTurnEndForParticipants), declared) != null
			&& typeof(HextechPowerBase).GetMethod(nameof(HextechPowerBase.AfterTurnEndForParticipants), declared) != null,
			"turn hook bridges pass participants through");

		MethodInfo typed = typeof(HextechPowerCmdCompat).GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(method => method.Name == nameof(HextechPowerCmdCompat.Apply)
				&& method.IsGenericMethodDefinition
				&& method.GetParameters().Length == 6
				&& method.GetParameters()[0].ParameterType == typeof(PlayerChoiceContext));
		Expect(typed.GetCustomAttribute<ObsoleteAttribute>() == null, "the typed choice-context overload is the supported one");
	}
}
