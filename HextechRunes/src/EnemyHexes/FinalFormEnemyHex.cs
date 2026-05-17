namespace HextechRunes;

internal sealed class FinalFormEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.FinalForm;

	internal override async Task AfterEnemyDamageGivenPlayerHit(HextechEnemyHexContext context, Creature dealer, Creature target)
	{
		if (dealer.IsAlive
			&& dealer.CombatId != null
			&& context.Tracking.FinalFormTriggeredThisTurn.Add(dealer.CombatId.Value))
		{
			int block = Math.Max(1, (int)Math.Floor(dealer.MaxHp * HextechMayhemModifier.FinalFormBlockPercent));
			await CreatureCmd.GainBlock(dealer, block, ValueProp.Unpowered, null);
		}
	}
}
