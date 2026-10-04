using MegaCrit.Sts2.Core.Models.Monsters;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

// Boss 转阶段后补发开局海克斯。战斗追踪里的 DoormakerRealStartApplied 是已删除的 Doormaker 延迟补发
// 留下的存档字段，保留以维持 JSON 形状。
internal sealed partial class HextechMayhemModifier
{
	private static readonly FieldInfo? TestSubjectRespawnsField = TryGetField(typeof(TestSubject), "_respawns");

	public override async Task AfterOstyRevived(Creature osty)
	{
		if (osty.Side != CombatSide.Enemy
			|| !osty.IsAlive
			|| osty.CombatState?.RunState != ActiveRunState
			|| osty.Monster is not TestSubject testSubject
			|| osty.CombatId == null
			|| ActiveRunState.CurrentRoom is not CombatRoom room)
		{
			return;
		}

		// 字段缺失时 TryGetField 已记入启动摘要,这里按"未复活"处理。
		int respawns = TestSubjectRespawnsField?.GetValue(testSubject) is int value ? value : 0;
		if (respawns <= 0)
		{
			return;
		}

		uint combatId = osty.CombatId.Value;
		int lastAppliedPhase = CombatTracking.TestSubjectPhaseStartApplied.GetValueOrDefault(combatId, 0);
		if (lastAppliedPhase >= respawns)
		{
			return;
		}

		CombatTracking.TestSubjectPhaseStartApplied[combatId] = respawns;
		HextechLog.Info("Mayhem", $"Reapplying boss start hexes after TestSubject revive: combatId={combatId} respawns={respawns}");
		await ApplyBossStartHexesToEnemy(osty, room);
		HextechEnemyUi.Refresh(this);
	}

	private async Task ApplyBossStartHexesToEnemy(Creature creature, CombatRoom room)
	{
		// 转阶段补发的是"开局类"海克斯增益,薄暮法衣不应镜像(等同战斗开始时的增益)。
		using (TwilightVeilRune.BeginMirrorSuppression())
		{
			await ApplyPersistentMonsterHexes(creature, replayOneShotPowers: true);
			await HextechEnemyHexDispatcher.ForEachActive(
				this,
				(effect, context) => effect.ApplyOpeningCombatStartToEnemy(
					context,
					creature,
					room,
					replayOneShotPowers: true));
			await ApplyMonsterCombatStartHexesToEnemy(creature, room);
		}
	}
}
