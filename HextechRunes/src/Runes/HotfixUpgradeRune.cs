using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class HotfixUpgradeRune : CardUpgradeRuneBase<Hotfix>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Hotfix
			|| Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<FocusPower>(Owner.Creature, 1m, Owner.Creature, cardPlay.Card);
	}
}
