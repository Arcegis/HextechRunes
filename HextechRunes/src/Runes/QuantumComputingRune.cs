using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class QuantumComputingRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamagePercent", 10m),
		new DamageVar(10m, ValueProp.Unpowered),
		new DynamicVar("HealPercent", 10m)
	];

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return;
		}

		int round = Owner.Creature.CombatState.RoundNumber;
		if (round <= 0 || round % 2 != 0)
		{
			return;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		int totalDamage = 0;
		foreach (Creature enemy in enemies)
		{
			decimal damage = DynamicVars.Damage.BaseValue + Math.Floor(enemy.MaxHp * DynamicVars["DamagePercent"].BaseValue / 100m);
			IEnumerable<DamageResult> results = await CreatureCmd.Damage(choiceContext, enemy, damage, ValueProp.Unpowered, Owner.Creature, null);
			totalDamage += results.Sum(static result => result.UnblockedDamage);
		}

		int heal = FloorToInt(totalDamage * DynamicVars["HealPercent"].BaseValue / 100m);
		if (heal > 0 && !Owner.Creature.IsDead)
		{
			await CreatureCmd.Heal(Owner.Creature, heal);
		}
	}
}
