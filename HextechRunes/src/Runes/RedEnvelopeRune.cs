namespace HextechRunes;

public sealed class RedEnvelopeRune : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (IsNetworkMultiplayer())
		{
			return Task.CompletedTask;
		}

		return ApplySharedCombatVictory(room);
	}

	public Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash(Array.Empty<Creature>());
		if (HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			75,
			"red-envelope-reward",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Relics.Count.ToString()))
		{
			HextechGoldRewardHelper.AddStableRangedExtraGoldReward(
				room,
				Owner,
				20,
				50,
				"red-envelope-gold",
				Owner.Relics.Count.ToString());
		}
		else
		{
			HextechForgeGrantHelper.AddRandomForgeReward(Owner, room);
		}

		return Task.CompletedTask;
	}
}
