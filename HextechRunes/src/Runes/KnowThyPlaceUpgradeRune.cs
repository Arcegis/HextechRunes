using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class KnowThyPlaceUpgradeRune : CardUpgradeRuneBase<KnowThyPlace>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not KnowThyPlace
			|| cardPlay.Target == null
			|| cardPlay.Target.Side == Owner.Creature.Side)
		{
			return;
		}

		Flash([cardPlay.Target]);
		await PowerCmd.Apply<StrengthPower>(cardPlay.Target, -1m, Owner.Creature, cardPlay.Card);
		await PowerCmd.Apply<DexterityPower>(cardPlay.Target, -1m, Owner.Creature, cardPlay.Card);
	}
}
