using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class BodyguardUpgradeRune : CardUpgradeRuneBase<Bodyguard>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Bodyguard
			|| !IsNecrobinderPlayer(Owner)
			|| Owner.Osty == null
			|| Owner.Osty.IsDead)
		{
			return;
		}

		decimal heal = Owner.Osty.MaxHp - Owner.Osty.CurrentHp;
		if (heal <= 0m)
		{
			return;
		}

		Flash([Owner.Osty]);
		await CreatureCmd.Heal(Owner.Osty, heal);
	}
}
