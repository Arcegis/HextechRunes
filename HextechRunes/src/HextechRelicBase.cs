using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public abstract partial class HextechRelicBase : RelicModel
{
	private static readonly string PlaceholderIconPath = ImageHelper.GetImagePath("powers/missing_power.png");

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
