namespace HextechRunes;

/// <summary>
/// "改名敌方海克斯"独立身份化（2026-07-02）的平滑迁移：
/// 旧枚举值/旧枚举名只存在于本地持久化数据（Mayhem 存档的 int 序列、配置禁用列表的名字），
/// 读入时 remap 到新身份。联机 payload 不 remap——两端 mod 版本由 idDatabaseHash 强制一致，
/// 不存在跨版本 payload；Enum.IsDefined 防御保留即可。
/// </summary>
internal static class MonsterHexKindMigration
{
	private static readonly IReadOnlyDictionary<int, MonsterHexKind> RetiredValueToNewKind =
		new Dictionary<int, MonsterHexKind>
		{
			[18] = MonsterHexKind.Queen,               // 旧 Queen
			[47] = MonsterHexKind.SkulkingColony,      // 旧 ImmortalBone
			[64] = MonsterHexKind.LagavulinMatriarch,  // 旧 Misery
			[71] = MonsterHexKind.PhantasmalGardener,  // 旧 ScaredStiff
			[72] = MonsterHexKind.Exoskeleton,         // 旧 GhostForm
			[82] = MonsterHexKind.TestSubject,         // 旧 SymphonyOfWar
		};

	private static readonly IReadOnlyDictionary<string, string> RetiredNameToNewName =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["ImmortalBone"] = nameof(MonsterHexKind.SkulkingColony),
			["ScaredStiff"] = nameof(MonsterHexKind.PhantasmalGardener),
			["Misery"] = nameof(MonsterHexKind.LagavulinMatriarch),
			["GhostForm"] = nameof(MonsterHexKind.Exoskeleton),
			["SymphonyOfWar"] = nameof(MonsterHexKind.TestSubject),
			// 旧 "Queen" 名字与新枚举名相同，无需 remap。
		};

	/// <summary>持久化读入的原始 int：旧墓碑值映射为新值，其余原样返回。</summary>
	internal static int RemapRawValue(int rawHex)
	{
		return RetiredValueToNewKind.TryGetValue(rawHex, out MonsterHexKind newKind) ? (int)newKind : rawHex;
	}

	/// <summary>持久化读入的枚举名（如配置禁用列表）：旧名映射为新名，其余原样返回。</summary>
	internal static string RemapName(string name)
	{
		return RetiredNameToNewName.TryGetValue(name, out string? newName) ? newName : name;
	}
}
