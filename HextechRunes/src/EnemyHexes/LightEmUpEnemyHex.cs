namespace HextechRunes;

internal sealed class LightEmUpEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.LightEmUp;

	internal override bool AffectsPlayerAttackCostPreview => true;

	internal override decimal ModifyPlayerAttackEnergyCostMultiplier(HextechEnemyHexContext context, CardModel card, decimal originalCost)
	{
		int nextAttackIndex = context.Modifier.GetPlayerAttacksPlayedThisTurn(card) + 1;
		return nextAttackIndex % 4 == 0 ? 2m : 1m;
	}
}
