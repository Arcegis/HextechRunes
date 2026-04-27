namespace HextechRunes;

internal static class HextechContentRegistry
{
    internal static readonly IReadOnlyList<Type> SilverRuneTypes =
    [
        typeof(SlapRune),
        typeof(DexterityToStrengthRune),
        typeof(StrengthToDexterityRune),
        typeof(DexterityStrengthToFocusRune),
        typeof(WizardlyThinkingRune),
        typeof(NimbleRune),
        typeof(EscapePlanRune),
        typeof(BadTasteRune),
        typeof(FirstAidKitRune),
        typeof(SpeedDemonRune),
        typeof(HeavyHitterRune),
        typeof(BigStrengthRune),
        typeof(TormentorRune),
        typeof(AdamantRune),
        typeof(MountainSoulRune),
        typeof(FrostWraithRune),
        typeof(BadgeBrothersRune),
        typeof(HomeguardRune),
        typeof(SwiftAndSafeRune),
        typeof(SacrificeRune),
        typeof(ProtectiveVeilRune),
        typeof(RepulsorRune),
        typeof(LightEmUpRune),
        typeof(UltimateUnstoppableRune),
        typeof(ThornmailRune),
        typeof(ZealotRune),
        typeof(MindToMatterRune),
        typeof(StatsRune),
        typeof(StartupRoutineRune),
        typeof(CollectorRune),
        typeof(TransmuteGoldRune)
    ];

    internal static readonly IReadOnlyList<Type> GoldRuneTypes =
    [
        typeof(JudicatorRune),
        typeof(TranscendentEvilRune),
        typeof(TankEngineRune),
        typeof(AstralBodyRune),
        typeof(AncientWineRune),
        typeof(HolyFireRune),
        typeof(NoNonsenseRune),
        typeof(SuperBrainRune),
        typeof(OverflowRune),
        typeof(SturdyRune),
        typeof(LoopRune),
        typeof(OkBoomerangRune),
        typeof(DivineInterventionRune),
        typeof(SonataRune),
        typeof(CuttingEdgeAlchemistRune),
        typeof(DevilsDanceRune),
        typeof(BeginningAndEndRune),
        typeof(KeystoneHunterRune),
        typeof(WarmogsSpiritRune),
        typeof(RedEnvelopeRune),
        typeof(MindPurificationRune),
        typeof(EndlessRecoveryRune),
        typeof(SpeedsterRune),
        typeof(ServantMasterRune),
        typeof(SoulEaterRune),
        typeof(DonationRune),
        typeof(TwiceThriceRune),
        typeof(FirebrandRune),
        typeof(NightstalkingRune),
        typeof(GetExcitedRune),
        typeof(ShrinkEngineRune),
        typeof(StatsOnStatsRune),
        typeof(LifeFlowRune),
        typeof(TrickLicenseRune),
        typeof(GalacticGiftRune),
        typeof(SomethingFromNothingRune),
        typeof(LubricantRune),
        typeof(HubrisRune),
        typeof(TransmutePrismaticRune),
        typeof(DawnbringersResolveRune),
        typeof(ShrinkRayRune)
    ];

    internal static readonly IReadOnlyList<Type> PrismaticRuneTypes =
    [
        typeof(EurekaRune),
        typeof(InfiniteLoopRune),
        typeof(SlowCookRune),
        typeof(GiantSlayerRune),
        typeof(CourageOfColossusRune),
        typeof(GlassCannonRune),
        typeof(FinalFormRune),
        typeof(BackToBasicsRune),
        typeof(DrawYourSwordRune),
        typeof(FeelTheBurnRune),
        typeof(MikaelsBlessingRune),
        typeof(EarthAwakensRune),
        typeof(SymphonyOfWarRune),
        typeof(UnmovableMountainRune),
        typeof(MysteryRune),
        typeof(MadScientistRune),
        typeof(JeweledGauntletRune),
        typeof(HailToTheKingRune),
        typeof(ArcanePunchRune),
        typeof(PandorasBoxRune),
        typeof(TapDanceRune),
        typeof(InfernalConduitRune),
        typeof(DualWieldRune),
        typeof(GoliathRune),
        typeof(MasterOfDualityRune),
        typeof(HandOfBaronRune),
        typeof(CantTouchThisRune),
        typeof(QueenRune),
        typeof(UltimateRefreshRune),
        typeof(GoldrendRune),
        typeof(CerberusRune),
        typeof(CircleOfDeathRune),
        typeof(FanTheHammerRune),
        typeof(FeyMagicRune),
        typeof(WatchOutGrapefruitRune),
        typeof(ProteinShakeRune),
        typeof(StatsOnStatsOnStatsRune),
        typeof(GoldenSpatulaRune),
        typeof(TransmuteChaosRune)
    ];

    internal static readonly IReadOnlyList<Type> SilverForgeTypes =
    [
        typeof(StrengthForge),
        typeof(DexterityForge),
        typeof(SilverPlatingForge),
        typeof(UpgradeForge),
        typeof(FocusForge),
        typeof(LifeForge),
        typeof(PreparedForge)
    ];

    internal static readonly IReadOnlyList<Type> GoldForgeTypes =
    [
        typeof(ConstitutionForge),
        typeof(GoldLifeForge),
        typeof(GoldFocusForge),
        typeof(DrawForge),
        typeof(GoldUpgradeForge),
        typeof(StarsForge),
        typeof(OrbSlotForge),
        typeof(PlatingForge),
        typeof(ThornsForge),
        typeof(ArtifactForge)
    ];

    internal static readonly IReadOnlyList<Type> PrismaticForgeTypes =
    [
        typeof(PrismaticLifeForge),
        typeof(AttackForge),
        typeof(ProtectionForge),
        typeof(EnergyForge),
        typeof(RitualForge),
        typeof(RegenForge),
        typeof(BufferForge),
        typeof(SlipperyForge),
        typeof(PrismaticArtifactForge),
        typeof(GhostForge)
    ];

    internal static readonly IReadOnlyList<Type> ShopOnlyRelicTypes =
    [
        typeof(RandomForgeShopRelic)
    ];

    internal static readonly IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes =
    [
        typeof(DexterityToStrengthRune),
        typeof(StrengthToDexterityRune),
        typeof(DexterityStrengthToFocusRune)
    ];

    internal static readonly IReadOnlySet<Type> FirstActExcludedRuneTypes = new HashSet<Type>
    {
        typeof(PandorasBoxRune)
    };

    internal static readonly IReadOnlySet<Type> ThirdActExcludedRuneTypes = new HashSet<Type>
    {
        typeof(TranscendentEvilRune),
        typeof(TankEngineRune),
        typeof(HubrisRune),
        typeof(ShrinkEngineRune),
        typeof(InfiniteLoopRune),
        typeof(HailToTheKingRune)
    };

    internal static readonly IReadOnlyList<MonsterHexKind> SilverMonsterHexes =
    [
        MonsterHexKind.Slap,
        MonsterHexKind.EscapePlan,
        MonsterHexKind.HeavyHitter,
        MonsterHexKind.BigStrength,
        MonsterHexKind.Tormentor,
        MonsterHexKind.ProtectiveVeil,
        MonsterHexKind.Repulsor,
        MonsterHexKind.Thornmail,
        MonsterHexKind.LightEmUp,
        MonsterHexKind.MountainSoul,
        MonsterHexKind.FirstAidKit,
        MonsterHexKind.SpeedDemon,
        MonsterHexKind.FrostWraith
    ];

    internal static readonly IReadOnlyList<MonsterHexKind> GoldMonsterHexes =
    [
        MonsterHexKind.Sturdy,
        MonsterHexKind.DawnbringersResolve,
        MonsterHexKind.ShrinkRay,
        MonsterHexKind.Firebrand,
        MonsterHexKind.SuperBrain,
        MonsterHexKind.Nightstalking,
        MonsterHexKind.AstralBody,
        MonsterHexKind.TankEngine,
        MonsterHexKind.ShrinkEngine,
        MonsterHexKind.GetExcited,
        MonsterHexKind.TwiceThrice,
        MonsterHexKind.Loop,
        MonsterHexKind.ServantMaster,
        MonsterHexKind.CuttingEdgeAlchemist,
        MonsterHexKind.DivineIntervention,
        MonsterHexKind.Sonata,
        MonsterHexKind.DevilsDance
    ];

    internal static readonly IReadOnlyList<MonsterHexKind> PrismaticMonsterHexes =
    [
        MonsterHexKind.CourageOfColossus,
        MonsterHexKind.GlassCannon,
        MonsterHexKind.Goliath,
        MonsterHexKind.Queen,
        MonsterHexKind.HandOfBaron,
        MonsterHexKind.CantTouchThis,
        MonsterHexKind.MasterOfDuality,
        MonsterHexKind.Goldrend,
        MonsterHexKind.FeelTheBurn,
        MonsterHexKind.BackToBasics,
        MonsterHexKind.MadScientist,
        MonsterHexKind.FeyMagic,
        MonsterHexKind.FinalForm,
        MonsterHexKind.UnmovableMountain,
        MonsterHexKind.MikaelsBlessing
    ];

    internal static readonly IReadOnlyList<Type> AllRuneTypes = SilverRuneTypes
        .Concat(GoldRuneTypes)
        .Concat(PrismaticRuneTypes)
        .ToArray();

    internal static readonly IReadOnlyList<Type> AllForgeTypes = SilverForgeTypes
        .Concat(GoldForgeTypes)
        .Concat(PrismaticForgeTypes)
        .ToArray();

    internal static readonly IReadOnlyList<Type> AllCustomRelicTypes = AllRuneTypes
        .Concat(AllForgeTypes)
        .Concat(ShopOnlyRelicTypes)
        .ToArray();
}
