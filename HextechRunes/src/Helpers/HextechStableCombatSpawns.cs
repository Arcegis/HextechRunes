namespace HextechRunes;

/// <summary>
/// 几个符文/锻造共用的"稳定下标 → 固定候选表"映射：随从牌（KingdomArmy、SendThemIn、RoyalTrial 符文）
/// 与充能球（HappyAccident、MarkovBabble、Emergence 符文与 SilverOrbForge）。下标全部来自 <see cref="HextechStableRandom.Index"/>，
/// 不引入新的随机来源；盐值组成与候选表顺序决定联机结果，改动会改变已有对局的生成结果。
/// </summary>
internal static class HextechStableCombatSpawns
{
	public static CardModel CreateMinionCard(HextechCombatState combatState, Player owner, string source, int ordinal)
	{
		int index = HextechStableRandom.Index((RunState)owner.RunState, 3,
			source,
			HextechStableRandom.PlayerKey(owner),
			"round",
			combatState.RoundNumber.ToString(),
			"ordinal",
			ordinal.ToString());
		return index switch
		{
			0 => combatState.CreateCard<MinionStrike>(owner),
			1 => combatState.CreateCard<MinionDiveBomb>(owner),
			_ => combatState.CreateCard<MinionSacrifice>(owner)
		};
	}

	public static OrbModel CreateOrb(RunState runState, Player owner, string source, int ordinal, int roundNumber)
	{
		int index = HextechStableRandom.Index(runState, 5,
			source,
			HextechStableRandom.PlayerKey(owner),
			"round",
			roundNumber.ToString(),
			"ordinal",
			ordinal.ToString());
		return index switch
		{
			0 => ModelDb.Orb<LightningOrb>().ToMutable(),
			1 => ModelDb.Orb<FrostOrb>().ToMutable(),
			2 => ModelDb.Orb<DarkOrb>().ToMutable(),
			3 => ModelDb.Orb<PlasmaOrb>().ToMutable(),
			_ => ModelDb.Orb<GlassOrb>().ToMutable()
		};
	}
}
