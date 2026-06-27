using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

public abstract class UniversalScopeRuneBase : HextechRelicBase
{
	private int _refundRollsThisCombat;

	protected abstract int ChancePercent { get; }

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ChancePercent", ChancePercent)
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		// 必须在 series 的最后一次打出后才把牌移回手牌:重放(playCount>1)会让同一张牌在 Play
		// 牌堆里连续打多次,若在 IsFirstInSeries 就移走,后续重放找不到这张牌会崩溃。
		// 普通牌 IsFirstInSeries==IsLastInSeries,行为不变。
		// 不再排除自动打出:地狱狂徒(Hellraiser)抽到牌即自动打出(IsAutoPlay),这些攻击牌
		// 同样应能被瞄准镜返还;返还到手牌不算抽牌、不会再触发 Hellraiser 自动打,无死循环。
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !cardPlay.IsLastInSeries
			|| !IsOwnedAttack(cardPlay.Card))
		{
			return;
		}

		int rollOrdinal = ConsumeCombatProcOrdinal(GetType().Name, ref _refundRollsThisCombat);
		if (!RollTrigger(cardPlay, rollOrdinal))
		{
			return;
		}

		HextechCardPlayResourceSpend resourceSpend = HextechCombatHooks.GetResourceSpendForCurrentCardPlay(cardPlay.Card);
		Flash();
		if (cardPlay.Card.Pile?.Type == PileType.Play)
		{
			await CardPileCmd.Add(cardPlay.Card, PileType.Hand, CardPilePosition.Bottom, this);
		}

		if (resourceSpend.Energy > 0m)
		{
			await PlayerCmd.GainEnergy(resourceSpend.Energy, Owner);
		}

		if (resourceSpend.Stars > 0m)
		{
			await PlayerCmd.GainStars(resourceSpend.Stars, Owner);
		}
	}

	private bool RollTrigger(CardPlay cardPlay, int rollOrdinal)
	{
		if (Owner == null)
		{
			return false;
		}

		return HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			DynamicVars["ChancePercent"].IntValue,
				"universal-scope-refund",
				GetType().Name,
				HextechStableRandom.PlayerKey(Owner),
				Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
				rollOrdinal.ToString(),
				HextechStableRandom.CardKey(cardPlay.Card));
	}
}

public sealed class UniversalScopeRune : UniversalScopeRuneBase
{
	protected override int ChancePercent => 15;
}
