using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace HextechRunes;

public sealed class DoubleExistenceRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		Flash();
		await CardPileCmd.Draw(choiceContext, combatState.RoundNumber, Owner, fromHandDraw: false);
	}

	public override Task AfterEnergyResetLate(Player player)
	{
		if (player != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return Task.CompletedTask;
		}

		return PlayerCmd.GainEnergy(combatState.RoundNumber, player);
	}
}
