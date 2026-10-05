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
		if (player != Owner || Owner.Creature.IsDead || Owner.Osty is not { IsAlive: true } osty)
		{
			return Task.CompletedTask;
		}

		Flash([osty]);
		int healAmount = Math.Max(1, FloorToInt(osty.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m));
		return CreatureCmd.Heal(osty, healAmount);
	}
}
