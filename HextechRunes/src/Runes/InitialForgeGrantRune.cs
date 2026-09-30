namespace HextechRunes;

public abstract class InitialForgeGrantRune : HextechRelicBase
{
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedInitialForgeGrantPending { get; set; }

	// 本次拾取效果已完成的锻造器步数（含被配置禁用拦下的一步），随存档保存，让补发幂等、只发剩余数量。
	// 2026-09-30 新增：SavedProperty 集合变化，与旧版本不能联机。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedInitialForgeGrantCompletedCount { get; set; }

	public override bool HasUponPickupEffect => true;

	protected abstract int InitialForgeCount { get; }

	public sealed override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		// RelicCmd 已在调用 AfterObtained 前把本体放进背包。这个标记必须在第一次打开选择界面前落下，
		// 让意外留下的存档能区分“拾取效果已完成”和“只有海克斯本体已入库”。
		// 单机海克斯选择途中保存并退出不会留下这种存档（读档回滚到选择前重选）；这里只是联机或其他来源的兜底。
		SavedInitialForgeGrantCompletedCount = 0;
		SavedInitialForgeGrantPending = true;
		Flash();
		await ResumePendingInitialForgeGrant();
	}

	internal async Task<bool> ResumePendingInitialForgeGrant()
	{
		if (!SavedInitialForgeGrantPending)
		{
			return true;
		}

		if (Owner == null)
		{
			return false;
		}

		int total = Math.Max(0, InitialForgeCount);
		int completed = ResolveCompletedForgeCount(
			total,
			SavedInitialForgeGrantCompletedCount,
			CountForgesObtainedAfter(Owner.Relics, this));
		SavedInitialForgeGrantCompletedCount = completed;
		int remaining = total - completed;
		bool finished = remaining == 0
			|| await HextechForgeGrantHelper.TryObtainRandomForges(
				Owner,
				remaining,
				firstOrdinal: completed,
				afterEachForge: () => SavedInitialForgeGrantCompletedCount++);
		if (finished)
		{
			SavedInitialForgeGrantPending = false;
			SavedInitialForgeGrantCompletedCount = 0;
		}

		return finished;
	}

	// 已完成数取保存值与背包推断值的较大者，封顶为总数：宁可少补也不重复发放。
	// 旧存档没有完成数（读回为 0），只能靠背包推断；新存档两者通常相等。
	internal static int ResolveCompletedForgeCount(int total, int savedCompleted, int forgesObtainedAfterRune)
	{
		return Math.Clamp(Math.Max(savedCompleted, forgesObtainedAfterRune), 0, Math.Max(0, total));
	}

	// 原版背包列表按获得顺序追加（RelicCmd.Obtain 默认 index -1，本模组没有插入式获得），
	// 存档按列表顺序写出、读回时按同一顺序追加（0.111.0 Player.ToSerializable / PopulateRelics），所以读档后顺序不变。
	// 只数排在本符文之后、与本符文同一楼层获得的海克斯锻造器：拾取效果在同一楼层内发放，
	// 同楼层条件挡掉补发失败后继续游戏、在之后楼层拿到的锻造器。
	internal static int CountForgesObtainedAfter(IReadOnlyList<RelicModel> relics, RelicModel rune)
	{
		int runeIndex = -1;
		for (int i = 0; i < relics.Count; i++)
		{
			if (ReferenceEquals(relics[i], rune))
			{
				runeIndex = i;
				break;
			}
		}

		if (runeIndex < 0)
		{
			return 0;
		}

		int count = 0;
		for (int i = runeIndex + 1; i < relics.Count; i++)
		{
			RelicModel relic = relics[i];
			if (relic.FloorAddedToDeck == rune.FloorAddedToDeck && HextechCatalog.IsHextechForgeRelic(relic))
			{
				count++;
			}
		}

		return count;
	}
}
