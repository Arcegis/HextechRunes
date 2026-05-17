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
	private int _debuffsThisCombat;
	private int _hpGainedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedDebuffsThisCombat
	{
		get => _debuffsThisCombat;
		set => _debuffsThisCombat = Math.Max(0, value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedHpGainedThisCombat
	{
		get => _hpGainedThisCombat;
		set => _hpGainedThisCombat = Math.Max(0, value);
	}

	public override Task BeforeCombatStart()
	{
		_debuffsThisCombat = 0;
		_hpGainedThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_debuffsThisCombat = 0;
		_hpGainedThisCombat = 0;
		return Task.CompletedTask;
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (!TryGetOwnedEnemyDebuffTarget(power, amount, applier, out _))
		{
			return;
		}

		_debuffsThisCombat++;
		if (Owner == null || _hpGainedThisCombat >= 10 || _debuffsThisCombat % 3 != 0)
		{
			return;
		}

		_hpGainedThisCombat++;
		Flash();
		await CreatureCmd.GainMaxHp(Owner.Creature, 1m);
	}
}
