using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HextechRunes;

public sealed class TrinityRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1),
		new StarsVar(1),
		new ForgeVar("ForgeAmount", 1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterEnergySpent(CardModel card, int amount)
	{
		if (Owner == null || card.Owner != Owner || Owner.Creature.IsDead || amount <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainStars(amount * DynamicVars.Stars.BaseValue, Owner);
	}

	public override Task AfterStarsSpent(int amount, Player spender)
	{
		if (spender != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return ForgeCmd.Forge(amount * DynamicVars["ForgeAmount"].BaseValue, Owner, this);
	}
}
