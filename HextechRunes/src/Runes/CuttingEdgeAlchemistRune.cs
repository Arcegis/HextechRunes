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

public sealed class CuttingEdgeAlchemistRune : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("RarePotionCount", 1m),
		new DynamicVar("UncommonPotionCount", 1m)
	];

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

		List<PotionModel> potionOptions = HextechGameApiCompat.GetPotionOptions(Owner).ToList();
		bool added = AddPotionRewards(
			room,
			Owner,
			potionOptions,
			PotionRarity.Rare,
			DynamicVars["RarePotionCount"].IntValue,
			"cutting-edge-alchemist-rare-reward");
		added |= AddPotionRewards(
			room,
			Owner,
			potionOptions,
			PotionRarity.Uncommon,
			DynamicVars["UncommonPotionCount"].IntValue,
			"cutting-edge-alchemist-uncommon-reward");

		if (added)
		{
			Flash(Array.Empty<Creature>());
		}

		return Task.CompletedTask;
	}

	private static bool AddPotionRewards(
		CombatRoom room,
		Player player,
		IReadOnlyList<PotionModel> potionOptions,
		PotionRarity rarity,
		int count,
		string source)
	{
		if (count <= 0)
		{
			return false;
		}

		List<PotionModel> candidates = potionOptions
			.Where(potion => potion.Rarity == rarity)
			.ToList();
		if (candidates.Count == 0)
		{
			return false;
		}

		for (int i = 0; i < count; i++)
		{
			PotionModel potion = HextechStableRandom.Pick(
				candidates,
				(RunState)player.RunState,
				HextechStableRandom.PotionKey,
				source,
				HextechStableRandom.PlayerKey(player),
				i.ToString()).ToMutable();
			room.AddExtraReward(player, new PotionReward(potion, player));
		}

		return true;
	}
}
