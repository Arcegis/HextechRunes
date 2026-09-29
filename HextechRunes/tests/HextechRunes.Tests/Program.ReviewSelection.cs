global using HextechForgeRarityWeights = HextechRunes.HextechRarityWeights;
using MegaCrit.Sts2.Core.Models;

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
}
