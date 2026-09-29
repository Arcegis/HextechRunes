namespace HextechRunes;

public sealed class ReaperFormUpgradeRune : AutoPlayFormsAtCombatStartRuneBase<ReaperForm>
{
	protected override bool IsAvailableForCharacter(Player player) => IsNecrobinderPlayer(player);
}
