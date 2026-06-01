using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class SerpentFormUpgradeRune : CardUpgradeRuneBase<SerpentForm>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (Owner == null || card.Owner != Owner || Owner.Creature.IsDead || Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		int damage = Owner.Creature.GetPowerAmount<SerpentFormPower>();
		if (damage <= 0)
		{
			return;
		}

		Creature? target = HextechRuneTargeting.PickRandomHittableEnemy(
			Owner,
			combatState,
			"serpent-form-upgrade",
			combatState.RoundNumber.ToString(),
			CombatManager.Instance.History.Entries.Count().ToString(),
			card.Id.Entry);
		if (target == null)
		{
			return;
		}

		Flash([target]);
		await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Unpowered, Owner.Creature, null);
	}
}
