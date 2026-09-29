namespace HextechRunes;

public sealed class MasterOfDualityRune : HextechRelicBase
{
	private const decimal TemporaryStatGain = 1m;

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || cardPlay.Card.Owner != Owner)
		{
			return;
		}

		if (IllusoryWeaponRune.IsSkillForEffects(cardPlay.Card))
		{
			Flash();
			await PowerCmd.Apply<HextechTemporaryStrengthPower>(Owner.Creature, TemporaryStatGain, Owner.Creature, null);
		}

		if (IsOwnedAttack(cardPlay.Card))
		{
			Flash();
			await PowerCmd.Apply<HextechTemporaryDexterityPower>(Owner.Creature, TemporaryStatGain, Owner.Creature, null);
		}
	}
}
