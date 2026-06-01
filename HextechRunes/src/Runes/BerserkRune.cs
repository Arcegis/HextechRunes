using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class BerserkRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VulnerablePower>(3m),
		new EnergyVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<VulnerablePower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		VulnerablePower? vulnerable = await PowerCmd.Apply<VulnerablePower>(Owner.Creature, DynamicVars.Vulnerable.BaseValue, Owner.Creature, null);
		if (vulnerable != null)
		{
			vulnerable.SkipNextDurationTick = false;
		}
	}

	public override Task AfterEnergyResetLate(Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
	}
}
