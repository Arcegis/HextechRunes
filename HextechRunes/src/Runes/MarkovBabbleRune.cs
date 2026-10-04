using MegaCrit.Sts2.Core.Entities.Orbs;

namespace HextechRunes;

public sealed class MarkovBabbleRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbCount", 1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		// 发放闸门之外，外部接口(RelicBundleGrantHelper)、控制台或其他模组可把本符文直接给任意角色，触发时再判角色。
		if (side != Owner.Creature.Side || Owner.Creature.IsDead || !IsDefectOwner)
		{
			return;
		}

		OrbQueue? orbQueue = Owner.PlayerCombatState?.OrbQueue;
		int emptySlots = Math.Max(0, (orbQueue?.Capacity ?? 0) - (orbQueue?.Orbs.Count ?? 0));
		if (emptySlots <= 0)
		{
			return;
		}

		Flash();
		for (int i = 0; i < emptySlots; i++)
		{
			OrbModel orb = HextechStableCombatSpawns.CreateOrb(
				(RunState)Owner.RunState,
				Owner,
				"markov-babble-turn-start",
				i,
				combatState.RoundNumber);
			await OrbCmd.Channel(new BlockingPlayerChoiceContext(), orb, Owner);
		}
	}
}
