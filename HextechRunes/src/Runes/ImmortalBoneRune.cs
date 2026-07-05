namespace HextechRunes;

public sealed class ImmortalBoneRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HealPercent", 50m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || !Owner.IsOstyAlive || Owner.Osty == null)
		{
			return Task.CompletedTask;
		}

		Flash([Owner.Osty]);
		int healAmount = Math.Max(1, FloorToInt(Owner.Osty.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m));
		return CreatureCmd.Heal(Owner.Osty, healAmount);
	}
}
