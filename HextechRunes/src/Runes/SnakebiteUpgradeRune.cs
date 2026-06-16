using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class SnakebiteUpgradeRune : CardUpgradeRuneBase<Snakebite>
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 1m)
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || Owner.Creature.IsDead || side != Owner.Creature.Side)
		{
			return Task.CompletedTask;
		}

		List<CardModel> retainedSnakebites = PileType.Hand.GetPile(Owner).Cards
			.Where(card => card.Owner == Owner && card is Snakebite && card.ShouldRetainThisTurn)
			.ToList();
		if (retainedSnakebites.Count == 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		foreach (CardModel card in retainedSnakebites)
		{
			card.BaseReplayCount += DynamicVars["Replays"].IntValue;
			CardCmd.Preview(card);
		}

		return Task.CompletedTask;
	}
}
