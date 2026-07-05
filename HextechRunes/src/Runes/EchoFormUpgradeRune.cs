namespace HextechRunes;

// 0.8.4 重做:回合结束时,手牌内有EchoForm则自动打出;获得时补卡(基类)。
public sealed class EchoFormUpgradeRune : AutoPlayFormsAtCombatStartRuneBase<EchoForm>
{
	protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);
}
