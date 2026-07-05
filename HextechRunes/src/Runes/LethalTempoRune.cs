namespace HextechRunes;

public sealed class LethalTempoRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedCard(cardPlay.Card) || !HextechKnifeHelper.IsShivLike(cardPlay.Card, Owner) || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<HextechLethalTempoTemporaryStrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, cardPlay.Card);
	}
}
