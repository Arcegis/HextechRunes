namespace HextechRunes;

public sealed class DemonFormUpgradeRune : AutoPlayFormsAtCombatStartRuneBase<DemonForm>
{
	protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);
}
