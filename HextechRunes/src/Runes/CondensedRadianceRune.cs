namespace HextechRunes;

public sealed class CondensedRadianceRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
		new StarsVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
#else
	public override async Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
#endif
	{
#if STS2_104_OR_NEWER
		bool addedByPlayer = creator == Owner;
#endif
		if (!addedByPlayer || card.Owner != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainStars(DynamicVars.Stars.BaseValue, Owner);
	}
}
