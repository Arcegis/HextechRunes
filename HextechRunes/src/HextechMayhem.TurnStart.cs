using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
    private async Task BeforePlayerSideTurnStart(CombatState combatState, IReadOnlyList<Creature> players)
    {
        _bloodPactProcsThisTurn.Clear();
        _clownCollegeProcsThisTurn.Clear();

        await ApplyToCurrentEnemiesIfNeeded();
        await ResolvePlayerTurnPendingEnemyEffects(combatState);
        await ApplyPlayerTurnStartEnemyHexes(combatState, players);
    }

    private async Task ResolvePlayerTurnPendingEnemyEffects(CombatState combatState)
    {
        if (_escapePlanPending.Count > 0)
        {
            foreach (uint combatId in _escapePlanPending.ToList())
            {
                Creature? creature = combatState.GetCreature(combatId);
                _escapePlanPending.Remove(combatId);
                if (creature == null || !creature.IsAlive)
                {
                    continue;
                }

                int blockAmount = (int)Math.Floor(creature.MaxHp * 0.6m);
                if (blockAmount > 0)
                {
                    await CreatureCmd.GainBlock(creature, blockAmount, ValueProp.Unpowered, null);
                }

                await PowerCmd.Apply<ShrinkPower>(creature, 1m, creature, null);
            }
        }

        if (_speedDemonPending.Count > 0)
        {
            foreach (uint combatId in _speedDemonPending.ToList())
            {
                Creature? creature = combatState.GetCreature(combatId);
                _speedDemonPending.Remove(combatId);
                if (creature == null || !creature.IsAlive)
                {
                    continue;
                }

                int blockAmount = Math.Max(1, (int)Math.Floor(creature.MaxHp * 0.1m));
                await CreatureCmd.GainBlock(creature, blockAmount, ValueProp.Unpowered, null);
            }
        }

        if (_feyMagicPendingNoDrawPlayers.Count > 0)
        {
            foreach (KeyValuePair<uint, uint> pending in _feyMagicPendingNoDrawPlayers.ToList())
            {
                uint combatId = pending.Key;
                Creature? creature = combatState.GetCreature(combatId);
                Creature? source = combatState.GetCreature(pending.Value);
                _feyMagicPendingNoDrawPlayers.Remove(combatId);
                if (creature == null || !creature.IsAlive || creature.Side != CombatSide.Player)
                {
                    continue;
                }

                await PowerCmd.Apply<NoDrawPower>(creature, 1m, source, null);
            }
        }

        if (_repulsorPending.Count > 0)
        {
            foreach (uint combatId in _repulsorPending.ToList())
            {
                Creature? creature = combatState.GetCreature(combatId);
                _repulsorPending.Remove(combatId);
                if (creature == null || !creature.IsAlive)
                {
                    continue;
                }

                await PowerCmd.Apply<SlipperyPower>(creature, RepulsorSlipperyStacks, creature, null);
            }
        }
    }

    private async Task ApplyPlayerTurnStartEnemyHexes(CombatState combatState, IReadOnlyList<Creature> players)
    {
        if (HasActiveMonsterHex(MonsterHexKind.MountainSoul))
        {
            foreach (Creature enemy in GetAliveEnemies(combatState))
            {
                if (enemy.CombatId == null)
                {
                    continue;
                }

                uint combatId = enemy.CombatId.Value;
                if (_mountainSoulHasPreviousTurn.Contains(combatId)
                    && !_mountainSoulDamagedSinceLastTurn.Contains(combatId))
                {
                    int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.1m));
                    await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, null);
                }

                _mountainSoulHasPreviousTurn.Add(combatId);
                _mountainSoulDamagedSinceLastTurn.Remove(combatId);
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.Sonata)
            && combatState.RoundNumber % 2 == 1)
        {
            foreach (Creature enemy in GetAliveEnemies(combatState))
            {
                int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.1m));
                await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, null);
            }
        }

        IReadOnlyList<Creature> aliveEnemies = GetAliveEnemies(combatState);
        if (HasActiveMonsterHex(MonsterHexKind.DivineIntervention)
            && combatState.RoundNumber > 1
            && combatState.RoundNumber % 4 == 0
            && aliveEnemies.Count > 0)
        {
            await PowerCmd.Apply<IntangiblePower>(aliveEnemies, 1m, null, null);
        }

        if (HasActiveMonsterHex(MonsterHexKind.FrostWraith)
            && combatState.RoundNumber > 1
            && combatState.RoundNumber % 4 == 0
            && players.Count > 0)
        {
            await RunGroupedPlayerDebuffBurst(async () =>
            {
                await PowerCmd.Apply<BorrowedTimePower>(players, 1m, null, null);
            });
        }

        if (HasActiveMonsterHex(MonsterHexKind.SingularityAI) && players.Count > 0)
        {
            await AddEnemySingularityAIStatusCards(players);
        }
    }

    private async Task BeforeEnemySideTurnStart(CombatState combatState, IReadOnlyList<Creature> players)
    {
        _enemyProtectiveVeilTurnCounter++;
        _slapProcsThisTurn.Clear();
        _tormentorProcsThisTurn.Clear();
        _courageProcsThisTurn.Clear();
        _bloodPactProcsThisTurn.Clear();
        _clownCollegeProcsThisTurn.Clear();
        _devilsDanceTriggeredThisTurn.Clear();
        _monsterDebuffActionProcKeysThisTurn.Clear();

        IReadOnlyList<Creature> enemies = GetAliveEnemies(combatState);

        if (HasActiveMonsterHex(MonsterHexKind.TankEngine))
        {
            foreach (Creature enemy in enemies)
            {
                int hpGain = Math.Min(5, Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.05m)));
                await CreatureCmd.GainMaxHp(enemy, hpGain);
                if (enemy.CombatId != null)
                {
                    uint combatId = enemy.CombatId.Value;
                    _tankEngineStacks[combatId] = _tankEngineStacks.GetValueOrDefault(combatId, 0) + 1;
                    UpdateEnemyScale(enemy);
                }
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.ShrinkEngine))
        {
            foreach (Creature enemy in enemies)
            {
                if (enemy.GetPowerAmount<SlipperyPower>() <= 0m)
                {
                    await PowerCmd.Apply<SlipperyPower>(enemy, ShrinkEngineSlipperyStacks, enemy, null);
                }

                if (enemy.CombatId != null)
                {
                    uint combatId = enemy.CombatId.Value;
                    _shrinkEngineStacks[combatId] = _shrinkEngineStacks.GetValueOrDefault(combatId, 0) + 1;
                    UpdateEnemyScale(enemy);
                }
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.Sturdy))
        {
            foreach (Creature enemy in enemies)
            {
                decimal percent = enemy.CurrentHp * 2 < enemy.MaxHp ? 0.04m : 0.02m;
                int heal = Math.Min(10, Math.Max(1, (int)Math.Floor(enemy.MaxHp * percent)));
                if (heal > 0)
                {
                    await CreatureCmd.Heal(enemy, heal);
                }
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.UnmovableMountain))
        {
            foreach (Creature enemy in enemies)
            {
                if (enemy.Block <= 0)
                {
                    int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.08m));
                    await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, null);
                }
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.Sonata))
        {
            if (combatState.RoundNumber % 2 == 0)
            {
                foreach (Creature enemy in enemies)
                {
                    int heal = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.05m));
                    await CreatureCmd.Heal(enemy, heal);
                }
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.Doomsday) && players.Count > 0)
        {
            await RunGroupedPlayerDebuffBurst(async () =>
            {
                foreach (Creature player in players)
                {
                    decimal doom = Math.Floor(player.MaxHp * 0.05m);
                    if (doom > 0m)
                    {
                        await PowerCmd.Apply<DoomPower>(player, doom, null, null);
                    }
                }
            });
        }

        if (HasActiveMonsterHex(MonsterHexKind.ProtectiveVeil)
            && _enemyProtectiveVeilTurnCounter % 2 == 0)
        {
            foreach (Creature enemy in enemies)
            {
                await PowerCmd.Apply<ArtifactPower>(enemy, 1m, enemy, null);
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.HandOfBaron) && players.Count > 0)
        {
            await RunGroupedPlayerDebuffBurst(async () =>
            {
                await PowerCmd.Apply<ShrinkPower>(players, 2m, null, null);
            });
        }

        if (HasActiveMonsterHex(MonsterHexKind.Queen)
            && combatState.RoundNumber > 1
            && combatState.RoundNumber % 2 == 0)
        {
            IReadOnlyList<Creature> queenTargets = players
                .Where(player => player.GetPowerAmount<ChainsOfBindingPower>() <= 3m)
                .ToList();
            if (queenTargets.Count > 0)
            {
                await RunGroupedPlayerDebuffBurst(async () =>
                {
                    await PowerCmd.Apply<ChainsOfBindingPower>(queenTargets, 1m, null, null);
                });
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.FeelTheBurn) && _feelTheBurnPending.Count > 0 && players.Count > 0)
        {
            foreach (uint combatId in _feelTheBurnPending.ToList())
            {
                Creature? creature = combatState.GetCreature(combatId);
                _feelTheBurnPending.Remove(combatId);
                if (creature == null || !creature.IsAlive)
                {
                    continue;
                }

                await RunGroupedPlayerDebuffBurst(async () =>
                {
                    await PowerCmd.Apply<WeakPower>(players, 1m, creature, null);
                    await PowerCmd.Apply<VulnerablePower>(players, 1m, creature, null);
                    await PowerCmd.Apply<HextechBurnPower>(players, 5m, creature, null);
                });
            }
        }
    }
}
