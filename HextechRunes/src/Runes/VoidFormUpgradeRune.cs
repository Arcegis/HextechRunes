namespace HextechRunes;

public sealed class VoidFormUpgradeRune : AutoPlayFormsAtCombatStartRuneBase<VoidForm>
{
	protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);
}
