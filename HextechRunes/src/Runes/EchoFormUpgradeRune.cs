namespace HextechRunes;

public sealed class EchoFormUpgradeRune : AutoPlayFormsAtCombatStartRuneBase<EchoForm>
{
	protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);
}
