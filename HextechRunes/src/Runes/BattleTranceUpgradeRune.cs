using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class BattleTranceUpgradeRune : CardUpgradeRuneBase<BattleTrance>
{
	private decimal _noDrawBeforePlay;
	private bool _shouldCleanNoDraw;

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		_shouldCleanNoDraw = cardPlay.Card.Owner == Owner && cardPlay.Card is BattleTrance;
		_noDrawBeforePlay = _shouldCleanNoDraw && Owner != null
			? Owner.Creature.GetPowerAmount<NoDrawPower>()
			: 0m;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!_shouldCleanNoDraw || Owner == null || cardPlay.Card.Owner != Owner || cardPlay.Card is not BattleTrance)
		{
			return;
		}

		_shouldCleanNoDraw = false;
		NoDrawPower? noDraw = Owner.Creature.GetPower<NoDrawPower>();
		decimal excess = (noDraw?.Amount ?? 0m) - _noDrawBeforePlay;
		if (excess <= 0m)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<NoDrawPower>(Owner.Creature, -excess, Owner.Creature, cardPlay.Card, silent: true);
	}
}
