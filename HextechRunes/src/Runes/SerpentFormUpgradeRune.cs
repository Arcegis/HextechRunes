namespace HextechRunes;

public sealed class SerpentFormUpgradeRune : AutoPlayFormsAtCombatStartRuneBase<SerpentForm>
{
	protected override bool IsAvailableForCharacter(Player player) => IsSilentPlayer(player);
}
