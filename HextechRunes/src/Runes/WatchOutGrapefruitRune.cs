using MegaCrit.Sts2.Core.Models.Relics;

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
