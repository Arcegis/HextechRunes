using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class EchoFormUpgradeRune : CardUpgradeRuneBase<EchoForm>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		return ShouldAddThirdPlay(card) ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (ShouldAddThirdPlay(card))
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private bool ShouldAddThirdPlay(CardModel card)
	{
		if (Owner == null || card.Owner != Owner)
		{
			return false;
		}

		decimal echoAmount = Owner.Creature.GetPowerAmount<EchoFormPower>();
		if (echoAmount <= 0m)
		{
			return false;
		}

		int echoedThisTurn = CombatManager.Instance.History.CardPlaysStarted.Count(entry =>
			entry.Actor == Owner.Creature
			&& entry.CardPlay.IsFirstInSeries
			&& entry.HappenedThisTurn(Owner.Creature.CombatState));
		return echoedThisTurn < echoAmount;
	}
}
