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

public sealed class MikaelsBlessingRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("PotionCount", 2m),
		new DynamicVar("HealPercent", 20m)
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		List<PotionModel> candidates = PotionFactory.GetPotionOptions(Owner, Array.Empty<PotionModel>()).ToList();
		if (candidates.Count == 0)
		{
			return;
		}

		Flash(Array.Empty<Creature>());
		for (int i = 0; i < DynamicVars["PotionCount"].IntValue; i++)
		{
			PotionModel potion = HextechStableRandom.Pick(
				candidates,
				(RunState)Owner.RunState,
				HextechStableRandom.PotionKey,
				"mikaels-blessing-potion",
				HextechStableRandom.PlayerKey(Owner),
				i.ToString()).ToMutable();
			await PotionCmd.TryToProcure(potion, Owner);
		}
	}

	public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
	{
		if (Owner == null || Owner.Creature.IsDead || !IsPotionUseOwnedByOrTargetingOwner(potion, target))
		{
			return;
		}

		Flash();
		int healAmount = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m));
		await CreatureCmd.Heal(Owner.Creature, healAmount);

		List<PowerModel> negativePowers = Owner.Creature.Powers
			.Where(static power => power.GetTypeForAmount(power.Amount) == PowerType.Debuff)
			.ToList();
		foreach (PowerModel power in negativePowers)
		{
			await PowerCmd.Remove(power);
		}
	}
}
