using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public abstract partial class HextechRelicBase : RelicModel
{
	private static readonly string PlaceholderIconPath = ImageHelper.GetImagePath("powers/missing_power.png");

	// 0.108.0 起伤害管线加 CardPlay 参数:游戏侧 override 按版本二选一,子类统一重写版本无关的
	// Compat 虚方法(签名沿用 0.107,cardPlay 目前无子类需要,需要时再扩)。
	public virtual decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1m;
	}

#if STS2_108_OR_NEWER
	public sealed override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		return ModifyDamageMultiplicativeCompat(target, amount, props, dealer, cardSource);
	}
#else
	public sealed override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return ModifyDamageMultiplicativeCompat(target, amount, props, dealer, cardSource);
	}
#endif

#if STS2_106_OR_NEWER
	public virtual Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		return Task.CompletedTask;
	}

	public sealed override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, HextechCombatState combatState)
	{
		return BeforeSideTurnStart(choiceContext, side, combatState);
	}

	public virtual Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		return Task.CompletedTask;
	}

	public sealed override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, HextechCombatState combatState)
	{
		return AfterSideTurnStart(side, combatState);
	}

	public virtual Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return Task.CompletedTask;
	}

	public sealed override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		return BeforeTurnEnd(choiceContext, side);
	}

	public virtual Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return Task.CompletedTask;
	}

	public sealed override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		return AfterTurnEnd(choiceContext, side);
	}
#endif

	public sealed override RelicRarity Rarity => RelicRarity.Starter;

	public override string PackedIconPath => GetResolvedIconPath();

	protected override string PackedIconOutlinePath => GetResolvedIconPath();

	protected override string BigIconPath => GetResolvedIconPath();

	public virtual bool IsAvailableForPlayer(Player player) => true;

	private string GetResolvedIconPath()
	{
		string? customPath = HextechAssets.TryGetCustomRelicIconPath(this);
		if (!string.IsNullOrEmpty(customPath) && ResourceLoader.Exists(customPath))
		{
			return customPath;
		}

		return PlaceholderIconPath;
	}
}
