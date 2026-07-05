namespace HextechRunes;

public sealed class ScaredStiffRune : HextechRelicBase
{
	private bool _autoPlaying;
	private int _autoPlayTargetsThisCombat;

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (_autoPlaying || Owner == null || Owner.Creature.IsDead || side != Owner.Creature.Side || !IsIroncladPlayer(Owner))
		{
			return;
		}

		List<CardModel> attacks = PileType.Hand.GetPile(Owner).Cards
			.Where(IsOwnedAttack)
			.ToList();
		if (attacks.Count == 0)
		{
			return;
		}

		_autoPlaying = true;
		try
		{
			Flash();
			for (int i = 0; i < attacks.Count; i++)
			{
				CardModel card = attacks[i];
				if (card.Pile?.Type != PileType.Hand)
				{
					continue;
					}

					card.ExhaustOnNextPlay = true;
					HextechCombatState? combatState = Owner.Creature.CombatState;
					int targetOrdinal = ConsumeCombatProcOrdinal(nameof(ScaredStiffRune), ref _autoPlayTargetsThisCombat);
					Creature? target = HextechRuneTargeting.PickRandomHittableEnemy(
						Owner,
						combatState,
						"scared-stiff",
						combatState?.RoundNumber.ToString() ?? "-1",
						i.ToString(),
						targetOrdinal.ToString());
					await HextechAutoPlayHelper.AutoPlayOrMoveToResultPile(choiceContext, card, target);
				}
		}
		finally
		{
			_autoPlaying = false;
		}
	}
}
