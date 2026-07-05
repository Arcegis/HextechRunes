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
