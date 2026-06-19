using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class ExposeUpgradeRune : CardUpgradeRuneBase<Expose>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Expose
			|| cardPlay.Target == null
			|| cardPlay.Target.Side == Owner.Creature.Side)
		{
			return;
		}

		List<PowerModel> buffs = cardPlay.Target.Powers
			.Where(static power => power.GetTypeForAmount(power.Amount) == PowerType.Buff
				&& !HextechMonsterInteractionPolicy.IsStructuralMonsterBuff(power))
			.ToList();
		if (buffs.Count == 0)
		{
			return;
		}

		Flash([cardPlay.Target]);
		foreach (PowerModel power in buffs)
		{
			await PowerCmd.Remove(power);
		}
	}
}
