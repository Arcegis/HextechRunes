namespace HextechRunes;

public sealed class MasterOfDualityRune : HextechRelicBase
{
	private const decimal TemporaryStatGain = 1m;

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || cardPlay.Card.Owner != Owner)
		{
			return;
		}

		if (HextechCardEffectTypes.IsSkillForEffects(cardPlay.Card))
		{
			Flash();
			await ApplyTemporaryStat<HextechTemporaryStrengthPower, HextechTemporaryStrengthLossPower>(
				Owner.Creature, isGain: true, Owner.Creature, null);
		}

		if (IsOwnedAttack(cardPlay.Card))
		{
			Flash();
			await ApplyTemporaryStat<HextechTemporaryDexterityPower, HextechTemporaryDexterityLossPower>(
				Owner.Creature, isGain: true, Owner.Creature, null);
		}
	}

	// 我方与敌方物法皆修由同一张牌同时触发，先扣减对向的隐藏临时能力，不让两份并存。
	// 并存时任一份被净化、转移或提前移除，另一份回合末的回收就会变成永久的力量/敏捷变化。
	// 施加减益一侧遇到人工制品时仍走原施加路径，由人工制品整体抵挡，不去扣减增益。
	internal static async Task ApplyTemporaryStat<TGain, TLoss>(
		Creature creature,
		bool isGain,
		Creature? applier,
		CardModel? cardSource)
		where TGain : PowerModel
		where TLoss : PowerModel
	{
		// 人工制品挡下的施加会留下 0 层实例；原版临时能力在"变化量等于当前层数"时视为首次施加、不再补属性，
		// 0 层实例被再次叠加会漏掉这次力量/敏捷变化，先移除让下次重新施加。
		PowerModel? stale = isGain ? creature.GetPower<TGain>() : creature.GetPower<TLoss>();
		if (stale is { Amount: <= 0 })
		{
			await HextechPowerCmdCompat.Remove(stale);
		}

		PowerModel? opposite = isGain ? creature.GetPower<TLoss>() : creature.GetPower<TGain>();
		bool artifactBlocksLoss = !isGain && creature.GetPowerAmount<ArtifactPower>() > 0;
		if (opposite is { Amount: > 0 } && !artifactBlocksLoss)
		{
			await HextechPowerCmdCompat.ModifyAmount(opposite, -TemporaryStatGain, applier, cardSource);
			return;
		}

		if (isGain)
		{
			await PowerCmd.Apply<TGain>(creature, TemporaryStatGain, applier, cardSource);
		}
		else
		{
			await PowerCmd.Apply<TLoss>(creature, TemporaryStatGain, applier, cardSource);
		}
	}
}
