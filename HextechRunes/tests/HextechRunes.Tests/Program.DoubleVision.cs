using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using HextechRunes;
using FormVfxKind = HextechRunes.HextechFormVfxSafetyHooks.FormVfxKind;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Text.Json;

namespace HextechRunes.Tests;

internal static partial class Program
{
	[HextechTest]
	private static void UniversalScopeChancesAddBeforeSingleRoll()
	{
		Equal(15, UniversalScopeRuneBase.CombineChancePercent([ 15 ]), "one scope keeps its own chance");
		Equal(45, UniversalScopeRuneBase.CombineChancePercent([ 15, 30 ]), "two scope chances add directly");
		Equal(95, UniversalScopeRuneBase.CombineChancePercent([ 15, 30, 50 ]), "all scope chances add directly");
		Equal(100, UniversalScopeRuneBase.CombineChancePercent([ 50, 50, 30 ]), "combined chance is capped at certainty");
	}

	// 联机分叉回归（一呼百应连打 + 最万用的瞄准镜）：返还额只看随出牌同步的实付，
	// 自动打出实付 0 就返还 0，不能因为本机记账缺值退回牌面费用。
	[HextechTest]
	private static void UniversalScopeRefundsOnlyTheSyncedSpend()
	{
		CardModel card = CreateMutableTestModel<MegaCrit.Sts2.Core.Models.Cards.Thunderclap>();
		HextechCardPlayResourceSpend autoPlay = HextechCombatHooks.GetResourceSpend(CreateTestCardPlay(card, isAutoPlay: true, energySpent: 0, starsSpent: 0));
		Equal(0m, autoPlay.Energy, "auto-played cards spent no energy and refund none");
		Equal(0m, autoPlay.Stars, "auto-played cards spent no stars and refund none");
		HextechCardPlayResourceSpend manual = HextechCombatHooks.GetResourceSpend(CreateTestCardPlay(card, isAutoPlay: false, energySpent: 2, starsSpent: 1));
		Equal(2m, manual.Energy, "manual plays refund the energy actually spent");
		Equal(1m, manual.Stars, "manual plays refund the stars actually spent");
	}

	// CardPlay 的成员随版本增减（0.107.1 没有 Player），用未初始化对象 + 反射只设需要的属性。
	private static CardPlay CreateTestCardPlay(CardModel card, bool isAutoPlay, int energySpent, int starsSpent)
	{
		CardPlay cardPlay = (CardPlay)RuntimeHelpers.GetUninitializedObject(typeof(CardPlay));
		AccessTools.Property(typeof(CardPlay), nameof(CardPlay.Card)).SetValue(cardPlay, card);
		AccessTools.Property(typeof(CardPlay), nameof(CardPlay.IsAutoPlay)).SetValue(cardPlay, isAutoPlay);
		AccessTools.Property(typeof(CardPlay), nameof(CardPlay.PlayCount)).SetValue(cardPlay, 1);
		AccessTools.Property(typeof(CardPlay), nameof(CardPlay.Resources)).SetValue(cardPlay, new ResourceInfo
		{
			EnergySpent = energySpent,
			EnergyValue = Math.Max(1, energySpent),
			StarsSpent = starsSpent,
			StarValue = starsSpent
		});
		return cardPlay;
	}

	[HextechTest]
	private static void UniversalScopeUpgradeRestorationKeepsCapturedLevels()
	{
		Equal(3, CardTransformUpgradeHelper.GetUpgradeRestorationSteps(0, 3, 30), "restore all lost multi-upgrade levels");
		Equal(2, CardTransformUpgradeHelper.GetUpgradeRestorationSteps(1, 3, 30), "restore only missing levels");
		Equal(0, CardTransformUpgradeHelper.GetUpgradeRestorationSteps(3, 3, 30), "preserve an unchanged card");
		Equal(0, CardTransformUpgradeHelper.GetUpgradeRestorationSteps(4, 3, 30), "never downgrade a card that gained levels while moving");
		Equal(1, CardTransformUpgradeHelper.GetUpgradeRestorationSteps(0, 3, 1), "respect the card max upgrade level");
	}

	private static DoubleVisionRune.EventRelicTransaction CreateTestEventRelicTransaction()
	{
		return new DoubleVisionRune.EventRelicTransaction(null!, null!, new DoubleVisionRune.EventRelicTransactionBatch());
	}

	private static DoubleVisionRune.EventRelicIntent CreateTestEventRelicIntent()
	{
		return new DoubleVisionRune.EventRelicIntent(null!, null!, []);
	}

	[HextechTest]
	private static void EventRelicTransactionCommitsSequentially()
	{
		DoubleVisionRune.EventRelicTransaction transaction = CreateTestEventRelicTransaction();
		DoubleVisionRune.EventRelicIntent first = CreateTestEventRelicIntent();
		DoubleVisionRune.EventRelicIntent second = CreateTestEventRelicIntent();
		Expect(transaction.TryRecord(first), "open event transaction should accept the first reward");
		Expect(transaction.TryRecord(second), "open event transaction should accept the second reward");
		TaskCompletionSource firstGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		List<DoubleVisionRune.EventRelicIntent> started = [];
		List<DoubleVisionRune.EventRelicIntent> completed = [];

		Task commitTask = transaction.CommitSequentially(async intent =>
		{
			started.Add(intent);
			if (ReferenceEquals(intent, first))
			{
				await firstGate.Task;
			}
			completed.Add(intent);
		});

		Expect(started.Count == 1 && ReferenceEquals(started[0], first), "second event reward must not start before the first reward completes");
		Expect(transaction.IsCommitting, "event transaction should report committing while rewards are pending");
		firstGate.SetResult();
		commitTask.GetAwaiter().GetResult();
		Expect(started.Count == 2 && ReferenceEquals(started[1], second), "event rewards should start in obtain order");
		Expect(completed.Count == 2 && ReferenceEquals(completed[0], first) && ReferenceEquals(completed[1], second), "event rewards should complete sequentially");
		Expect(!transaction.IsCommitting, "event transaction should stop committing after all rewards complete");
	}

	[HextechTest]
	private static void EventRelicTransactionRejectsLateRecordsAndSecondCommit()
	{
		DoubleVisionRune.EventRelicTransaction transaction = CreateTestEventRelicTransaction();
		Expect(transaction.TryRecord(CreateTestEventRelicIntent()), "open event transaction should accept its original reward");
		transaction.CommitSequentially(static _ => Task.CompletedTask).GetAwaiter().GetResult();

		Expect(!transaction.TryRecord(CreateTestEventRelicIntent()), "committed event transaction should reject late records");
		Equal(1, transaction.Count, "late record must not enter the committed event batch");
		ExpectThrows<InvalidOperationException>(
			() => transaction.CommitSequentially(static _ => Task.CompletedTask).GetAwaiter().GetResult(),
			"event transaction should not commit twice");
	}

	[HextechTest]
	private static void EventRelicTransactionTryRecordSkipsLateAsyncRewards()
	{
		DoubleVisionRune.EventRelicTransaction transaction = CreateTestEventRelicTransaction();
		DoubleVisionRune.EventRelicIntent original = CreateTestEventRelicIntent();
		Expect(transaction.TryRecord(original), "open event transaction should accept its original reward");
		transaction.CloseForRecording();
		Expect(!transaction.TryRecord(CreateTestEventRelicIntent()), "closed event transaction should ignore inherited async rewards");

		List<DoubleVisionRune.EventRelicIntent> committed = [];
		transaction.CommitSequentially(intent =>
		{
			committed.Add(intent);
			return Task.CompletedTask;
		}).GetAwaiter().GetResult();

		Expect(committed.Count == 1 && ReferenceEquals(committed[0], original), "late inherited reward must not enter the committed event batch");
	}

	[HextechTest]
	private static void DoubleVisionCopiesWaxStateWithoutCopyingMeltedState()
	{
		DustyTome source = CreateMutableTestModel<DustyTome>();
		source.IsWax = true;
		source.IsMelted = true;
		DustyTome copy = CreateMutableTestModel<DustyTome>();

		DoubleVisionRune.CopyWaxState(source, copy);

		Expect(copy.IsWax, "Double Vision should preserve wax on a copied relic");
		Expect(!copy.IsMelted, "Double Vision should not copy an already-melted state");
	}

	[HextechTest]
	private static void DoubleVisionDustyTomeSuppressionCoversOnlyTheCopyDuringObtain()
	{
		DustyTome source = CreateTestDustyTome();
		DustyTome copy = CreateTestDustyTome();
		DustyTome unrelated = CreateTestDustyTome();
		int obtainCount = 0;

		RelicModel obtained = DoubleVisionRune.RunWithDustyTomeAfterObtainedSuppressed(
			copy,
			() =>
			{
				obtainCount++;
				Expect(DoubleVisionRune.ShouldSuppressDustyTomeAfterObtained(copy), "copied Dusty Tome should suppress its own AfterObtained");
				Task copiedAfterObtained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
				Expect(
					!HextechRewardSafetyHooks.DustyTomePatch.Prefix(copy, ref copiedAfterObtained),
					"copied Dusty Tome AfterObtained prefix should skip the original");
				Expect(copiedAfterObtained.IsCompletedSuccessfully, "copied Dusty Tome AfterObtained should return a completed task");
				Expect(!DoubleVisionRune.ShouldSuppressDustyTomeAfterObtained(source), "source Dusty Tome must not be suppressed");
				Task sourceAfterObtained = Task.CompletedTask;
				Expect(
					HextechRewardSafetyHooks.DustyTomePatch.Prefix(source, ref sourceAfterObtained),
					"source Dusty Tome AfterObtained must still run");
				Expect(!DoubleVisionRune.ShouldSuppressDustyTomeAfterObtained(unrelated), "unrelated Dusty Tome must not be suppressed");
				return Task.FromResult<RelicModel>(copy);
			})
			.GetAwaiter()
			.GetResult();

		Equal(1, obtainCount, "Dusty Tome obtain count");
		Expect(ReferenceEquals(copy, obtained), "suppression scope should return the obtained copy");
		Expect(!DoubleVisionRune.ShouldSuppressDustyTomeAfterObtained(copy), "Dusty Tome suppression must end after obtain");
	}
}
