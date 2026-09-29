namespace HextechRunes;

public sealed class SoulUpgradeRune : CardUpgradeRuneBase<Soul>
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		// 注意:名为 Energy 的变量必须用 EnergyVar,DynamicVars.Energy 访问器按该类型强转。
		new EnergyVar(1)
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| cardPlay.Card is not Soul
			|| cardPlay.Card.Owner != Owner)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}
}
