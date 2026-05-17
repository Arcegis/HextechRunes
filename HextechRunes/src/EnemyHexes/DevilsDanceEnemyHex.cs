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
			int heal = Math.Max(1, (int)Math.Floor(dealer.MaxHp * HextechMayhemModifier.DevilsDanceHealPercent));
			await CreatureCmd.Heal(dealer, heal);
		}
	}
}
