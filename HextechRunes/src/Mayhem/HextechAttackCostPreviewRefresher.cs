using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class HextechAttackCostPreviewRefresher
{
	public static void Refresh(
		HextechMayhemModifier modifier,
		RunState runState,
		IEnumerable<Creature> playerCreatures)
	{
		if (!HextechEnemyHexEffects.HasActiveAttackCostPreviewEffect(modifier))
		{
			return;
		}

		foreach (Creature playerCreature in playerCreatures)
		{
			Player? player = playerCreature.Player;
			if (player == null
				|| playerCreature.CombatState?.RunState != runState)
			{
				continue;
			}

			foreach (CardModel card in PileType.Hand.GetPile(player).Cards)
			{
				if (IllusoryWeaponRune.IsAttackForEffects(card, player) && !card.EnergyCost.CostsX)
				{
					card.InvokeEnergyCostChanged();
				}
			}
		}
	}
}
