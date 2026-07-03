using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class DullBladeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpLoss", 1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
#else
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
#endif
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CurrentHp <= 1)
		{
			return;
		}

		int currentHp = Owner.Creature.CurrentHp;
		Flash([Owner.Creature]);
		await HextechGameApiCompat.Damage(
			choiceContext,
			Owner.Creature,
			DynamicVars["HpLoss"].BaseValue,
			ValueProp.Unblockable | ValueProp.Unpowered,
			Owner.Creature,
			null);

		if (!Owner.Creature.IsDead && Owner.Creature.CurrentHp < currentHp)
		{
			await CreatureCmd.SetCurrentHp(Owner.Creature, currentHp);
		}
	}
}
