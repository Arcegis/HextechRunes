namespace HextechRunes;

/// <summary>
/// 升级：播种（仅骨妹）——每次打出[gold]播种[/gold]后,这张[gold]播种[/gold]在本局游戏里的伤害永久 +1。
/// 逐张卡实例独立累加、持久存档,见 <see cref="HextechSelfUpgradeCardStore"/>。
/// </summary>
public sealed class SowUpgradeRune : SelfUpgradeOnPlayRuneBase<Sow>
{
	protected override int DamagePerPlay => 1;

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}
}
