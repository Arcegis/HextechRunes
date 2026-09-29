#if STS2_111_OR_NEWER
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HextechRunes;

internal static class HextechCreatureVisualsCompat
{
	// 0.111.0 起形态特效容器为 FormVfxHolder 属性（0.110.0 是 _formVfxHolder 字段）。
	internal static Control? GetFormVfxHolder(NCreatureVisuals visuals) => visuals.FormVfxHolder;
}
#endif
