namespace HextechRunes;

public sealed class MasterOfDualityRune : HextechRelicBase
{
	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || cardPlay.Card.Owner != Owner)
		{
			return;
		}

		if (IllusoryWeaponRune.IsSkillForEffects(cardPlay.Card))
		{
			Flash();
			await PowerCmd.Apply<HextechTemporaryStrengthPower>(Owner.Creature, 1m, Owner.Creature, null);
		}
		if (IsOwnedAttack(cardPlay.Card))
		{
			Flash();
			await PowerCmd.Apply<HextechTemporaryDexterityPower>(Owner.Creature, 1m, Owner.Creature, null);
		}
	}
}
