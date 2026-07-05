namespace HextechRunes;

public sealed class HappyAccidentRune : HextechRelicBase
{
	private int _statusOrbsThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbCount", 1m),
		new PowerVar<FocusPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
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
		if (!addedByPlayer
			|| card.Owner != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState == null
			|| card.Type != CardType.Status)
		{
			return;
		}

		Flash();
		PlayerChoiceContext choiceContext = new BlockingPlayerChoiceContext();
		for (int i = 0; i < DynamicVars["OrbCount"].IntValue; i++)
		{
			int orbOrdinal = ConsumeCombatProcOrdinal(nameof(HappyAccidentRune), ref _statusOrbsThisCombat);
			OrbModel orb = HextechStableRandom.CreateOrb(
				(RunState)Owner.RunState,
				Owner,
				"happy-accident-status-orb",
				orbOrdinal,
				Owner.Creature.CombatState.RoundNumber);
			await OrbCmd.Channel(choiceContext, orb, Owner);
		}

		await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, card);
	}
}
