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

public sealed class SoulEaterRune : HextechRelicBase
{
	private int _hpGainedThisCombat;
	private int _maxHpGainCapThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("EnemyMaxHpGainPercent", 5m),
		new DynamicVar("OwnerMaxHpGainCapPercent", 5m)
	];

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedHpGainedThisCombat
	{
		get => _hpGainedThisCombat;
		set => _hpGainedThisCombat = Math.Max(0, value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedMaxHpGainCapThisCombat
	{
		get => _maxHpGainCapThisCombat;
		set => _maxHpGainCapThisCombat = Math.Max(0, value);
	}

	public override Task BeforeCombatStart()
	{
		_hpGainedThisCombat = 0;
		_maxHpGainCapThisCombat = CalculateMaxHpGainCap();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_hpGainedThisCombat = 0;
		_maxHpGainCapThisCombat = 0;
		return Task.CompletedTask;
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented
			|| Owner == null
			|| Owner.Creature.IsDead
			|| target.Side == Owner.Creature.Side
			|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(target))
		{
			return;
		}

		if (_maxHpGainCapThisCombat <= 0)
		{
			_maxHpGainCapThisCombat = CalculateMaxHpGainCap();
		}

		int remaining = _maxHpGainCapThisCombat - _hpGainedThisCombat;
		if (remaining <= 0)
		{
			return;
		}

		int hpGain = Math.Max(1, FloorToInt(target.MaxHp * DynamicVars["EnemyMaxHpGainPercent"].BaseValue / 100m));
		hpGain = Math.Min(hpGain, remaining);
		if (hpGain <= 0)
		{
			return;
		}

		_hpGainedThisCombat += hpGain;
		Flash();
		await CreatureCmd.GainMaxHp(Owner.Creature, hpGain);
	}

	private int CalculateMaxHpGainCap()
	{
		return Owner == null
			? 0
			: Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["OwnerMaxHpGainCapPercent"].BaseValue / 100m));
	}
}
