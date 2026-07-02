using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

/// <summary>
/// 「升级：XX形态」系共用基类:回合结束时,若手牌内有目标能力牌(TCard),自动将其打出。
/// 打出走 HextechAutoPlayHelper(正常打出路径,免费),与保留/虚无等词条按原版语义交互。
/// 补卡/无刷新门槛由 CardUpgradeRuneBase 提供。
/// </summary>
public abstract class PlayFromHandOnTurnEndRuneBase<TCard> : CardUpgradeRuneBase<TCard>
	where TCard : CardModel
{
	private bool _autoPlaying;

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (_autoPlaying
			|| Owner == null
			|| Owner.Creature.IsDead
			|| side != Owner.Creature.Side
			|| !IsAvailableForCharacter(Owner))
		{
			return;
		}

		List<TCard> cards = PileType.Hand.GetPile(Owner).Cards
			.OfType<TCard>()
			.Where(card => card.Owner == Owner)
			.ToList();
		if (cards.Count == 0)
		{
			return;
		}

		_autoPlaying = true;
		try
		{
			Flash();
			foreach (TCard card in cards)
			{
				if (card.Pile?.Type != PileType.Hand)
				{
					continue;
				}

				await HextechAutoPlayHelper.AutoPlayOrMoveToResultPile(choiceContext, card, target: null);
			}
		}
		finally
		{
			_autoPlaying = false;
		}
	}
}
