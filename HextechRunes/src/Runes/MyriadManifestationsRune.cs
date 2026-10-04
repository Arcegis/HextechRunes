namespace HextechRunes;

public sealed class MyriadManifestationsRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player) => IsDefectPlayer(player);

	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (Owner.PlayerCombatState == null || Owner.Creature.Side != side || Owner.Creature.IsDead
			|| !HextechTurnParticipants.Includes(participants, Owner))
		{
			return;
		}

		OrbModel[] orbs = Owner.PlayerCombatState.OrbQueue.Orbs.ToArray();
		int rounds = CountOrbTypes(orbs);
		if (rounds == 0)
		{
			return;
		}

		Flash();
		// 触发开始时冻结种类数和球的顺序。保留原版被动次数修饰及相关回调，兼容增强被动的效果和其他模组充能球。
		await OrbSnapshotPassiveHelper.TriggerRounds(
			Owner,
			orbs,
			rounds,
			orb => HextechOrbPassiveCompat.TriggerPassive(choiceContext, orb));
	}

	internal static int CountOrbTypes(IEnumerable<OrbModel> orbs) => orbs.Select(orb => orb.Id).Distinct().Count();
}
