using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class BattleTranceUpgradeRune : CardUpgradeRuneBase<BattleTrance>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsIroncladPlayer(player);
	}

	/// <summary>
	/// 从源头阻止战斗专注施加"不可抽牌"(施加量乘 0),而不是施加后再移除:
	/// 旧做法的中间态(短暂持有 NoDraw)会与其他抽牌类效果产生时序交互 bug。
	/// 原版 BattleTrance.OnPlay 施加时 cardSource=卡实例,可精确匹配;
	/// 其他来源的不可抽牌(如敌方效果)不受影响。
	/// </summary>
	public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return power is NoDrawPower
			&& Owner != null
			&& giver == Owner.Creature
			&& cardSource is BattleTrance
			? 0m
			: 1m;
	}
}
