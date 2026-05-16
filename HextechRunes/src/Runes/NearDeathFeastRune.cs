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

public sealed class NearDeathFeastRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CurrentHpLossPercent", 10m),
		new DynamicVar("StrengthPerHpLost", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || Owner.Creature.CurrentHp <= 1)
		{
			return;
		}

		decimal loss = Math.Floor(Owner.Creature.CurrentHp * DynamicVars["CurrentHpLossPercent"].BaseValue / 100m);
		loss = Math.Min(loss, Owner.Creature.CurrentHp - 1m);
		if (loss <= 0m)
		{
			return;
		}

		decimal strength = Math.Floor(loss * DynamicVars["StrengthPerHpLost"].BaseValue);
		if (strength <= 0m)
		{
			return;
		}

		Flash();
		await CreatureCmd.SetCurrentHp(Owner.Creature, Owner.Creature.CurrentHp - loss);
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, strength, Owner.Creature, null);
	}
}
