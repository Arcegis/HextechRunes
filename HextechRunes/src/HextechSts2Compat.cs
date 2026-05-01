using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal static class HextechSts2Compat
{
	public static bool IsPoweredAttack(ValueProp props)
	{
#if STS2_104_OR_NEWER
		return MegaCrit.Sts2.Core.ValueProps.ValuePropExtensions.IsPoweredAttack(props);
#else
		return props != ValueProp.Unpowered;
#endif
	}

	public static bool IsPartOfPlayerTurn(Player player)
	{
#if STS2_104_OR_NEWER
		return CombatManager.Instance.IsPartOfPlayerTurn(player);
#else
		CombatManager combatManager = CombatManager.Instance;
		return combatManager.IsInProgress
			&& combatManager.IsPlayPhase
			&& !combatManager.IsEnemyTurnStarted
			&& player.Creature.CombatState?.CurrentSide == player.Creature.Side;
#endif
	}
}
