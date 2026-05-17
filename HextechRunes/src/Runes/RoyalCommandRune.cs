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

public sealed class RoyalCommandRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ForgeVar("ForgeAmount", 3)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterStarsGained(int amount, Player gainer)
	{
		return HandleStarsChanged(amount, gainer);
	}

	public override Task AfterStarsSpent(int amount, Player spender)
	{
		return HandleStarsChanged(amount, spender);
	}

	private Task HandleStarsChanged(int amount, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return ForgeCmd.Forge(DynamicVars["ForgeAmount"].BaseValue, Owner, this);
	}
}
