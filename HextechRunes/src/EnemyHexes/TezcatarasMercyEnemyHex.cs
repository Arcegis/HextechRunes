namespace HextechRunes;

internal sealed class TezcatarasMercyEnemyHex : HextechEnemyHexEffect
{
	private const int CombatInterval = 4;

	internal override MonsterHexKind Kind => MonsterHexKind.TezcatarasMercy;

	internal override async Task AfterCombatVictory(HextechEnemyHexContext context, CombatRoom room)
	{
		if (!context.Modifier.IncrementEnemyTezcatarasMercyCombatCounter(CombatInterval))
		{
			return;
		}

		foreach (Player player in context.RunState.Players.OrderBy(static player => player.NetId))
		{
			RelicModel? relic = player.Relics.FirstOrDefault(static relic => relic.IsWax && !relic.IsMelted);
			if (relic != null)
			{
				await RelicCmd.Melt(relic);
			}
		}
	}

	internal override bool TryModifyRewards(HextechEnemyHexContext context, Player player, List<Reward> rewards, AbstractRoom? room)
	{
		bool modified = false;
		for (int i = 0; i < rewards.Count; i++)
		{
			if (rewards[i] is not RelicReward { IsPopulated: true } relicReward)
			{
				continue;
			}

			RelicModel? relic = relicReward.ClaimedRelic;
			if (relic != null && ShouldConvertRelic(player, relic))
			{
				rewards[i] = new HextechWaxRelicReward(relic, player);
				modified = true;
			}
		}

		return modified;
	}

	internal static bool ShouldConvertRelic(Player player, RelicModel? relic)
	{
		return relic != null
			&& !relic.IsWax
			&& !HextechCatalog.IsHextechRelic(relic)
			&& !HextechCatalog.IsHextechForgeRelic(relic)
			&& !HextechCatalog.IsHextechShopRelic(relic)
			&& IsActiveFor(player);
	}

	private static bool IsActiveFor(Player player)
	{
		return player.RunState is RunState runState
			&& runState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is HextechMayhemModifier modifier
			&& modifier.HasActiveMonsterHex(MonsterHexKind.TezcatarasMercy);
	}
}
