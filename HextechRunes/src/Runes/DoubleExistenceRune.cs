using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

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
			|| Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<EchoFormPower>(Owner.Creature, 1m, Owner.Creature, null);
	}
}
