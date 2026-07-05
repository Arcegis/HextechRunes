namespace HextechRunes;

/// <summary>
/// 升级：收割（仅骨妹）——每次打出[gold]收割[/gold]后,这张[gold]收割[/gold]在本局游戏里的伤害永久 +3。
/// 逐张卡实例独立累加、持久存档,见 <see cref="HextechSelfUpgradeCardStore"/>。
/// </summary>
public sealed class ReapUpgradeRune : SelfUpgradeOnPlayRuneBase<Reap>
{
	protected override int DamagePerPlay => 3;

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}
}
