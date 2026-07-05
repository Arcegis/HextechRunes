using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

public sealed class PorcupineRune : HextechRelicBase
{
	// 临时荆棘:回合结束按格挡获得,覆盖敌方回合的反伤,下个我方回合开始时收回。
	private decimal _temporaryThorns;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("BlockPerThorn", 5m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ThornsPower>()
	];

	public override Task BeforeCombatStart()
	{
		_temporaryThorns = 0m;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_temporaryThorns = 0m;
		return Task.CompletedTask;
	}

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead)
		{
			return;
		}

		decimal thorns = Math.Floor(Owner.Creature.Block / DynamicVars["BlockPerThorn"].BaseValue);
		if (thorns <= 0m)
		{
			return;
		}

		_temporaryThorns += thorns;
		Flash();
		await PowerCmd.Apply<ThornsPower>(Owner.Creature, thorns, Owner.Creature, null);
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side || _temporaryThorns <= 0m)
		{
			return;
		}

		decimal remove = _temporaryThorns;
		_temporaryThorns = 0m;
		if (Owner.Creature.IsDead)
		{
			return;
		}

		await PowerCmd.Apply<ThornsPower>(Owner.Creature, -remove, Owner.Creature, null);
	}
}
