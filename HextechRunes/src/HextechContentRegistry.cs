namespace HextechRunes;

internal static class HextechContentRegistry
{
    [Flags]
    private enum RuneFlags
    {
        None = 0,
        Disabled = 1,
        AttributeConversionExclusive = 2,
        FirstActExcluded = 4,
        ThirdActExcluded = 8
    }

    private enum HextechCharacterPool
    {
        Ironclad,
        Silent,
        Regent,
        Defect,
        Necrobinder
    }

    private readonly record struct RuneRegistration(
        Type Type,
        HextechRarityTier Rarity,
        RuneFlags Flags = RuneFlags.None,
        HextechCharacterPool? CharacterPool = null,
        int CharacterOrder = 0);

    private readonly record struct ForgeRegistration(Type Type, HextechRarityTier Rarity);

    private readonly record struct MonsterHexRegistration(
        MonsterHexKind Kind,
        HextechRarityTier Rarity,
        Type IconRelicType,
        bool Disabled = false,
        bool HasBurnHoverTip = false);

    private static readonly IReadOnlyList<RuneRegistration> RuneRegistrations =
    [
        Rune<SlapRune>(HextechRarityTier.Silver),
        Rune<DexterityToStrengthRune>(HextechRarityTier.Silver, flags: RuneFlags.AttributeConversionExclusive),
        Rune<StrengthToDexterityRune>(HextechRarityTier.Silver, flags: RuneFlags.AttributeConversionExclusive),
        Rune<DexterityStrengthToFocusRune>(HextechRarityTier.Silver, flags: RuneFlags.AttributeConversionExclusive, characterPool: HextechCharacterPool.Defect, characterOrder: 1),
        Rune<WizardlyThinkingRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Defect, characterOrder: 2),
        Rune<NimbleRune>(HextechRarityTier.Silver),
        Rune<EscapePlanRune>(HextechRarityTier.Silver, flags: RuneFlags.Disabled),
        Rune<BadTasteRune>(HextechRarityTier.Silver),
        Rune<FirstAidKitRune>(HextechRarityTier.Silver),
        Rune<SpeedDemonRune>(HextechRarityTier.Silver),
        Rune<HeavyHitterRune>(HextechRarityTier.Silver),
        Rune<BigStrengthRune>(HextechRarityTier.Silver),
        Rune<TormentorRune>(HextechRarityTier.Silver),
        Rune<AdamantRune>(HextechRarityTier.Silver),
        Rune<MountainSoulRune>(HextechRarityTier.Silver),
        Rune<FrostWraithRune>(HextechRarityTier.Silver),
        Rune<BadgeBrothersRune>(HextechRarityTier.Silver),
        Rune<HomeguardRune>(HextechRarityTier.Silver),
        Rune<SwiftAndSafeRune>(HextechRarityTier.Silver),
        Rune<SacrificeRune>(HextechRarityTier.Silver),
        Rune<ProtectiveVeilRune>(HextechRarityTier.Silver),
        Rune<RepulsorRune>(HextechRarityTier.Silver),
        Rune<LightEmUpRune>(HextechRarityTier.Silver),
        Rune<UltimateUnstoppableRune>(HextechRarityTier.Silver),
        Rune<ThornmailRune>(HextechRarityTier.Silver),
        Rune<ZealotRune>(HextechRarityTier.Silver),
        Rune<MindToMatterRune>(HextechRarityTier.Silver),
        Rune<StatsRune>(HextechRarityTier.Silver),
        Rune<StartupRoutineRune>(HextechRarityTier.Silver),
        Rune<CollectorRune>(HextechRarityTier.Silver),
        Rune<UnyieldingArmorRune>(HextechRarityTier.Silver),
        Rune<NightParadeRune>(HextechRarityTier.Silver),
        Rune<BloodPactRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Ironclad, characterOrder: 2),
        Rune<PlateletRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Ironclad, characterOrder: 3),
        Rune<SnakebiteRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Silent, characterOrder: 7),
        Rune<CatalystRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Silent, characterOrder: 10),
        Rune<SwordIntentRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Regent, characterOrder: 3),
        Rune<FlawlessRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Regent, characterOrder: 4),
        Rune<CondensedRadianceRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Regent, characterOrder: 5),
        Rune<RoyalCommandRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Regent, characterOrder: 10),
        Rune<ByproductRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Defect, characterOrder: 7),
        Rune<ElectricSurgeRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Defect, characterOrder: 9),
        Rune<SoulCallingRune>(HextechRarityTier.Silver),
        Rune<TauntRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 5),
        Rune<SwordsmanshipRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Regent, characterOrder: 9),
        Rune<AdvanceToRetreatRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Ironclad, characterOrder: 7),
        Rune<BoneGuardRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 12),
        Rune<PlasterRune>(HextechRarityTier.Silver, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 13),
        Rune<EasyDoesItRune>(HextechRarityTier.Silver),
        Rune<SweepingBladeRune>(HextechRarityTier.Silver),
        Rune<OceanDragonSoulRune>(HextechRarityTier.Silver),
        Rune<InfernalDragonSoulRune>(HextechRarityTier.Silver),
        Rune<HextechDragonSoulRune>(HextechRarityTier.Silver),
        Rune<CarefulSelectionRune>(HextechRarityTier.Silver),
        Rune<PowerShieldRune>(HextechRarityTier.Silver),
        Rune<CorrosionRune>(HextechRarityTier.Silver),
        Rune<TransmuteGoldRune>(HextechRarityTier.Silver),

        Rune<JudicatorRune>(HextechRarityTier.Gold),
        Rune<TranscendentEvilRune>(HextechRarityTier.Gold, flags: RuneFlags.ThirdActExcluded, characterPool: HextechCharacterPool.Defect, characterOrder: 3),
        Rune<TankEngineRune>(HextechRarityTier.Gold, flags: RuneFlags.ThirdActExcluded),
        Rune<AstralBodyRune>(HextechRarityTier.Gold, flags: RuneFlags.Disabled),
        Rune<AncientWineRune>(HextechRarityTier.Gold),
        Rune<HolyFireRune>(HextechRarityTier.Gold, flags: RuneFlags.Disabled),
        Rune<NoNonsenseRune>(HextechRarityTier.Gold, flags: RuneFlags.Disabled),
        Rune<SuperBrainRune>(HextechRarityTier.Gold),
        Rune<OverflowRune>(HextechRarityTier.Gold),
        Rune<SturdyRune>(HextechRarityTier.Gold),
        Rune<LoopRune>(HextechRarityTier.Gold),
        Rune<OkBoomerangRune>(HextechRarityTier.Gold),
        Rune<DivineInterventionRune>(HextechRarityTier.Gold),
        Rune<SonataRune>(HextechRarityTier.Gold),
        Rune<CuttingEdgeAlchemistRune>(HextechRarityTier.Gold),
        Rune<DevilsDanceRune>(HextechRarityTier.Gold),
        Rune<BeginningAndEndRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 3),
        Rune<KeystoneHunterRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Silent, characterOrder: 1),
        Rune<WarmogsSpiritRune>(HextechRarityTier.Gold),
        Rune<RedEnvelopeRune>(HextechRarityTier.Gold),
        Rune<MindPurificationRune>(HextechRarityTier.Gold, flags: RuneFlags.Disabled),
        Rune<EndlessRecoveryRune>(HextechRarityTier.Gold),
        Rune<SpeedsterRune>(HextechRarityTier.Gold),
        Rune<ServantMasterRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 1),
        Rune<SoulEaterRune>(HextechRarityTier.Gold),
        Rune<DonationRune>(HextechRarityTier.Gold),
        Rune<TwiceThriceRune>(HextechRarityTier.Gold),
        Rune<BreadAndButterRune>(HextechRarityTier.Gold),
        Rune<BreadAndCheeseRune>(HextechRarityTier.Gold),
        Rune<BreadAndJamRune>(HextechRarityTier.Gold),
        Rune<FirebrandRune>(HextechRarityTier.Gold),
        Rune<NightstalkingRune>(HextechRarityTier.Gold),
        Rune<GetExcitedRune>(HextechRarityTier.Gold),
        Rune<ShrinkEngineRune>(HextechRarityTier.Gold, flags: RuneFlags.ThirdActExcluded),
        Rune<StatsOnStatsRune>(HextechRarityTier.Gold),
        Rune<LifeFlowRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Ironclad, characterOrder: 1),
        Rune<RekindleRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Ironclad, characterOrder: 4),
        Rune<TrickLicenseRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Silent, characterOrder: 2),
        Rune<GalacticGiftRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Regent, characterOrder: 1),
        Rune<SomethingFromNothingRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 2),
        Rune<LubricantRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Defect, characterOrder: 4),
        Rune<HubrisRune>(HextechRarityTier.Gold, flags: RuneFlags.ThirdActExcluded),
        Rune<DrainRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 4),
        Rune<LethalTempoRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Silent, characterOrder: 3),
        Rune<EmergenceRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Defect, characterOrder: 5),
        Rune<MirageRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Silent, characterOrder: 5),
        Rune<AdaptiveCapacitorRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Defect, characterOrder: 8),
        Rune<RenewalRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Silent, characterOrder: 4),
        Rune<WraithRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 6),
        Rune<SummonForthRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Regent, characterOrder: 2),
        Rune<ImmortalBoneRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 7),
        Rune<MakeItMineRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 8),
        Rune<DoomsdayRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 14),
        Rune<OldIdolRune>(HextechRarityTier.Gold),
        Rune<MonarchsGazeRune>(HextechRarityTier.Gold),
        Rune<HardBonesRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 11),
        Rune<SendThemInRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Regent, characterOrder: 8),
        Rune<ChainInSleeveRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Silent, characterOrder: 9),
        Rune<RoyalTrialRune>(HextechRarityTier.Gold, characterPool: HextechCharacterPool.Regent, characterOrder: 11),
        Rune<GoodLuckRune>(HextechRarityTier.Gold),
        Rune<RadianceRune>(HextechRarityTier.Gold),
        Rune<ManipulateRealityRune>(HextechRarityTier.Gold),
        Rune<TransmutePrismaticRune>(HextechRarityTier.Gold),
        Rune<DawnbringersResolveRune>(HextechRarityTier.Gold, flags: RuneFlags.Disabled),
        Rune<ShrinkRayRune>(HextechRarityTier.Gold),

        Rune<EurekaRune>(HextechRarityTier.Prismatic),
        Rune<InfiniteLoopRune>(HextechRarityTier.Prismatic, flags: RuneFlags.ThirdActExcluded),
        Rune<SlowCookRune>(HextechRarityTier.Prismatic),
        Rune<GiantSlayerRune>(HextechRarityTier.Prismatic),
        Rune<CourageOfColossusRune>(HextechRarityTier.Prismatic),
        Rune<GlassCannonRune>(HextechRarityTier.Prismatic),
        Rune<FinalFormRune>(HextechRarityTier.Prismatic),
        Rune<BackToBasicsRune>(HextechRarityTier.Prismatic),
        Rune<DrawYourSwordRune>(HextechRarityTier.Prismatic),
        Rune<FeelTheBurnRune>(HextechRarityTier.Prismatic, flags: RuneFlags.Disabled),
        Rune<MikaelsBlessingRune>(HextechRarityTier.Prismatic, flags: RuneFlags.Disabled),
        Rune<EarthAwakensRune>(HextechRarityTier.Prismatic),
        Rune<SymphonyOfWarRune>(HextechRarityTier.Prismatic),
        Rune<UnmovableMountainRune>(HextechRarityTier.Prismatic),
        Rune<MysteryRune>(HextechRarityTier.Prismatic),
        Rune<MadScientistRune>(HextechRarityTier.Prismatic),
        Rune<JeweledGauntletRune>(HextechRarityTier.Prismatic),
        Rune<HailToTheKingRune>(HextechRarityTier.Prismatic, flags: RuneFlags.ThirdActExcluded),
        Rune<ArcanePunchRune>(HextechRarityTier.Prismatic),
        Rune<PandorasBoxRune>(HextechRarityTier.Prismatic, flags: RuneFlags.FirstActExcluded),
        Rune<TapDanceRune>(HextechRarityTier.Prismatic),
        Rune<InfernalConduitRune>(HextechRarityTier.Prismatic),
        Rune<DualWieldRune>(HextechRarityTier.Prismatic),
        Rune<GoliathRune>(HextechRarityTier.Prismatic),
        Rune<MasterOfDualityRune>(HextechRarityTier.Prismatic),
        Rune<HandOfBaronRune>(HextechRarityTier.Prismatic),
        Rune<CantTouchThisRune>(HextechRarityTier.Prismatic),
        Rune<QueenRune>(HextechRarityTier.Prismatic),
        Rune<UltimateRefreshRune>(HextechRarityTier.Prismatic),
        Rune<GoldrendRune>(HextechRarityTier.Prismatic),
        Rune<CerberusRune>(HextechRarityTier.Prismatic),
        Rune<CircleOfDeathRune>(HextechRarityTier.Prismatic),
        Rune<FanTheHammerRune>(HextechRarityTier.Prismatic),
        Rune<FeyMagicRune>(HextechRarityTier.Prismatic),
        Rune<WatchOutGrapefruitRune>(HextechRarityTier.Prismatic),
        Rune<ProteinShakeRune>(HextechRarityTier.Prismatic),
        Rune<StatsOnStatsOnStatsRune>(HextechRarityTier.Prismatic),
        Rune<GoldenSpatulaRune>(HextechRarityTier.Prismatic),
        Rune<PrecisionCognitionRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Defect, characterOrder: 6),
        Rune<HastyScribbleRune>(HextechRarityTier.Prismatic),
        Rune<ClownCollegeRune>(HextechRarityTier.Prismatic),
        Rune<BladeWaltzRune>(HextechRarityTier.Prismatic),
        Rune<SingularityAIRune>(HextechRarityTier.Prismatic),
        Rune<EightPennyGateRune>(HextechRarityTier.Prismatic),
        Rune<GrowingStrongerRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Ironclad, characterOrder: 5),
        Rune<GroundedRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Ironclad, characterOrder: 6),
        Rune<KillerHunterRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Silent, characterOrder: 6),
        Rune<SerpentsFangRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Silent, characterOrder: 8),
        Rune<ExplosionArtRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Regent, characterOrder: 6),
        Rune<StarlightSplendorRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Regent, characterOrder: 7),
        Rune<MiserableFateRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 9),
        Rune<DieForYouRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Necrobinder, characterOrder: 10),
        Rune<HappyAccidentRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Defect, characterOrder: 10),
        Rune<MiseryRune>(HextechRarityTier.Prismatic),
        Rune<GhostFormRune>(HextechRarityTier.Prismatic),
        Rune<ForbiddenGrimoireRune>(HextechRarityTier.Prismatic),
        Rune<OneLaneBridgeRune>(HextechRarityTier.Prismatic),
        Rune<OrbSymbiosisRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Defect, characterOrder: 11),
        Rune<NearDeathFeastRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Ironclad, characterOrder: 8),
        Rune<UnsealedThroneRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Regent, characterOrder: 12),
        Rune<CoreOverloadRune>(HextechRarityTier.Prismatic, characterPool: HextechCharacterPool.Defect, characterOrder: 12),
        Rune<ForgottenSoulRune>(HextechRarityTier.Prismatic),
        Rune<UpgradeRune>(HextechRarityTier.Prismatic),
        Rune<OmniDragonSoulRune>(HextechRarityTier.Prismatic),
        Rune<TransmuteChaosRune>(HextechRarityTier.Prismatic)
    ];

    private static readonly IReadOnlyList<ForgeRegistration> ForgeRegistrations =
    [
        Forge<StrengthForge>(HextechRarityTier.Silver),
        Forge<DexterityForge>(HextechRarityTier.Silver),
        Forge<SilverPlatingForge>(HextechRarityTier.Silver),
        Forge<UpgradeForge>(HextechRarityTier.Silver),
        Forge<FocusForge>(HextechRarityTier.Silver),
        Forge<LifeForge>(HextechRarityTier.Silver),
        Forge<PreparedForge>(HextechRarityTier.Silver),
        Forge<NecrobinderForge>(HextechRarityTier.Silver),
        Forge<SilverStarsForge>(HextechRarityTier.Silver),
        Forge<SilverOrbForge>(HextechRarityTier.Silver),

        Forge<ConstitutionForge>(HextechRarityTier.Gold),
        Forge<DisasterForge>(HextechRarityTier.Gold),
        Forge<GoldLifeForge>(HextechRarityTier.Gold),
        Forge<GoldFocusForge>(HextechRarityTier.Gold),
        Forge<DrawForge>(HextechRarityTier.Gold),
        Forge<GoldUpgradeForge>(HextechRarityTier.Gold),
        Forge<StarsForge>(HextechRarityTier.Gold),
        Forge<OrbSlotForge>(HextechRarityTier.Gold),
        Forge<PlatingForge>(HextechRarityTier.Gold),
        Forge<ThornsForge>(HextechRarityTier.Gold),
        Forge<ArtifactForge>(HextechRarityTier.Gold),

        Forge<PrismaticLifeForge>(HextechRarityTier.Prismatic),
        Forge<AttackForge>(HextechRarityTier.Prismatic),
        Forge<ProtectionForge>(HextechRarityTier.Prismatic),
        Forge<EnergyForge>(HextechRarityTier.Prismatic),
        Forge<RitualForge>(HextechRarityTier.Prismatic),
        Forge<RegenForge>(HextechRarityTier.Prismatic),
        Forge<BufferForge>(HextechRarityTier.Prismatic),
        Forge<SlipperyForge>(HextechRarityTier.Prismatic),
        Forge<PrismaticArtifactForge>(HextechRarityTier.Prismatic),
        Forge<GhostForge>(HextechRarityTier.Prismatic),
        Forge<FortuneForge>(HextechRarityTier.Prismatic)
    ];

    private static readonly IReadOnlyList<MonsterHexRegistration> MonsterHexRegistrations =
    [
        Monster<SlapRune>(MonsterHexKind.Slap, HextechRarityTier.Silver),
        Monster<EscapePlanRune>(MonsterHexKind.EscapePlan, HextechRarityTier.Silver),
        Monster<HeavyHitterRune>(MonsterHexKind.HeavyHitter, HextechRarityTier.Silver),
        Monster<BigStrengthRune>(MonsterHexKind.BigStrength, HextechRarityTier.Silver),
        Monster<TormentorRune>(MonsterHexKind.Tormentor, HextechRarityTier.Silver, hasBurnHoverTip: true),
        Monster<ProtectiveVeilRune>(MonsterHexKind.ProtectiveVeil, HextechRarityTier.Silver),
        Monster<RepulsorRune>(MonsterHexKind.Repulsor, HextechRarityTier.Silver),
        Monster<ThornmailRune>(MonsterHexKind.Thornmail, HextechRarityTier.Silver),
        Monster<LightEmUpRune>(MonsterHexKind.LightEmUp, HextechRarityTier.Silver),
        Monster<MountainSoulRune>(MonsterHexKind.MountainSoul, HextechRarityTier.Silver),
        Monster<FirstAidKitRune>(MonsterHexKind.FirstAidKit, HextechRarityTier.Silver),
        Monster<SpeedDemonRune>(MonsterHexKind.SpeedDemon, HextechRarityTier.Silver),
        Monster<FrostWraithRune>(MonsterHexKind.FrostWraith, HextechRarityTier.Silver),
        Monster<BloodPactRune>(MonsterHexKind.BloodPact, HextechRarityTier.Silver),
        Monster<StartupRoutineRune>(MonsterHexKind.StartupRoutine, HextechRarityTier.Silver),

        Monster<SturdyRune>(MonsterHexKind.Sturdy, HextechRarityTier.Gold),
        Monster<DawnbringersResolveRune>(MonsterHexKind.DawnbringersResolve, HextechRarityTier.Gold),
        Monster<ShrinkRayRune>(MonsterHexKind.ShrinkRay, HextechRarityTier.Gold),
        Monster<FirebrandRune>(MonsterHexKind.Firebrand, HextechRarityTier.Gold, hasBurnHoverTip: true),
        Monster<SuperBrainRune>(MonsterHexKind.SuperBrain, HextechRarityTier.Gold),
        Monster<NightstalkingRune>(MonsterHexKind.Nightstalking, HextechRarityTier.Gold),
        Monster<AstralBodyRune>(MonsterHexKind.AstralBody, HextechRarityTier.Gold),
        Monster<TankEngineRune>(MonsterHexKind.TankEngine, HextechRarityTier.Gold),
        Monster<ShrinkEngineRune>(MonsterHexKind.ShrinkEngine, HextechRarityTier.Gold),
        Monster<GetExcitedRune>(MonsterHexKind.GetExcited, HextechRarityTier.Gold),
        Monster<TwiceThriceRune>(MonsterHexKind.TwiceThrice, HextechRarityTier.Gold),
        Monster<LoopRune>(MonsterHexKind.Loop, HextechRarityTier.Gold),
        Monster<ServantMasterRune>(MonsterHexKind.ServantMaster, HextechRarityTier.Gold),
        Monster<CuttingEdgeAlchemistRune>(MonsterHexKind.CuttingEdgeAlchemist, HextechRarityTier.Gold),
        Monster<DivineInterventionRune>(MonsterHexKind.DivineIntervention, HextechRarityTier.Gold),
        Monster<SonataRune>(MonsterHexKind.Sonata, HextechRarityTier.Gold),
        Monster<DevilsDanceRune>(MonsterHexKind.DevilsDance, HextechRarityTier.Gold),
        Monster<ImmortalBoneRune>(MonsterHexKind.ImmortalBone, HextechRarityTier.Gold),
        Monster<DoomsdayRune>(MonsterHexKind.Doomsday, HextechRarityTier.Gold),
        Monster<WarmogsSpiritRune>(MonsterHexKind.WarmogsSpirit, HextechRarityTier.Gold),

        Monster<CourageOfColossusRune>(MonsterHexKind.CourageOfColossus, HextechRarityTier.Prismatic),
        Monster<GlassCannonRune>(MonsterHexKind.GlassCannon, HextechRarityTier.Prismatic),
        Monster<GoliathRune>(MonsterHexKind.Goliath, HextechRarityTier.Prismatic),
        Monster<QueenRune>(MonsterHexKind.Queen, HextechRarityTier.Prismatic),
        Monster<HandOfBaronRune>(MonsterHexKind.HandOfBaron, HextechRarityTier.Prismatic),
        Monster<CantTouchThisRune>(MonsterHexKind.CantTouchThis, HextechRarityTier.Prismatic),
        Monster<MasterOfDualityRune>(MonsterHexKind.MasterOfDuality, HextechRarityTier.Prismatic),
        Monster<GoldrendRune>(MonsterHexKind.Goldrend, HextechRarityTier.Prismatic),
        Monster<FeelTheBurnRune>(MonsterHexKind.FeelTheBurn, HextechRarityTier.Prismatic, hasBurnHoverTip: true),
        Monster<BackToBasicsRune>(MonsterHexKind.BackToBasics, HextechRarityTier.Prismatic),
        Monster<DrawYourSwordRune>(MonsterHexKind.DrawYourSword, HextechRarityTier.Prismatic, disabled: true),
        Monster<MadScientistRune>(MonsterHexKind.MadScientist, HextechRarityTier.Prismatic),
        Monster<FeyMagicRune>(MonsterHexKind.FeyMagic, HextechRarityTier.Prismatic),
        Monster<FinalFormRune>(MonsterHexKind.FinalForm, HextechRarityTier.Prismatic),
        Monster<UnmovableMountainRune>(MonsterHexKind.UnmovableMountain, HextechRarityTier.Prismatic),
        Monster<MikaelsBlessingRune>(MonsterHexKind.MikaelsBlessing, HextechRarityTier.Prismatic),
        Monster<ClownCollegeRune>(MonsterHexKind.ClownCollege, HextechRarityTier.Prismatic),
        Monster<SingularityAIRune>(MonsterHexKind.SingularityAI, HextechRarityTier.Prismatic),
        Monster<ProteinShakeRune>(MonsterHexKind.ProteinShake, HextechRarityTier.Prismatic),
        Monster<GoldenSpatulaRune>(MonsterHexKind.GoldenSpatula, HextechRarityTier.Prismatic),
        Monster<HailToTheKingRune>(MonsterHexKind.HailToTheKing, HextechRarityTier.Prismatic),
        Monster<EightPennyGateRune>(MonsterHexKind.EightPennyGate, HextechRarityTier.Prismatic),
        Monster<HastyScribbleRune>(MonsterHexKind.HastyScribble, HextechRarityTier.Prismatic)
    ];

    internal static readonly IReadOnlyList<Type> SilverRuneTypes = RuneTypesForRarity(HextechRarityTier.Silver);

    internal static readonly IReadOnlyList<Type> GoldRuneTypes = RuneTypesForRarity(HextechRarityTier.Gold);

    internal static readonly IReadOnlyList<Type> PrismaticRuneTypes = RuneTypesForRarity(HextechRarityTier.Prismatic);

    internal static readonly IReadOnlyList<Type> SilverForgeTypes = ForgeTypesForRarity(HextechRarityTier.Silver);

    internal static readonly IReadOnlyList<Type> GoldForgeTypes = ForgeTypesForRarity(HextechRarityTier.Gold);

    internal static readonly IReadOnlyList<Type> PrismaticForgeTypes = ForgeTypesForRarity(HextechRarityTier.Prismatic);

    internal static readonly IReadOnlyList<Type> ShopOnlyRelicTypes =
    [
        typeof(RandomForgeShopRelic)
    ];

    internal static readonly IReadOnlyList<Type> CustomCardTypes =
    [
        typeof(ElicitCard),
        typeof(TrickMagicCard),
        typeof(BladeWaltzCard),
        typeof(CatalystCard),
        typeof(OceanDragonSoulCard),
        typeof(InfernalDragonSoulCard),
        typeof(HextechDragonSoulCard),
        typeof(MountainDragonSoulCard),
        typeof(ChemtechDragonSoulCard),
        typeof(CloudDragonSoulCard)
    ];

    internal static readonly IReadOnlySet<Type> DisabledPlayerRuneTypes = RuneTypesWithFlag(RuneFlags.Disabled).ToHashSet();

    internal static readonly IReadOnlyList<Type> IroncladRuneTypes = RuneTypesForCharacter(HextechCharacterPool.Ironclad);

    internal static readonly IReadOnlyList<Type> SilentRuneTypes = RuneTypesForCharacter(HextechCharacterPool.Silent);

    internal static readonly IReadOnlyList<Type> RegentRuneTypes = RuneTypesForCharacter(HextechCharacterPool.Regent);

    internal static readonly IReadOnlyList<Type> DefectRuneTypes = RuneTypesForCharacter(HextechCharacterPool.Defect);

    internal static readonly IReadOnlyList<Type> NecrobinderRuneTypes = RuneTypesForCharacter(HextechCharacterPool.Necrobinder);

    internal static readonly IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes = RuneTypesWithFlag(RuneFlags.AttributeConversionExclusive);

    internal static readonly IReadOnlySet<Type> FirstActExcludedRuneTypes = RuneTypesWithFlag(RuneFlags.FirstActExcluded).ToHashSet();

    internal static readonly IReadOnlySet<Type> ThirdActExcludedRuneTypes = RuneTypesWithFlag(RuneFlags.ThirdActExcluded).ToHashSet();

    internal static readonly IReadOnlySet<MonsterHexKind> DisabledMonsterHexes = MonsterHexRegistrations
        .Where(static registration => registration.Disabled)
        .Select(static registration => registration.Kind)
        .ToHashSet();

    internal static readonly IReadOnlySet<MonsterHexKind> MonsterHexesWithBurnHoverTip = MonsterHexRegistrations
        .Where(static registration => registration.HasBurnHoverTip)
        .Select(static registration => registration.Kind)
        .ToHashSet();

    internal static readonly IReadOnlyDictionary<MonsterHexKind, Type> MonsterHexIconRelicTypes = MonsterHexRegistrations
        .ToDictionary(static registration => registration.Kind, static registration => registration.IconRelicType);

    internal static readonly IReadOnlyList<MonsterHexKind> SilverMonsterHexes = MonsterHexesForRarity(HextechRarityTier.Silver);

    internal static readonly IReadOnlyList<MonsterHexKind> GoldMonsterHexes = MonsterHexesForRarity(HextechRarityTier.Gold);

    internal static readonly IReadOnlyList<MonsterHexKind> PrismaticMonsterHexes = MonsterHexesForRarity(HextechRarityTier.Prismatic);

    internal static readonly IReadOnlyList<Type> AllRuneTypes = SilverRuneTypes
        .Concat(GoldRuneTypes)
        .Concat(PrismaticRuneTypes)
        .Distinct()
        .ToArray();

    internal static readonly IReadOnlyList<Type> AllForgeTypes = SilverForgeTypes
        .Concat(GoldForgeTypes)
        .Concat(PrismaticForgeTypes)
        .Distinct()
        .ToArray();

    internal static readonly IReadOnlyList<Type> AllCustomRelicTypes = AllRuneTypes
        .Concat(AllForgeTypes)
        .Concat(ShopOnlyRelicTypes)
        .Distinct()
        .ToArray();

    private static RuneRegistration Rune<TRune>(
        HextechRarityTier rarity,
        RuneFlags flags = RuneFlags.None,
        HextechCharacterPool? characterPool = null,
        int characterOrder = 0)
    {
        return new RuneRegistration(typeof(TRune), rarity, flags, characterPool, characterOrder);
    }

    private static ForgeRegistration Forge<TForge>(HextechRarityTier rarity)
    {
        return new ForgeRegistration(typeof(TForge), rarity);
    }

    private static MonsterHexRegistration Monster<TRelic>(
        MonsterHexKind kind,
        HextechRarityTier rarity,
        bool disabled = false,
        bool hasBurnHoverTip = false)
    {
        return new MonsterHexRegistration(kind, rarity, typeof(TRelic), disabled, hasBurnHoverTip);
    }

    private static IReadOnlyList<Type> RuneTypesForRarity(HextechRarityTier rarity)
    {
        return RuneRegistrations
            .Where(registration => registration.Rarity == rarity)
            .Select(static registration => registration.Type)
            .ToArray();
    }

    private static IReadOnlyList<Type> ForgeTypesForRarity(HextechRarityTier rarity)
    {
        return ForgeRegistrations
            .Where(registration => registration.Rarity == rarity)
            .Select(static registration => registration.Type)
            .ToArray();
    }

    private static IReadOnlyList<Type> RuneTypesForCharacter(HextechCharacterPool characterPool)
    {
        return RuneRegistrations
            .Where(registration => registration.CharacterPool == characterPool)
            .OrderBy(static registration => registration.CharacterOrder)
            .Select(static registration => registration.Type)
            .ToArray();
    }

    private static IReadOnlyList<Type> RuneTypesWithFlag(RuneFlags flag)
    {
        return RuneRegistrations
            .Where(registration => (registration.Flags & flag) != 0)
            .Select(static registration => registration.Type)
            .ToArray();
    }

    private static IReadOnlyList<MonsterHexKind> MonsterHexesForRarity(HextechRarityTier rarity)
    {
        return MonsterHexRegistrations
            .Where(registration => registration.Rarity == rarity && !registration.Disabled)
            .Select(static registration => registration.Kind)
            .ToArray();
    }
}
