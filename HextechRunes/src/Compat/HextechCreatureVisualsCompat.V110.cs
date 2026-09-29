#if STS2_110_OR_NEWER && !STS2_111_OR_NEWER
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HextechRunes;

internal static class HextechCreatureVisualsCompat
{
	// 0.110.0 的形态特效容器是公开字段 _formVfxHolder；0.111.0 起改为 FormVfxHolder 属性。
	internal static Control? GetFormVfxHolder(NCreatureVisuals visuals) => visuals._formVfxHolder;
}
#endif
