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

public sealed class MakeItMineRune : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	private int _stacks;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedStacks
	{
		get => _stacks;
		set
		{
			_stacks = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => true;

	public override int DisplayAmount => !IsCanonical ? _stacks : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new SummonVar(4m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (IsNetworkMultiplayer())
		{
			return Task.CompletedTask;
		}

		return ApplySharedCombatVictory(room);
	}

	public Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		SavedStacks++;
		Flash();
		return Task.CompletedTask;
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
#else
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
#endif
	{
		if (player != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState?.RoundNumber > 1
			|| _stacks <= 0
			|| !IsNecrobinderPlayer(player))
		{
			return;
		}

		Flash();
		await OstyCmd.Summon(choiceContext, player, _stacks * DynamicVars.Summon.BaseValue, this);
	}
}
