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

public sealed class JeweledGauntletRune : HextechRelicBase
{
	private int _replayRollsThisCombat;
	private CardModel? _pendingReplayRollCard;
	private bool _pendingReplayRollResult;

	public override Task BeforeCombatStart()
	{
		ResetReplayRollState();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetReplayRollState();
		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (Owner == null || card.Owner != Owner)
		{
			ClearPendingReplayRoll();
			return playCount;
		}

		int ordinal = ConsumeCombatProcOrdinal(nameof(JeweledGauntletRune), ref _replayRollsThisCombat);
		bool shouldReplay = HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			33,
			"jeweled-gauntlet-replay",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			ordinal.ToString(),
			TargetKey(target),
			HextechStableRandom.CardKey(card));
		_pendingReplayRollCard = card;
		_pendingReplayRollResult = shouldReplay;
		return shouldReplay ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (Owner != null && card.Owner == Owner && _pendingReplayRollCard == card && _pendingReplayRollResult)
		{
			Flash();
		}

		if (_pendingReplayRollCard == card)
		{
			ClearPendingReplayRoll();
		}

		return Task.CompletedTask;
	}

	private static string TargetKey(Creature? target)
	{
		return target?.CombatId?.ToString() ?? target?.Side.ToString() ?? "none";
	}

	private void ResetReplayRollState()
	{
		_replayRollsThisCombat = 0;
		ClearPendingReplayRoll();
	}

	private void ClearPendingReplayRoll()
	{
		_pendingReplayRollCard = null;
		_pendingReplayRollResult = false;
	}
}
