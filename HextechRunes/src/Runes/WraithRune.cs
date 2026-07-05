using Godot;

namespace HextechRunes;

public sealed class WraithRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
		new DynamicVar("DamagePercentPerSoul", 3m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Soul>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		IEnumerable<Soul> souls = Soul.Create(Owner, DynamicVars.Cards.IntValue, combatState);
		await HextechCardGeneration.AddGeneratedCardsToCombat(souls, PileType.Hand, addedByPlayer: true);
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource))
		{
			return 1m;
		}

		int soulCount = Owner?.PlayerCombatState?.ExhaustPile.Cards.Count(static card => card is Soul) ?? 0;
		return 1m + soulCount * DynamicVars["DamagePercentPerSoul"].BaseValue / 100m;
	}
}
