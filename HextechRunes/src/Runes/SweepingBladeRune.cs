using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

public sealed class SweepingBladeRune : HextechRelicBase
{
	private static readonly FieldInfo? AttackCommandSingleTargetField = TryGetField(typeof(AttackCommand), "_singleTarget");
	private static readonly FieldInfo? AttackCommandCombatStateField = TryGetField(typeof(AttackCommand), "_combatState");

	public override Task BeforeAttack(AttackCommand command)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| command.Attacker != Owner.Creature
			|| command.ModelSource is not CardModel card
			|| !IsOwnedAttack(card)
			|| !card.IsBasicStrikeOrDefend
			|| !command.IsSingleTargeted
			|| Owner.Creature.CombatState == null)
		{
			return Task.CompletedTask;
		}

		Flash();
		RetargetToAllOpponents(command, Owner.Creature.CombatState);
		return Task.CompletedTask;
	}

	private static void RetargetToAllOpponents(AttackCommand command, object combatState)
	{
		if (AttackCommandSingleTargetField == null || AttackCommandCombatStateField == null)
		{
			return;
		}

		AttackCommandSingleTargetField.SetValue(command, null);
		AttackCommandCombatStateField.SetValue(command, combatState);
	}
}
