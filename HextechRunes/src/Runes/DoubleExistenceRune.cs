using MegaCrit.Sts2.Core.Combat;
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

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null
			|| side != Owner.Creature.Side
			|| Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<EchoFormPower>(Owner.Creature, 1m, Owner.Creature, null);
	}
}
