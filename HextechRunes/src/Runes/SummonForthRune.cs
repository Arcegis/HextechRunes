namespace HextechRunes;

public sealed class SummonForthRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ForgeVar("ForgeAmount", 5)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<SovereignBlade>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || !IsRegentOwner)
		{
			return;
		}

		Flash();
		await ForgeCmd.Forge(DynamicVars["ForgeAmount"].BaseValue, Owner, this);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner?.PlayerCombatState == null || Owner.Creature.IsDead)
		{
			return;
		}

		// 排除 Play 堆:正在打出结算中的剑不能中途拽进手牌(OnPlayWrapper 的收尾还认为它在
		// Play 堆,被拽走后结算与手牌 UI 各持一份状态,是"空白手牌位"的成因之一)。
		IReadOnlyList<SovereignBlade> blades = Owner.PlayerCombatState.AllCards
			.OfType<SovereignBlade>()
			.Where(static card => card.Pile?.Type is not PileType.Hand and not PileType.Play)
			.ToList();
		if (blades.Count == 0)
		{
			return;
		}

		Flash();
		foreach (SovereignBlade blade in blades)
		{
			await CardPileCmd.Add(blade, PileType.Hand);
		}
	}
}
