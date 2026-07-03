using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class BlankCheckRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		List<CardModel> pool = ModelDb.CardPool<ColorlessCardPool>()
			.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
			.Where(static card => card.Rarity is not CardRarity.Basic and not CardRarity.Ancient && card.CanBeGeneratedInCombat)
			.OrderBy(HextechStableRandom.CardKey, StringComparer.Ordinal)
			.ToList();
		if (pool.Count == 0)
		{
			return;
		}

		CardModel canonicalCard = HextechStableRandom.Pick(
			pool,
			(RunState)Owner.RunState,
			HextechStableRandom.CardKey,
			"blank-check-colorless-card",
			HextechStableRandom.PlayerKey(Owner),
			combatState.RoundNumber.ToString(),
			PileType.Hand.GetPile(Owner).Cards.Count.ToString(),
			HextechStableRandom.CardPileKey(pool));
		CardModel card = combatState.CreateCard(canonicalCard, Owner);

		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldAffectColorlessCard(card) || card.EnergyCost.CostsX)
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldAffectColorlessCard(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		// 只改写常规去向(弃牌堆):去向为 None 的是复制品/能力牌等"打出后移出战斗"的卡,
		// 抢改成消耗堆会把复制品变成滞留的幽灵实体,再被召之即来拉回手牌就是
		// "空白手牌位"(玩家实报,复制打出君王之剑时触发)。
		return pileType is PileType.Discard && ShouldAffectColorlessCard(card)
			? (PileType.Exhaust, position)
			: (pileType, position);
	}

	private bool ShouldAffectColorlessCard(CardModel card)
	{
		// 有意包含 Token 卡(君王之剑/仆从牌):游戏逻辑上它们不算无色牌,
		// 但按玩家直觉统一视作无色处理。
		return card.Owner == Owner && HextechColorlessCardHelper.IsColorlessCard(card);
	}
}
