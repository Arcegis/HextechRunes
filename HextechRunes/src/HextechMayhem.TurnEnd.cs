using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        CombatRoom? combatRoom = RunState.CurrentRoom as CombatRoom;

        HextechEnemyHexContext context = new(this);
        foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
        {
            await effect.BeforeTurnEnd(context, choiceContext, side, combatRoom);
        }

        if (side == CombatSide.Player)
        {
            _combatTracking.PreparePlayerSideTurnEnd();
            if (combatRoom != null)
            {
                RefreshPlayerAttackCostDoublingPreviews(GetAlivePlayerSideCreatures(combatRoom.CombatState));
            }
        }
    }
}
