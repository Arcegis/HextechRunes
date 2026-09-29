using MegaCrit.Sts2.Core.Models.Exceptions;

namespace HextechRunes;

/// <summary>
/// 符文效果口径下的卡牌类型判定。持有幻影武器（<see cref="IllusoryWeaponRune"/>）时，持有者自己的原始技能牌
/// 在"攻击牌"类效果里也算攻击；技能牌按规范实例的类型判定，不受战斗中临时改类型影响。
/// </summary>
internal static class HextechCardEffectTypes
{
	internal static bool ShouldTreatSkillAsAttack(Player? owner)
	{
		return owner?.GetRelic<IllusoryWeaponRune>() != null;
	}

	internal static bool IsOriginalOwnedSkill(CardModel? card, Player owner)
	{
		return card?.Owner == owner && IsSkillForEffects(card);
	}

	internal static bool IsAttackForEffects(CardModel? card, Player? owner)
	{
		if (card == null)
		{
			return false;
		}

		if (card.Type == CardType.Attack)
		{
			return true;
		}

		return owner != null
			&& ShouldTreatSkillAsAttack(owner)
			&& IsOriginalOwnedSkill(card, owner);
	}

	internal static bool IsSkillForEffects(CardModel? card)
	{
		if (card == null)
		{
			return false;
		}

		try
		{
			return (card.CanonicalInstance?.Type ?? card.Type) == CardType.Skill;
		}
		catch (CanonicalModelException)
		{
			return card.Type == CardType.Skill;
		}
	}
}
