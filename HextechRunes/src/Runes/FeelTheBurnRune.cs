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

public sealed class FeelTheBurnRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<WeakPower>(2m),
		new PowerVar<VulnerablePower>(2m),
		new DynamicVar("BurnPercent", 10m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<VulnerablePower>(),
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null
			|| target != Owner.Creature
			|| result.UnblockedDamage <= 0
			|| Owner.Creature.CombatState == null)
		{
			return;
		}

		List<Creature> enemies = Owner.Creature.CombatState.Enemies.Where(static enemy => enemy.IsAlive).ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		int burnAmount = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["BurnPercent"].BaseValue / 100m));
		await PowerCmd.Apply<WeakPower>(enemies, DynamicVars.Weak.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<VulnerablePower>(enemies, DynamicVars.Vulnerable.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<HextechBurnPower>(enemies, burnAmount, Owner.Creature, null);
	}
}
