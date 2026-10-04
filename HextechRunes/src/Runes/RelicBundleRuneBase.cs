namespace HextechRunes;

/// <summary>
/// 捆包系列：拾取时按固定顺序获得一组原版遗物，悬浮提示列出这些遗物。
/// </summary>
public abstract class RelicBundleRuneBase : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected abstract IReadOnlyList<Type> BundledRelicTypes { get; }

	// 欧洛巴斯祝福历来不在拾取时闪光，保持原表现。
	protected virtual bool FlashOnObtain => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => BundledRelicHoverTips(BundledRelicTypes);

	public override async Task AfterObtained()
	{
		if (FlashOnObtain)
		{
			Flash();
		}

		await RelicBundleGrantHelper.GrantRelics(Owner, BundledRelicTypes);
	}
}
