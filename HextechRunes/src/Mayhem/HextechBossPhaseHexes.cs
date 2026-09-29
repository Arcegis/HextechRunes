using MegaCrit.Sts2.Core.Models.Monsters;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

/// <summary>
/// Boss 转阶段后补发开局海克斯的判定与流程。由 <see cref="HextechMayhemModifier"/> 的分部转发进来。
/// 战斗追踪里的 DoormakerRealStartApplied 是已删除的 Doormaker 延迟补发留下的存档字段，保留以维持 JSON 形状。
/// </summary>
internal static class HextechBossPhaseHexes
{
	private static readonly FieldInfo? TestSubjectRespawnsField = TryGetField(typeof(TestSubject), "_respawns");

	internal static async Task AfterOstyRevived(HextechMayhemModifier modifier, Creature osty)
	{
		if (osty.Side != CombatSide.Enemy
			|| !osty.IsAlive
			|| osty.CombatState?.RunState != modifier.ActiveRunState
			|| osty.Monster is not TestSubject testSubject
			|| osty.CombatId == null
			|| modifier.ActiveRunState.CurrentRoom is not CombatRoom room)
		{
			return;
		}

		int respawns = GetTestSubjectRespawns(testSubject);
		if (respawns <= 0)
		{
			return;
		}

		uint combatId = osty.CombatId.Value;
		int lastAppliedPhase = modifier.CombatTracking.TestSubjectPhaseStartApplied.GetValueOrDefault(combatId, 0);
		if (lastAppliedPhase >= respawns)
		{
			return;
		}

		modifier.CombatTracking.TestSubjectPhaseStartApplied[combatId] = respawns;
		HextechLog.Info("Mayhem", $"Reapplying boss start hexes after TestSubject revive: combatId={combatId} respawns={respawns}");
		await modifier.ApplyBossStartHexesToEnemy(osty, room);
		HextechEnemyUi.Refresh(modifier);
	}

	private static int GetTestSubjectRespawns(TestSubject testSubject)
	{
		return NormalizeTestSubjectRespawns(TestSubjectRespawnsField?.GetValue(testSubject));
	}

	internal static int NormalizeTestSubjectRespawns(object? fieldValue)
	{
		return fieldValue is int respawns ? Math.Max(0, respawns) : 0;
	}
}
