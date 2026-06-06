using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer?.Side != CombatSide.Enemy || dealer.CombatState?.RunState != RunState)
        {
            return 1m;
        }

        return HextechEnemyHexDispatcher.Transform(
            this,
            1m,
            (effect, context, multiplier) => multiplier * effect.ModifyDamageMultiplicative(context, target, amount, props, dealer, cardSource));
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target.Side != CombatSide.Enemy || target.CombatState?.RunState != RunState)
        {
            return 1m;
        }

        return HextechEnemyHexDispatcher.Transform(
            this,
            1m,
            (effect, context, multiplier) => multiplier * effect.ModifyBlockMultiplicative(context, target, block, props, cardSource, cardPlay));
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return HextechEnemyHexDispatcher.Transform(
            this,
            count,
            (effect, context, current) => effect.ModifyHandDraw(context, player, current));
    }

    public override bool ShouldFlush(Player player)
    {
        return HextechEnemyHexDispatcher.All(
            this,
            (effect, context) => effect.ShouldFlush(context, player));
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature.Side != CombatSide.Player
            || !IllusoryWeaponRune.IsAttackForEffects(card, card.Owner)
            || card.Pile?.Type != PileType.Hand
            || card.EnergyCost.CostsX
            || originalCost <= 0m
            || card.Owner.Creature.CombatState?.RunState != RunState)
        {
            return false;
        }

        decimal multiplier = HextechEnemyHexDispatcher.Transform(
            this,
            1m,
            (effect, context, current) => current * effect.ModifyPlayerAttackEnergyCostMultiplier(context, card, originalCost));

        if (multiplier == 1m)
        {
            return false;
        }

        modifiedCost = originalCost * multiplier;
        return true;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
    {
        (pileType, position) = HextechEnemyHexDispatcher.Transform(
            this,
            (pileType, position),
            (effect, context, current) =>
            {
                if (effect.ModifyCardPlayResultPileTypeAndPosition(context, card, isAutoPlay, resources, current.pileType, current.position) is (PileType nextPileType, CardPilePosition nextPosition))
                {
                    return (nextPileType, nextPosition);
                }

                return current;
            });

        return (pileType, position);
    }
}
