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

public sealed class DexterityToStrengthRune : AttributeConversionRelicBase
{

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m)
	];

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null)
		{
			return Task.CompletedTask;
		}

		return PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
	}

	protected override bool ShouldConvert(PowerModel canonicalPower)
	{
		return canonicalPower is DexterityPower;
	}

	protected override bool ShouldConvertAppliedPower(PowerModel power)
	{
		return power is DexterityPower;
	}

	protected override Task ApplyConvertedPower(decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<StrengthPower>(Owner!.Creature, amount, applier, cardSource);
	}

	protected override Task RevertOriginalPower(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<DexterityPower>(Owner!.Creature, -amount, applier, cardSource);
	}
}
