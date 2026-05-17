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

public sealed class DrawYourSwordRune : HextechRelicBase
{
	private readonly Queue<decimal> _pendingEnergyConversions = new();

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[
				new DynamicVar("HpGainPercent", 0.3m)
			];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override Task BeforeCombatStart()
	{
		_pendingEnergyConversions.Clear();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingEnergyConversions.Clear();
		return Task.CompletedTask;
	}

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		int hpGain = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["HpGainPercent"].BaseValue));
		await CreatureCmd.GainMaxHp(Owner.Creature, hpGain);
	}

	public override decimal ModifyEnergyGain(Player player, decimal amount)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0m)
		{
			return amount;
		}

		_pendingEnergyConversions.Enqueue(amount);
		return 0m;
	}

	public override async Task AfterModifyingEnergyGain()
	{
		if (!_pendingEnergyConversions.TryDequeue(out decimal amount)
			|| Owner == null
			|| Owner.Creature.IsDead
			|| amount <= 0m)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, amount, Owner.Creature, null);
		await PowerCmd.Apply<DexterityPower>(Owner.Creature, amount, Owner.Creature, null);
		await PowerCmd.Apply<FocusPower>(Owner.Creature, amount, Owner.Creature, null);
	}
}
