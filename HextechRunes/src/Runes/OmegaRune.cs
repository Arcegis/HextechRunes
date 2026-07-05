using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class OmegaRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("StartTurn", 4m),
		new DynamicVar("Damage", 50m)
	];

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| side != Owner.Creature.Side
			|| Owner.Creature.CombatState == null
			|| Owner.Creature.CombatState.RoundNumber < DynamicVars["StartTurn"].IntValue)
		{
			return;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		// 表现:红色预警环+赤红审判光柱(纯表现层);逻辑等待让伤害与光柱落点对齐。
		HextechCombatVfx.OmegaJudgment(enemies);
		await Cmd.CustomScaledWait(0.42f, 0.55f);
		await HextechGameApiCompat.Damage(
			choiceContext,
			enemies,
			DynamicVars["Damage"].BaseValue,
			ValueProp.Unpowered,
			Owner.Creature,
			null);
	}
}
