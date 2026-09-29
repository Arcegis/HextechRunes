global using HextechForgeRarityWeights = HextechRunes.HextechRarityWeights;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes.Tests;

internal static partial class Program
{
	private static void ReviewTelemetryConfigWithoutEndpointKeepsUserOptOut()
	{
		string defaultEndpoint = HextechTelemetry.DefaultEndpointForTests;
		(bool enabled, string endpoint) = HextechTelemetry.ParseConfigForTests("{\"enabled\":false}");
		Expect(!enabled, "opt-out without endpoint must stay disabled");
		Equal(defaultEndpoint, endpoint, "missing endpoint falls back to default");

		(enabled, endpoint) = HextechTelemetry.ParseConfigForTests("{\"enabled\":false,\"endpoint\":\"  \"}");
		Expect(!enabled, "opt-out with blank endpoint must stay disabled");
		Equal(defaultEndpoint, endpoint, "blank endpoint falls back to default");

		(enabled, endpoint) = HextechTelemetry.ParseConfigForTests("{\"enabled\":true,\"endpoint\":\"http://example.invalid/x\"}");
		Expect(enabled, "explicit enabled is preserved");
		Equal("http://example.invalid/x", endpoint, "explicit endpoint is preserved");

		(enabled, endpoint) = HextechTelemetry.ParseConfigForTests("null");
		Expect(enabled, "null document keeps previous default-on behavior");
		Equal(defaultEndpoint, endpoint, "null document uses default endpoint");
	}

	/// <summary>
	/// 迁移链改为表驱动后,从每个旧版本迁移的结果必须与改动前的 if 链逐项相同。
	/// 这里保留一份改动前的链作为参照实现。
	/// </summary>
	private static void ReviewConfigMigrationTableMatchesLegacyChain()
	{
		string[] allConfigurable = HextechCatalog.GetConfigurablePlayerRuneIds().Select(static id => id.Entry).ToArray();
		string[][] seeds =
		[
			[],
			allConfigurable,
			[ "custom-unknown-rune", ModelDb.GetId<OkBoomerangRune>().Entry, ModelDb.GetId<StrikeUpgradeRune>().Entry ],
			HextechRuneConfiguration.GetDefaultDisabledPlayerRuneIds().ToArray()
		];
		string[][] monsterSeeds =
		[
			[],
			[ MonsterHexKind.GetExcited.ToString() ],
			[ MonsterHexKind.ShoulderVaku.ToString(), "not-a-hex" ]
		];

		for (int version = 15; version <= 40; version++)
		{
			foreach (string[] seed in seeds)
			{
				(int migratedVersion, IReadOnlySet<string> migrated) = HextechRuneConfiguration.MigrateDisabledIdsForTests(version, seed);
				Expect(migratedVersion >= 39, $"v{version} lands on current version");
				SetEqual(LegacyMigrateDisabledPlayerRuneIds(version, seed), migrated, $"v{version} player migration matches legacy chain (seed size {seed.Length})");
			}

			foreach (string[] seed in monsterSeeds)
			{
				(_, IReadOnlySet<string> migrated) = HextechRuneConfiguration.MigrateDisabledMonsterHexIdsForTests(version, seed);
				SetEqual(LegacyMigrateDisabledMonsterHexIds(version, seed), migrated, $"v{version} monster hex migration matches legacy chain");
			}
		}
	}

	private static HashSet<string> LegacyMigrateDisabledPlayerRuneIds(int version, IEnumerable<string> seed)
	{
		HashSet<string> ids = HextechRuneConfiguration.NormalizeDisabledPlayerRuneIds(seed);
		void Enable(params Type[] types) => ids.ExceptWith(HextechPlayerRuneConfigIds.FromTypes(types));
		void Disable(params Type[] types) => ids.UnionWith(HextechPlayerRuneConfigIds.FromTypes(types));
		if (version < 16) { Enable(typeof(CorruptedBranchRune)); }
		if (version < 17) { Enable(typeof(FeelTheBurnRune), typeof(OkBoomerangRune)); }
		if (version < 18) { Enable(typeof(AstralBodyRune)); }
		if (version < 19) { Disable(typeof(KakaRune)); }
		if (version < 20) { Disable(typeof(PiggyBankRune)); }
		if (version < 21) { Disable(typeof(StrikeUpgradeRune), typeof(DefendUpgradeRune), typeof(CardInspectionRune)); }
		if (version < 22) { Disable(typeof(GetExcitedRune)); }
		if (version < 23) { Disable(typeof(ShoulderVakuRune), typeof(PorcupineRune), typeof(CourageOfColossusRune), typeof(DeathHarvestRune), typeof(FinalFormRune)); }
		if (version < 24) { Enable(typeof(StrikeUpgradeRune), typeof(DefendUpgradeRune)); }
		if (version < 25) { Enable(typeof(AnthonyBiasRune)); }
		if (version < 27) { Disable(typeof(OmegaRune), typeof(OkBoomerangRune), typeof(FeyMagicRune), typeof(AstralBodyRune)); }
		if (version < 30) { Enable(typeof(AdvanceToRetreatRune)); }
		if (version < 31) { Enable(typeof(HappyAccidentRune)); }
		if (version < 34) { Disable(typeof(IllusoryWeaponRune)); }
		if (version < 35) { Disable(typeof(AutoPatrolRune)); }
		if (version < 37) { Disable(typeof(SomethingForNothingRune), typeof(SoulCallingRune)); }
		if (version < 39) { Disable(typeof(GhostFormRune), typeof(DieForYouRune)); }
		return ids;
	}

	private static HashSet<string> LegacyMigrateDisabledMonsterHexIds(int version, IEnumerable<string> seed)
	{
		HashSet<string> ids = HextechRuneConfiguration.NormalizeDisabledMonsterHexIds(seed);
		if (version < 36)
		{
			ids.Add(MonsterHexKind.GetExcited.ToString());
		}

		if (version < 38)
		{
			ids.Add(MonsterHexKind.ShoulderVaku.ToString());
		}

		return ids;
	}

	private static void ReviewShareCodePreviewNormalizesLikeSave()
	{
		HextechRunConfigurationSnapshot defaults = HextechRuneConfiguration.GetDefaultSnapshot();
		HextechRunConfigurationSnapshot zeroWeights = defaults with
		{
			RuneRarityWeightsByAct = [ new(0, 0, 0), new(2, 3, 4), new(0, 0, 0) ],
			ForgeRarityWeights = new(0, 0, 0),
			ChaosRuneChancePercent = 250
		};
		string code = HextechConfigShareCodec.Export(zeroWeights);
		HextechConfigShareCodec.ImportPreview preview = HextechConfigShareCodec.TryParseForTests(code, defaults)
			?? throw new InvalidOperationException("share code should parse");
		HextechRunConfigurationSnapshot expected = HextechRuneConfiguration.NormalizeSnapshot(zeroWeights);
		SequenceEqual(expected.RuneRarityWeightsByAct, preview.Snapshot.RuneRarityWeightsByAct, "all-zero act weights fall back like save");
		Equal(expected.ForgeRarityWeights, preview.Snapshot.ForgeRarityWeights, "all-zero forge weights fall back like save");
		Equal(new HextechRarityWeights(2, 3, 4), preview.Snapshot.RuneRarityWeightsByAct[1], "valid act weights survive");
		Equal(100, preview.Snapshot.ChaosRuneChancePercent, "chaos chance clamped in preview");
		Equal(HextechRuneConfiguration.GetDefaultForgeRarityWeights(), preview.Snapshot.ForgeRarityWeights, "forge fallback is default");
	}

	private static void ReviewExternalRegistryRejectsBuiltInTypes()
	{
		int runeCount = HextechExternalContentRegistry.GetPlayerRuneRegistrations().Count;
		int forgeCount = HextechExternalContentRegistry.GetForgeRegistrations().Count;
		int version = HextechExternalContentRegistry.Version;

		// 测试进程没有 Godot 原生层,真实 Warn 在 0.107.1 会崩溃;先耗尽该告警的日志预算。
		Action restoreWarnings = SuppressCompatibilityWarnings("external-content.built-in-rejected");
		try
		{
			HextechExternalContentRegistry.RegisterPlayerRune(new PlayerRuneRegistration(typeof(SlapRune), HextechRarityTier.Gold), assetModId: null);
			HextechExternalContentRegistry.RegisterForge(new ForgeRegistration(typeof(StrengthForge), HextechRarityTier.Gold), assetModId: null);
		}
		finally
		{
			restoreWarnings();
		}

		Equal(runeCount, HextechExternalContentRegistry.GetPlayerRuneRegistrations().Count, "built-in rune not added to external list");
		Equal(forgeCount, HextechExternalContentRegistry.GetForgeRegistrations().Count, "built-in forge not added to external list");
		Equal(version, HextechExternalContentRegistry.Version, "rejected registration does not bump version");
		Equal(HextechRarityTier.Silver, HextechContentRegistry.PlayerRuneMetadata.GetRegistration(typeof(SlapRune)).Rarity, "built-in rune metadata retained and lookups still build");
		Expect(HextechContentRegistry.SilverForgeTypes.Contains(typeof(StrengthForge)), "built-in forge metadata retained");
	}

	private static void ReviewEnemyHexIconRelicTypesMatchMonsterHexRegistry()
	{
		HashSet<Type> playerRuneTypes = HextechPlayerRuneRegistry.Registrations.Select(static registration => registration.Type).ToHashSet();
		Type[] derived = HextechMonsterHexRegistry.Registrations
			.Select(static registration => registration.IconRelicType)
			.Where(type => !playerRuneTypes.Contains(type))
			.Distinct()
			.ToArray();
		SetEqual(derived, HextechCustomModelRegistry.EnemyHexIconRelicTypes, "enemy-only hex icon carriers must be listed in HextechCustomModelRegistry");
		Equal(HextechCustomModelRegistry.EnemyHexIconRelicTypes.Count, HextechCustomModelRegistry.EnemyHexIconRelicTypes.Distinct().Count(), "enemy hex icon carriers are unique");
	}

	private static void ReviewSyncedRuneOptionsRequireRegisteredPlayerRunes()
	{
		ModelId slap = ModelDb.GetId<SlapRune>();
		ModelId goldrend = ModelDb.GetId<GoldrendRune>();
		Expect(HextechRuneSelectionCoordinator.AreRegisteredPlayerRuneIds([ slap, goldrend ]), "registered player runes are accepted");
		Expect(!HextechRuneSelectionCoordinator.AreRegisteredPlayerRuneIds([ slap, ModelDb.GetId<Vajra>() ]), "vanilla relic in synced options is rejected");
		Expect(!HextechRuneSelectionCoordinator.AreRegisteredPlayerRuneIds([ ModelDb.GetId<StrengthForge>() ]), "forge in synced rune options is rejected");
		Expect(!HextechRuneSelectionCoordinator.AreRegisteredPlayerRuneIds([ new ModelId("HEXTECH_TEST", "UNKNOWN_RUNE") ]), "unknown id is rejected");
		Expect(!HextechRuneSelectionCoordinator.AreRegisteredPlayerRuneIds([]), "empty synced options are rejected");
	}

	private static void ReviewEnemyHexAdjustmentValidatesSlotsHexesAndRerolls()
	{
		MonsterHexKind[] initial = [ MonsterHexKind.Slap, MonsterHexKind.HeavyHitter ];
		MonsterHexKind[] candidates = [ MonsterHexKind.Tormentor, MonsterHexKind.Repulsor ];
		EnemyHexAdjustmentPayload Payload(MonsterHexKind?[] hexes, int[] rerolls)
		{
			return new EnemyHexAdjustmentPayload(0, 0, hexes, rerolls, IsFinal: false);
		}

		Expect(HextechRuneSelectionCoordinator.IsValidEnemyHexAdjustment(Payload([ MonsterHexKind.Slap, MonsterHexKind.Tormentor ], [ 0, 1 ]), initial, candidates, 1), "initial hex and reroll candidate within limit are valid");
		Expect(HextechRuneSelectionCoordinator.IsValidEnemyHexAdjustment(Payload([ MonsterHexKind.Repulsor, null ], [ 5, 9 ]), initial, candidates, HextechRuneConfiguration.InfiniteRerollLimit), "infinite limit accepts any reroll count");
		Expect(!HextechRuneSelectionCoordinator.IsValidEnemyHexAdjustment(Payload([ MonsterHexKind.Slap ], [ 0 ]), initial, candidates, 1), "slot count must match");
		Expect(!HextechRuneSelectionCoordinator.IsValidEnemyHexAdjustment(Payload([ MonsterHexKind.Slap, MonsterHexKind.HeavyHitter ], [ 0, 0, 0 ]), initial, candidates, 1), "reroll count list must match slots");
		Expect(!HextechRuneSelectionCoordinator.IsValidEnemyHexAdjustment(Payload([ MonsterHexKind.Slap, MonsterHexKind.CorruptHeart ], [ 0, 1 ]), initial, candidates, 1), "hex outside initial and reroll pool is rejected");
		Expect(!HextechRuneSelectionCoordinator.IsValidEnemyHexAdjustment(Payload([ MonsterHexKind.Slap, MonsterHexKind.Tormentor ], [ 0, 2 ]), initial, candidates, 1), "reroll count above limit is rejected");
	}

	private static void ReviewRelicChoiceCodecKeepsWireFormatForBothKinds()
	{
		const int Token = 424242;
		RelicModel[] options = CreateRuneSelectionTestOptions(3);
		(HextechRelicChoiceKind Kind, int MessageKind)[] kinds =
		[
			(HextechRelicChoiceKind.Forge, ChoiceKindForgeSelection),
			(HextechRelicChoiceKind.RelicOption, ChoiceKindRelicOptionSelection)
		];
		foreach ((HextechRelicChoiceKind kind, int messageKind) in kinds)
		{
			PlayerChoiceResult result = HextechChoiceCodec.CreateRelicChoice(kind, Token, 2, options);
			Expect(HextechChoiceCodec.TryGetIndexPayload(result, out List<int> payload), $"{kind} payload is an index list");
			SequenceEqual(new[] { Magic, messageKind, Token, 2, StableModelIdListVersion }, payload.Take(5), $"{kind} header layout");
			Expect(HextechChoiceCodec.TryDecodeRelicChoice(kind, result, Token, out int selectedIndex, out List<ModelId> ids), $"{kind} round trip decodes");
			Equal(2, selectedIndex, $"{kind} selected index");
			SequenceEqual(options.Select(static relic => relic.CanonicalId()), ids, $"{kind} option ids");
			Expect(HextechChoiceCodec.IsRelicChoice(kind, result, Token, options), $"{kind} matches same options");
			Expect(!HextechChoiceCodec.IsRelicChoice(kind, result, Token, options.Reverse().ToArray()), $"{kind} rejects reordered options");
			HextechRelicChoiceKind other = kind == HextechRelicChoiceKind.Forge ? HextechRelicChoiceKind.RelicOption : HextechRelicChoiceKind.Forge;
			Expect(!HextechChoiceCodec.TryDecodeRelicChoice(other, result, Token, out _, out _), $"{kind} payload is not decoded as {other}");
			Expect(!HextechChoiceCodec.IsMalformedRelicChoiceEnvelope(kind, result, Token), $"{kind} valid payload is not malformed");
		}

		// 旧入口与共享实现产出完全相同的载荷。
		SequenceEqual(
			Payload(HextechChoiceCodec.CreateRelicChoice(HextechRelicChoiceKind.Forge, Token, 0, options)),
			Payload(HextechChoiceCodec.CreateForgeSelection(Token, 0, options)),
			"forge wrapper payload");
		SequenceEqual(
			Payload(HextechChoiceCodec.CreateRelicChoice(HextechRelicChoiceKind.RelicOption, Token, 0, options)),
			Payload(HextechChoiceCodec.CreateRelicOptionSelection(Token, 0, options)),
			"relic option wrapper payload");

		static List<int> Payload(PlayerChoiceResult result)
		{
			return HextechChoiceCodec.TryGetIndexPayload(result, out List<int> payload) ? payload : [];
		}
	}

	private static void ReviewRuneSelectionTailParsersValidateHeader()
	{
		RelicModel[] options = [ new GeneratedTestRelic { Data = "recipe:a" }, new GeneratedTestRelic { Data = "recipe:b" } ];
		HextechWeightedRuneOptions weighted = new(options, 140);
		PlayerChoiceResult valid = HextechChoiceCodec.CreateRuneSelection(1, 0, 0, [ 1 ], weighted);
		Expect(HextechChoiceCodec.TryGetIndexPayload(valid, out List<int> payload), "rune selection payload");
		Expect(HextechChoiceCodec.TryReadRuneSelectionHeader(payload, out int cursor), "valid header parses");
		Equal(7, cursor, "final options start after header and one reroll entry");
		Expect(HextechRuneWeightCodec.TryRestore(valid, options, out List<RelicModel> restored), "weight restores from valid payload");
		Equal(140, HextechWeightedRuneOptions.GetWeight(restored), "restored weight");

		List<int> wrongKind = payload.ToList();
		wrongKind[1] = ChoiceKindForgeSelection;
		PlayerChoiceResult wrongKindResult = PlayerChoiceResult.FromIndexes(wrongKind);
		Expect(!HextechRuneWeightCodec.TryRestore(wrongKindResult, options, out _), "weight tail parser rejects non rune-selection kind");
		Expect(!HextechGeneratedRuneDataCodec.Restore(wrongKindResult, options), "recipe tail parser rejects non rune-selection kind");

		List<int> wrongMagic = payload.ToList();
		wrongMagic[0] = Magic + 1;
		Expect(!HextechRuneWeightCodec.TryRestore(PlayerChoiceResult.FromIndexes(wrongMagic), options, out _), "weight tail parser rejects wrong magic");
	}

	private static void ReviewChaosTransformResultIsValidated()
	{
		RelicModel slap = new SlapRune();
		RelicModel goldrend = new GoldrendRune();
		List<RelicModel> options = [ slap, goldrend ];
		Expect(HextechRuneGeneration.TryAcceptTransformResult(options, [ goldrend, slap ], out _), "same-size hextech result is accepted");
		Expect(!HextechRuneGeneration.TryAcceptTransformResult(options, null, out _), "null result is rejected");
		Expect(!HextechRuneGeneration.TryAcceptTransformResult(options, [ slap ], out _), "count change is rejected");
		Expect(!HextechRuneGeneration.TryAcceptTransformResult(options, [ slap, new Vajra() ], out _), "vanilla relic is rejected");
		Expect(!HextechRuneGeneration.TryAcceptTransformResult(options, [ slap, null! ], out _), "null entry is rejected");
	}
}

