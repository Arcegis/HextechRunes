using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class DemonFormUpgradeRune : CardUpgradeRuneBase<DemonForm>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead)
		{
			return;
		}

		decimal heal = Owner.Creature.GetPowerAmount<DemonFormPower>();
		if (heal <= 0m)
		{
			return;
		}

		Flash();
		await CreatureCmd.Heal(Owner.Creature, heal);
	}
}
