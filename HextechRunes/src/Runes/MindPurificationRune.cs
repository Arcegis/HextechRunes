using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace HextechRunes;

public sealed class MindPurificationRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamagePercent", 30m)
	];

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (Owner == null
			|| wasRemovalPrevented
			|| target.Side == Owner.Creature.Side
			|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(target, out HextechCombatState? combatState))
		{
			return;
		}

		List<(Creature creature, int damage)> toDamage = combatState.Enemies
			.Where(enemy => enemy != target && enemy.IsAlive)
			.Select(enemy => (enemy, FloorToInt(enemy.CurrentHp * 0.3m)))
			.Where(pair => pair.Item2 > 0)
			.ToList();
		if (toDamage.Count == 0)
		{
			return;
		}

		Flash(toDamage.Select(static pair => pair.creature));
		foreach ((Creature creature, int damage) in toDamage)
		{
			await CreatureCmd.Damage(choiceContext, creature, damage, ValueProp.Unpowered, Owner.Creature, null);
		}
	}
}
