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

public sealed class EscapePlanRune : HextechRelicBase
{
	private bool _pendingTrigger;
	private bool _triggeredThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedPendingTrigger
	{
		get => _pendingTrigger;
		set => _pendingTrigger = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisCombat
	{
		get => _triggeredThisCombat;
		set => _triggeredThisCombat = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ThresholdPercent", 50m),
		new DynamicVar("BlockPercent", 60m)
	];

	public override Task BeforeCombatStart()
	{
		_pendingTrigger = false;
		_triggeredThisCombat = false;
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingTrigger = false;
		_triggeredThisCombat = false;
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null
			|| target != Owner.Creature
			|| result.UnblockedDamage <= 0
			|| _triggeredThisCombat
			|| _pendingTrigger
			|| target.CurrentHp >= target.MaxHp * 0.5m)
		{
			return Task.CompletedTask;
		}

		_pendingTrigger = true;
		_triggeredThisCombat = true;
		Status = RelicStatus.Active;
		Flash();
		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || !_pendingTrigger)
		{
			return;
		}

		_pendingTrigger = false;
		Status = RelicStatus.Normal;
		int blockAmount = FloorToInt(player.Creature.MaxHp * 0.6m);
		Flash();
		if (blockAmount > 0)
		{
			await CreatureCmd.GainBlock(player.Creature, blockAmount, ValueProp.Unpowered, null);
		}

		await PowerCmd.Apply<ShrinkPower>(player.Creature, 1m, player.Creature, null);
	}
}
