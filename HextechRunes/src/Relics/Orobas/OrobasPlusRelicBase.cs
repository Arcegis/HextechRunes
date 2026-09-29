using MegaCrit.Sts2.Core.Entities.Relics;

namespace HextechRunes;

// 保留原版起始稀有度，图鉴只枚举角色 StartingRelics 与第一次升级结果，因此默认不展示二次升级。
// 不继承 HextechRelicBase：这些是角色遗物，不应被重铸、海克斯计数或候选池识别为符文。
public abstract class OrobasPlusRelicBase : RelicModel
{
	public sealed override RelicRarity Rarity => RelicRarity.Starter;

	public sealed override bool IsAllowedInShops => false;

	protected abstract RelicModel OriginalRelic { get; }

	protected sealed override string IconBaseName => OriginalRelic.Id.Entry.ToLowerInvariant();

	// 与 HextechRelicBase.Flash 同一约定：原版 Flash 同步调用本机 UI 订阅者，只隔离表现回调，
	// 不包裹调用方的状态修改。
	public new void Flash()
	{
		try
		{
			base.Flash();
		}
		catch (Exception ex)
		{
			HextechLog.Warn("RelicVisual", $"Flash failed for {GetType().Name}: {ex.Message}");
		}
	}
}
