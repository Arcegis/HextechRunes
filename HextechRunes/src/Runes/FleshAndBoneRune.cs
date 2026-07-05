namespace HextechRunes;

public sealed class FleshAndBoneRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpLoss", 3m),
		new SummonVar(15m),
		new DynamicVar("OstyMaxHpPerHeal", 10m),
		new DynamicVar("OstyHeal", 1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || !IsNecrobinderPlayer(Owner))
		{
			return;
		}

		Flash();
		if (Owner.Creature.CurrentHp > 1)
		{
			decimal nextHp = Math.Max(1m, Owner.Creature.CurrentHp - DynamicVars["HpLoss"].BaseValue);
			await CreatureCmd.SetCurrentHp(Owner.Creature, nextHp);
		}

		await OstyCmd.Summon(new BlockingPlayerChoiceContext(), Owner, DynamicVars.Summon.BaseValue, this);
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !Owner.IsOstyAlive
			|| Owner.Osty == null
			|| !IsNecrobinderPlayer(Owner))
		{
			return Task.CompletedTask;
		}

		decimal healAmount = Math.Floor(Owner.Osty.MaxHp / DynamicVars["OstyMaxHpPerHeal"].BaseValue) * DynamicVars["OstyHeal"].BaseValue;
		if (healAmount <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash([Owner.Creature]);
		return CreatureCmd.Heal(Owner.Creature, healAmount);
	}
}
