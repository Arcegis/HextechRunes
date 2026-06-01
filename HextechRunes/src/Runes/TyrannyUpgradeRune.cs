using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class TyrannyUpgradeRune : CardUpgradeRuneBase<Tyranny>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int count = Math.Max(0, Owner.Creature.GetPowerAmount<TyrannyPower>());
		if (count <= 0)
		{
			return;
		}

		Flash();
		await AddCardCopiesToCombatHand<Debris>(
			count,
			static card => card.AddKeyword(CardKeyword.Ethereal));
	}
}
