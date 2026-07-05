namespace HextechRunes;

public sealed class UnleashUpgradeRune : CardUpgradeRuneBase<Unleash>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Unleash>(),
		HoverTipFactory.FromCard<Protector>()
	];

	protected override bool DeckContainsRequiredCard(Player player)
	{
		return DeckContains<Unleash>(player) || DeckContains<Protector>(player);
	}

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| target.Side != CombatSide.Enemy
			|| result.UnblockedDamage <= 0m
			|| cardSource?.Owner != Owner
			|| cardSource is not (Unleash or Protector)
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		Flash([target]);
		await OstyCmd.Summon(choiceContext, Owner, result.UnblockedDamage, this);
	}
}
