using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

/// <summary>
/// 「升级：XX形态」系共用基类:每场战斗开始时,自动打出你所有的目标能力牌(TCard),
/// 类似注能附魔——形态不再占用手牌与抽牌流。触发时机照抄固态时间:首个玩家回合开始后
/// (牌堆与打出管线就绪),以每场一次标志防重复;打出走 HextechAutoPlayHelper 正常路径(免费)。
/// 补卡/无刷新门槛由 CardUpgradeRuneBase 提供。
/// </summary>
public abstract class AutoPlayFormsAtCombatStartRuneBase<TCard> : CardUpgradeRuneBase<TCard>
	where TCard : CardModel
{
	private bool _startedThisCombat;
	private bool _autoPlaying;

	public override Task BeforeCombatStart()
	{
		_startedThisCombat = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_startedThisCombat = false;
		return Task.CompletedTask;
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
#else
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
#endif
	{
		if (_startedThisCombat
			|| _autoPlaying
			|| player != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.PlayerCombatState == null
			|| !IsAvailableForCharacter(Owner))
		{
			return;
		}

		_startedThisCombat = true;

		// "你所有的 XX 形态":战斗牌堆(抽牌/手牌/弃牌)里的全部,含引魂/固有等把牌送进
		// 非常规起始位置的情况;消耗堆与已移除的不算。
		List<TCard> cards = Owner.PlayerCombatState.AllCards
			.OfType<TCard>()
			.Where(card => card.Owner == Owner
				&& card.Pile?.Type is PileType.Draw or PileType.Hand or PileType.Discard)
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
				if (card.Pile?.Type is not (PileType.Draw or PileType.Hand or PileType.Discard))
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
