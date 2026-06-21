using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

public sealed class EchoRune : HextechRelicBase
{
	private bool _echoingCard;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
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
		if (_echoingCard
			|| !addedByPlayer
			|| Owner == null
			|| Owner.Creature.IsDead
			|| card.Owner != Owner
			|| !TryGetEchoPile(card, out PileType pileType))
		{
			return;
		}

		CardModel echo = card.CreateClone();
		echo.AddKeyword(CardKeyword.Ethereal);
		echo.SetToFreeThisTurn();

		_echoingCard = true;
		try
		{
			Flash();
			await HextechCardGeneration.AddGeneratedCardToCombat(echo, pileType, addedByPlayer: true, previewNonHandAdds: false);
		}
		finally
		{
			_echoingCard = false;
		}
	}

	private static bool TryGetEchoPile(CardModel card, out PileType pileType)
	{
		pileType = card.Pile?.Type ?? PileType.None;
		return pileType is PileType.Hand or PileType.Draw or PileType.Discard;
	}
}
