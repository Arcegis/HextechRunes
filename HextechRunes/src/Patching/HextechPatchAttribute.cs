namespace HextechRunes;

/// <summary>
/// 补丁类元数据，供 <see cref="HextechPatcher"/> 汇报与应用。
/// 目标由 <c>[HarmonyPatch]</c> 声明，或由类内 <c>static void Apply(Harmony)</c> 动态安装。
/// </summary>
/// <remarks>
/// 约定:
/// <list type="bullet">
/// <item><see cref="Id"/> 稳定且唯一,形如 <c>combat.heal</c>,日志与补丁清单快照都按它定位。</item>
/// <item><see cref="Feature"/> 是玩家可感知的功能名(符文名 / 界面名),失败时日志按功能归因。</item>
/// <item><see cref="Rune"/> 指定后，补丁失败会由 <see cref="HextechRuntimeRuneCompatibility"/> 将该符文标为本运行时不可用。</item>
/// <item><see cref="Optional"/> 允许类处理器未安装任何目标；安装异常仍计入失败，
/// 未关联符文时以诊断 Info 记录，关联符文时仍标记其不可用。</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class HextechPatchAttribute : Attribute
{
	public HextechPatchAttribute(string id, string feature)
	{
		Id = id;
		Feature = feature;
	}

	public string Id { get; }

	public string Feature { get; }

	public Type? Rune { get; init; }

	/// <summary>一个补丁同时服务多个符文时(如卡牌标签),失败时全部标记。</summary>
	public Type[]? Runes { get; init; }

	public bool Optional { get; init; }

	internal IEnumerable<Type> AffectedRunes
	{
		get
		{
			if (Rune != null)
			{
				yield return Rune;
			}

			foreach (Type rune in Runes ?? [])
			{
				yield return rune;
			}
		}
	}
}
