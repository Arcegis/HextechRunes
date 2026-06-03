using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class ScaredStiffRune : HextechRelicBase
{
	private bool _autoPlaying;

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
				Creature? target = HextechRuneTargeting.PickRandomHittableEnemy(
					Owner,
					combatState,
					"scared-stiff",
					combatState?.RoundNumber.ToString() ?? "-1",
					i.ToString(),
					CombatManager.Instance.History.Entries.Count().ToString());
				await HextechAutoPlayHelper.AutoPlayOrMoveToResultPile(choiceContext, card, target);
			}
		}
		finally
		{
			_autoPlaying = false;
		}
	}
}
