using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class DiceManiacRune : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	private const int SilverForgeWeight = 65;
	private const int GoldForgeWeight = 25;
	private const int PrismaticForgeWeight = 10;
	internal const int ForgeRarityMultiplier = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DropChance", 50m),
		new DynamicVar("ForgeMultiplier", 2m)
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

		if (!HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			DynamicVars["DropChance"].IntValue,
			"dice-maniac-forge-reward",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Relics.Count.ToString()))
		{
			return Task.CompletedTask;
		}

		Flash(Array.Empty<Creature>());
		HextechForgeGrantHelper.AddWeightedRandomForgeReward(
			Owner,
			room,
			"dice-maniac-random-forge-reward",
			SilverForgeWeight,
			GoldForgeWeight,
			PrismaticForgeWeight);
		return Task.CompletedTask;
	}
}
