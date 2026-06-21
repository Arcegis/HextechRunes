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
	private const int CreatureStatHardCap = 999999999;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MaxHpGainPercent", 0.05m)
	];

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedHpGainedThisCombat
	{
		get => 0;
		set { }
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedMaxHpGainCapThisCombat
	{
		get => 0;
		set { }
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

		int rewardMaxHp = GetRewardMaxHpForDeath(target);
		int hpGain = Math.Max(1, FloorToInt(rewardMaxHp * DynamicVars["MaxHpGainPercent"].BaseValue));
		if (hpGain <= 0)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainMaxHp(Owner.Creature, hpGain);
	}

	internal static int GetRewardMaxHpForDeath(Creature target)
	{
		if (!IsTransientInfiniteHpState(target))
		{
			return target.MaxHp;
		}

		int initialMaxHp = GetScaledInitialMonsterMaxHp(target);
		return initialMaxHp > 0
			? Math.Min(target.MaxHp, initialMaxHp)
			: target.MaxHp;
	}

	private static bool IsTransientInfiniteHpState(Creature target)
	{
		return target.HpDisplay is HpDisplay.InfiniteWithNumbers or HpDisplay.InfiniteWithoutNumbers
			|| target.MaxHp >= CreatureStatHardCap;
	}

	private static int GetScaledInitialMonsterMaxHp(Creature target)
	{
		int initialMaxHp = target.MonsterMaxHpBeforeModification ?? target.Monster?.MaxInitialHp ?? 0;
		if (initialMaxHp <= 0)
		{
			return 0;
		}

		if (target.CombatState == null || target.CombatState.Players.Count <= 1)
		{
			return initialMaxHp;
		}

		decimal scaledMaxHp = Creature.ScaleHpForMultiplayer(
			initialMaxHp,
			target.CombatState.Encounter,
			target.CombatState.Players.Count,
			target.CombatState.RunState.CurrentActIndex);
		return Math.Clamp(FloorToInt(scaledMaxHp), 1, CreatureStatHardCap);
	}
}
