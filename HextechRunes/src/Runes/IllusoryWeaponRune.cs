using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class IllusoryWeaponRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(2m, ValueProp.Unpowered)
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

		Creature? target = HextechRuneTargeting.PickRandomHittableEnemy(
			Owner,
			combatState,
			"illusory-weapon",
			combatState.RoundNumber.ToString(),
			CombatManager.Instance.History.Entries.Count().ToString(),
			cardPlay.Card.Id.Entry);
		if (target == null)
		{
			return;
		}

		Flash([target]);
		await CreatureCmd.Damage(choiceContext, target, DynamicVars.Damage.BaseValue, ValueProp.Unpowered, Owner.Creature, cardPlay.Card);
	}

	internal static bool ShouldTreatSkillAsAttack(Player? owner)
	{
		return owner?.GetRelic<IllusoryWeaponRune>() != null;
	}

	internal static bool IsOriginalOwnedSkill(CardModel? card, Player owner)
	{
		return card?.Owner == owner && IsSkillForEffects(card);
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
