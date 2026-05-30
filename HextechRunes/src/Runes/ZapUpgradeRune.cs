using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace HextechRunes;

public sealed class ZapUpgradeRune : CardUpgradeRuneBase<Zap>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Zap
			|| Owner.PlayerCombatState == null
			|| Owner.Creature.IsDead)
		{
			return;
		}

		List<LightningOrb> lightningOrbs = Owner.PlayerCombatState.OrbQueue.Orbs
			.OfType<LightningOrb>()
			.ToList();
		if (lightningOrbs.Count == 0)
		{
			return;
		}

		Flash();
		foreach (LightningOrb orb in lightningOrbs)
		{
			await orb.Passive(context, null);
		}
	}
}
