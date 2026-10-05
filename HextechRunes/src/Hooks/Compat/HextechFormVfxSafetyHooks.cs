namespace HextechRunes;

// 自定义角色场景不一定提供 0.110 起新增的 %FormVfx 容器。形态能力本身不依赖该节点，
// 因此容器缺失时只跳过纯视觉挂载与清理，避免战斗结算和放弃流程被 VFX 异常中断。
// 补丁本体只在 0.110+ 存在，见 HextechFormVfxSafetyHooks.Official.cs。
internal static partial class HextechFormVfxSafetyHooks
{
	internal enum FormVfxKind
	{
		Other,
		Demon,
		Serpent
	}

	/// <summary>持有战争交响乐时,新形态特效进来后是否保留已有的这一层。</summary>
	internal static bool ShouldPreserveExistingForSymphony(FormVfxKind incoming, FormVfxKind existing)
	{
		return existing is FormVfxKind.Demon or FormVfxKind.Serpent
			&& existing != incoming;
	}
}
