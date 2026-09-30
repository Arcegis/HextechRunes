using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunes.Tests;

internal static partial class Program
{
	/// <summary>
	/// 原版 RunManager.CleanUp 在清覆盖层时 State 仍指向本局、IsCleaningUp 为 true；这段时间续跑的选择任务链
	/// 必须被视为“本局已不在进行”，否则会继续标记本幕已决。
	/// </summary>
	[HextechTest]
	private static void ReviewSaveLoadRollbackCleanupIsNotAnInProgressRun()
	{
		object run = new();
		object otherRun = new();
		Expect(HextechRuneSelectionCoordinator.IsRunInProgress(run, run, isCleaningUp: false), "current run outside cleanup is in progress");
		Expect(!HextechRuneSelectionCoordinator.IsRunInProgress(run, run, isCleaningUp: true), "current run during save-and-quit cleanup is not in progress");
		Expect(!HextechRuneSelectionCoordinator.IsRunInProgress(otherRun, run, isCleaningUp: false), "a different current run means the old run ended");
		Expect(!HextechRuneSelectionCoordinator.IsRunInProgress(null, run, isCleaningUp: false), "no current run means the old run ended");
	}

	/// <summary>
	/// 本模组在已完成的先古/涅奥房里存档时，必须把该房作为 preFinishedRoom 写入（与原版 EventRoom 完成时的存档同口径）；
	/// 否则读档会按地图坐标把先古当新事件重开。其他房间仍传 null。
	/// </summary>
	[HextechTest]
	private static void ReviewSaveLoadRollbackSaveKeepsFinishedEventRoom()
	{
		// 测试进程的 ModelDb 没有注册事件模型；只需要房间类型与原版 MarkPreFinished 写入的完成标志，跳过构造函数。
		EventRoom finishedAncient = CreateBareEventRoom();
		finishedAncient.MarkPreFinished();
		Expect(
			ReferenceEquals(finishedAncient, HextechRuneSelectionCoordinator.SelectPreFinishedRoomForSave(finishedAncient)),
			"finished event room is saved as the pre-finished room");

		EventRoom unfinishedAncient = CreateBareEventRoom();
		Expect(
			HextechRuneSelectionCoordinator.SelectPreFinishedRoomForSave(unfinishedAncient) == null,
			"unfinished event room keeps the original null pre-finished room");
		Expect(
			HextechRuneSelectionCoordinator.SelectPreFinishedRoomForSave(new MapRoom()) == null,
			"map room keeps the original null pre-finished room");
		Expect(
			HextechRuneSelectionCoordinator.SelectPreFinishedRoomForSave(null) == null,
			"no current room keeps the original null pre-finished room");
	}

	/// <summary>
	/// 单机选择链：拾取（含棱彩海克斯连续锻造）返回后先复查本局仍在进行，才能回到调用方；
	/// 本幕完成前也要复查一次，退出清理中被取消的发放不会标记已决、不会存档。两处存档都经 preFinishedRoom 选择。
	/// </summary>
	[HextechTest]
	private static void ReviewSaveLoadRollbackSelectionRechecksRunBeforeResolving()
	{
		string[] singleplayerCalls = GetCoordinatorAsyncCallNames("SelectSinglePlayerRuneAsync");
		int obtainIndex = Array.LastIndexOf(singleplayerCalls, "Obtain");
		Expect(obtainIndex >= 0, "single-player selection obtains the chosen rune");
		Expect(
			Array.IndexOf(singleplayerCalls, "IsCurrentRunInProgress", obtainIndex) > obtainIndex,
			"single-player selection rechecks the run after the pickup effect finishes");

		string[] stageCalls = GetCoordinatorAsyncCallNames("HandleStageSelection");
		int playerSelectionsIndex = Array.IndexOf(stageCalls, "RunStagePlayerSelectionsAsync");
		int completeIndex = Array.LastIndexOf(stageCalls, "CompleteStageAsync");
		int recheckIndex = Array.IndexOf(stageCalls, "IsCurrentRunInProgress", playerSelectionsIndex);
		Expect(playerSelectionsIndex >= 0 && completeIndex > playerSelectionsIndex, "stage completes after player selections");
		Expect(
			recheckIndex > playerSelectionsIndex && recheckIndex < completeIndex,
			"stage rechecks the run (including cleanup) before marking the act resolved");

		foreach (string saveMethod in new[] { "PersistActSelection", "PersistRuneSelectionCheckpoint" })
		{
			string[] saveCalls = GetCoordinatorAsyncCallNames(saveMethod);
			int roomIndex = Array.IndexOf(saveCalls, "SelectPreFinishedRoomForSave");
			int saveIndex = Array.IndexOf(saveCalls, "SaveRun");
			Expect(roomIndex >= 0 && saveIndex > roomIndex, $"{saveMethod} passes the finished event room to SaveRun");
		}
	}

	private static EventRoom CreateBareEventRoom()
	{
		return (EventRoom)RuntimeHelpers.GetUninitializedObject(typeof(EventRoom));
	}

	private static string[] GetCoordinatorAsyncCallNames(string methodName)
	{
		MethodInfo method = typeof(HextechRuneSelectionCoordinator).GetMethod(
			methodName,
			BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
			?? throw new MissingMethodException(nameof(HextechRuneSelectionCoordinator), methodName);
		return PatchProcessor.GetOriginalInstructions(GetAsyncStateMachineMoveNext(method))
			.Select(static instruction => instruction.operand)
			.OfType<MethodInfo>()
			.Select(static call => call.Name)
			.ToArray();
	}
}
