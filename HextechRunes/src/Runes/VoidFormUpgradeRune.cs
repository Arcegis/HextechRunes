using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class VoidFormUpgradeRune : CardUpgradeRuneBase<VoidForm>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsRegentPlayer(player);
	}

	public static bool ShouldUseUpgradedPlay(VoidForm card)
	{
		return card.Owner.GetRelic<VoidFormUpgradeRune>() != null;
	}

	public static async Task PlayUpgraded(PlayerChoiceContext choiceContext, VoidForm card, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<VoidFormPower>(
			card.Owner.Creature,
			card.DynamicVars["VoidFormPower"].BaseValue,
			card.Owner.Creature,
			card);
		card.Owner.GetRelic<VoidFormUpgradeRune>()?.Flash();
	}
}
