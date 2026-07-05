namespace HextechRunes;

public sealed class BrutalityRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpLoss", 2m),
		new CardsVar(2)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		if (Owner.Creature.CurrentHp > 1)
		{
			decimal nextHp = Math.Max(1m, Owner.Creature.CurrentHp - DynamicVars["HpLoss"].BaseValue);
			await CreatureCmd.SetCurrentHp(Owner.Creature, nextHp);
		}

		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}
}
