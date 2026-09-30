namespace HextechRunes;

public abstract class InitialForgeGrantRune : HextechRelicBase
{
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedInitialForgeGrantPending { get; set; }

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
		// 只存布尔、不存已发数量：恢复时按完整数量重发。补存剩余数量要新增 SavedProperty，会改变联机 net-id 布局。
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

		int count = Math.Max(0, InitialForgeCount);
		bool completed = count == 0
			|| await HextechForgeGrantHelper.TryObtainRandomForges(Owner, count);
		if (completed)
		{
			SavedInitialForgeGrantPending = false;
		}

		return completed;
	}
}
