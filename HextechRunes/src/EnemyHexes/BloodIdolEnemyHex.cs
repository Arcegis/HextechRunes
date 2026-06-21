namespace HextechRunes;

internal sealed class BloodIdolEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.BloodIdol;

	internal override async Task AfterGoldGained(HextechEnemyHexContext context, Player player)
	{
		if (player.RunState != context.RunState || player.Creature.IsDead)
		{
			return;
		}

		await CreatureCmd.Damage(
			new BlockingPlayerChoiceContext(),
			player.Creature,
			1m,
			ValueProp.Unblockable | ValueProp.Unpowered,
			null,
			null);
	}
}
