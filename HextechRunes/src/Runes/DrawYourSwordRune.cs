namespace HextechRunes;

public sealed class DrawYourSwordRune : AttributeConversionRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FocusPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Evoke),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	internal bool ShouldReplaceOrbEvoke(OrbModel orb)
	{
		return !Owner.Creature.IsDead
			&& IsDefectOwner
			&& ReferenceEquals(orb.Owner, Owner)
			&& ReferenceEquals(Owner.GetRelic<DrawYourSwordRune>(), this);
	}

	internal async Task<IEnumerable<Creature>> ReplaceOrbEvoke()
	{
		Flash();
		await PowerCmd.Apply<FocusPower>(
			Owner.Creature,
			DynamicVars["FocusPower"].BaseValue,
			Owner.Creature,
			null);
		return Array.Empty<Creature>();
	}

	protected override bool ShouldConvert(PowerModel power)
	{
		return IsDefectOwner && !HasConflictingFocusConverter && power is FocusPower;
	}

	protected override async Task ApplyConvertedPower(Creature owner, decimal amount, Creature? applier, CardModel? cardSource)
	{
		await PowerCmd.Apply<StrengthPower>(owner, amount, applier, cardSource);
		await PowerCmd.Apply<DexterityPower>(owner, amount, applier, cardSource);
	}

	protected override Task RevertOriginalPower(Creature owner, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<FocusPower>(owner, -amount, applier, cardSource);
	}

	private bool HasConflictingFocusConverter => Owner.GetRelic<DexterityStrengthToFocusRune>() != null;

	[HextechPatch("rune.draw-your-sword.evoke", "亮出你的剑", Rune = typeof(DrawYourSwordRune))]
	private static class DrawYourSwordEvokePatch
	{
		// 跳过型前缀按设计哲学用 Priority.Low：别的模组的前缀先跑；若它们已跳过原方法，本前缀（返回 bool、
		// 不读 __runOriginal）会被 Harmony 一并跳过，把替换让给对方。本模组没有别的 Evoke 前缀，Low 不会让亮剑自己失效。
		// FindLoadedOrbEvokeMethods 只返回原版程序集里的 Evoke，安装失败由 HextechPatcher 按补丁归因报告。
		private static void Apply(Harmony harmony)
		{
			HarmonyMethod prefix = new(typeof(HextechPlayerRuneHooks), nameof(HextechPlayerRuneHooks.OrbEvokePrefix))
			{
				priority = Priority.Low
			};

			foreach (MethodInfo method in HextechPlayerRuneHooks.FindOrbEvokeMethods())
			{
				harmony.Patch(method, prefix: prefix);
			}
		}
	}
}
