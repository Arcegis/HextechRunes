using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
    private async Task ApplyCombatStartEnemyHexes(CombatRoom room)
    {
        await ApplyMonsterCombatStartHexes(room);
        await ApplyCombatStartPlayerDebuffHexes(room);
    }

    private async Task ApplyMonsterCombatStartHexes(CombatRoom room)
    {
        IReadOnlyList<Creature> enemies = GetAliveEnemies(room.CombatState);
        if (enemies.Count == 0)
        {
            return;
        }

        enemies = enemies
            .Where(static enemy => !ShouldDeferInitialBossStartHexes(enemy))
            .ToList();
        if (enemies.Count == 0)
        {
            return;
        }

        foreach (Creature enemy in enemies)
        {
            await ApplyMonsterCombatStartHexesToEnemy(enemy, room);
        }
    }

    private async Task ApplyMonsterCombatStartHexesToEnemy(
        Creature enemy,
        CombatRoom room)
    {
        HextechEnemyHexContext context = new(this);
        foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
        {
            await effect.ApplyCombatStartToEnemy(context, enemy, room);
        }
    }

    private async Task ApplyCombatStartPlayerDebuffHexes(CombatRoom room)
    {
        IReadOnlyList<Creature> players = GetAlivePlayerSideCreatures(room.CombatState);
        if (players.Count == 0)
        {
            return;
        }

        HextechEnemyHexContext context = new(this);
        foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
        {
            await effect.ApplyCombatStartPlayerDebuffs(context, room, players);
        }
    }
}
