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

public sealed class OneLaneBridgeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ImbalancedPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ImbalancedPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		await PowerCmd.Apply<ImbalancedPower>(enemies, DynamicVars["ImbalancedPower"].BaseValue, Owner.Creature, null);
	}
}
