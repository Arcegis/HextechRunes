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

public sealed class TwiceThriceRune : HextechRelicBase
{
	private const int AttacksPerReplay = 3;

	private int _attacksPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisCombat
	{
		get => IsNetworkMultiplayer() ? 0 : GetAttacksPlayedThisCombat();
		set
		{
			_attacksPlayedThisCombat = Math.Max(0, value) % AttacksPerReplay;
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			return GetAttacksPlayedThisCombat();
		}
	}

	public override Task BeforeCombatStart()
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (!IsOwnedAttack(card))
		{
			return playCount;
		}

		int nextAttacksPlayed = GetAttacksPlayedBeforeCurrentAttack() + 1;
		if (nextAttacksPlayed % AttacksPerReplay == 0)
		{
			return playCount + 1;
		}

		return playCount;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsCountedAttackPlay(cardPlay))
		{
			return Task.CompletedTask;
		}

		if (!ShouldUseNetworkCombatHistory())
		{
			_attacksPlayedThisCombat = (_attacksPlayedThisCombat + 1) % AttacksPerReplay;
		}

		InvokeDisplayAmountChanged();
		Flash();
		return Task.CompletedTask;
	}

	private void ResetAttacksPlayedThisCombat()
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
	}

	private int GetAttacksPlayedThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedAttackCardsPlayedFromHistory() % AttacksPerReplay
			: _attacksPlayedThisCombat;
	}

	private int GetAttacksPlayedBeforeCurrentAttack()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedAttackCardsPlayedFromHistory()
			: _attacksPlayedThisCombat;
	}

	private bool IsCountedAttackPlay(CardPlay cardPlay)
	{
		return cardPlay.IsFirstInSeries
			&& !cardPlay.IsAutoPlay
			&& IsOwnedAttack(cardPlay.Card);
	}
}
