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

public sealed class WatchOutGrapefruitRune : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	private static readonly Type[] FoodRelicTypes =
	[
		typeof(Strawberry),
		typeof(Pear),
		typeof(Mango),
		typeof(DragonFruit),
		typeof(LoomingFruit),
		typeof(LeesWaffle),
		typeof(YummyCookie),
		typeof(MeatOnTheBone),
		typeof(PaelsFlesh),
		typeof(IceCream),
		typeof(Bread),
		typeof(NutritiousOyster),
		typeof(VeryHotCocoa),
		typeof(FragrantMushroom),
		typeof(BigMushroom)
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

			Type[] candidates = Owner.GetRelic<IceCream>() == null
				? FoodRelicTypes
				: FoodRelicTypes.Where(static type => type != typeof(IceCream)).ToArray();
			Type relicType = HextechStableRandom.Pick(
				candidates,
				(RunState)Owner.RunState,
				HextechStableRandom.TypeModelKey,
				"treat-yourself-food-relic",
				HextechStableRandom.PlayerKey(Owner),
				Owner.Relics.Count.ToString());
			RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(relicType)).ToMutable();
		Flash(Array.Empty<Creature>());
		room.AddExtraReward(Owner, new RelicReward(relic, Owner));
		return Task.CompletedTask;
	}
}
