namespace HextechRunes;

internal sealed class DevilsDanceEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.DevilsDance;

	internal override async Task AfterEnemyDamageGivenPlayerHit(HextechEnemyHexContext context, Creature dealer, Creature target)
	{
		if (dealer.IsAlive
			&& dealer.CombatId != null
			&& context.Tracking.DevilsDanceTriggeredThisTurn.Add(dealer.CombatId.Value))
		{
			decimal healPercent = context.TierValue(Kind, 0.06m, 0.08m, 0.10m);
			int heal = Math.Max(1, (int)Math.Floor(dealer.MaxHp * healPercent));
			await CreatureCmd.Heal(dealer, heal);
		}
	}
}
