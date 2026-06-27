using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

/// <summary>
/// 升级：铁斩波（仅战士）——每次打出[gold]铁斩波[/gold]后,这张[gold]铁斩波[/gold]在本局游戏里的伤害和格挡各永久 +1。
/// 逐张卡实例独立累加、持久存档,见 <see cref="HextechSelfUpgradeCardStore"/>。
/// </summary>
public sealed class IronWaveUpgradeRune : SelfUpgradeOnPlayRuneBase<IronWave>
{
	protected override int DamagePerPlay => 1;

	protected override int BlockPerPlay => 1;

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsIroncladPlayer(player);
	}
}
