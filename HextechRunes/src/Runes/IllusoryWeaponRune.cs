using MegaCrit.Sts2.Core.Models.Exceptions;

namespace HextechRunes;

public sealed class IllusoryWeaponRune : HextechRelicBase
{
	private int _damageTargetsThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(2m, ValueProp.Move)
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !IsOriginalOwnedSkill(cardPlay.Card, Owner)
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		try
		{
			int targetOrdinal = ConsumeCombatProcOrdinal(nameof(IllusoryWeaponRune), ref _damageTargetsThisCombat);
			Creature? target = HextechRuneTargeting.PickRandomHittableEnemy(
				Owner,
				combatState,
				"illusory-weapon",
				combatState.RoundNumber.ToString(),
				targetOrdinal.ToString(),
				cardPlay.Card.Id.Entry);
			if (target == null)
			{
				return;
			}

			Flash([target]);
			await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
				.FromCardCompat(cardPlay.Card)
				.Targeting(target)
				.WithNoAttackerAnim()
				.Execute(choiceContext);
		}
		finally
		{
			HextechPlayerRuneHooks.ClearIllusoryWeaponPendingPenNib(Owner, cardPlay.Card);
		}
	}

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
