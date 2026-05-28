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

public sealed class DieForYouRune : HextechRelicBase
{
	private int _pendingWishAmount;
	private int _lastRecordedRound = -1;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPendingWishAmount
	{
		get => _pendingWishAmount;
		set => _pendingWishAmount = Math.Max(0, value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedLastRecordedRound
	{
		get => _lastRecordedRound;
		set => _lastRecordedRound = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new SummonVar(5m),
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard(OstyWishCard.CreatePlaceholderPreview())
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
#else
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
#endif
	{
		if (player != Owner || Owner.Creature.IsDead || !IsNecrobinderPlayer(player))
		{
			return;
		}

		Flash();
		await OstyCmd.Summon(choiceContext, player, DynamicVars.Summon.BaseValue, this);
		await AddPendingWishCard(choiceContext, player);
	}

	public override Task BeforeCombatStart()
	{
		ResetCombatState();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetCombatState();
		return Task.CompletedTask;
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		HextechCombatState? combatState = target.CombatState;
		if (Owner == null
			|| wasRemovalPrevented
			|| Owner.Creature.IsDead
			|| target.PetOwner != Owner
			|| target.Monster is not Osty
			|| combatState == null)
		{
			return Task.CompletedTask;
		}

		int roundNumber = combatState.RoundNumber;
		if (_lastRecordedRound == roundNumber)
		{
			return Task.CompletedTask;
		}

		int amount = FloorToInt(target.MaxHp);
		if (amount <= 0)
		{
			return Task.CompletedTask;
		}

		_lastRecordedRound = roundNumber;
		_pendingWishAmount = amount;
		Flash();

		return Task.CompletedTask;
	}

	private async Task AddPendingWishCard(PlayerChoiceContext choiceContext, Player player)
	{
		if (_pendingWishAmount <= 0
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| !CombatManager.Instance.IsInProgress
			|| CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}

		int amount = _pendingWishAmount;
		_pendingWishAmount = 0;

		CardModel card = combatState.CreateCard<OstyWishCard>(player);
		OstyWishCard.SetWishAmount(card, amount);
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}

	private void ResetCombatState()
	{
		_pendingWishAmount = 0;
		_lastRecordedRound = -1;
	}
}
