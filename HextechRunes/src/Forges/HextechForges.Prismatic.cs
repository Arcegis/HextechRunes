using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class PrismaticLifeForge : HextechForgeBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MaxHpPercent", 30m)
	];

	public override Task AfterObtained()
	{
		if (Owner == null)
		{
			return Task.CompletedTask;
		}

		int maxHpGain = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["MaxHpPercent"].BaseValue / 100m));
		Flash();
		return CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);
	}
}

public sealed class AttackForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamageMultiplier", 1.2m)
	];

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? StackedMultiplier(DynamicVars["DamageMultiplier"].BaseValue) : 1m;
	}
}

public sealed class ProtectionForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("SustainMultiplier", 1.2m)
	];

	public decimal SustainMultiplier => StackedMultiplier(DynamicVars["SustainMultiplier"].BaseValue);

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}
}

public sealed class EnergyForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	public override Task AfterEnergyResetLate(Player player)
	{
		if (Owner == null || player != Owner || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainEnergy(Stacked(DynamicVars.Energy.BaseValue), Owner);
	}
}

public sealed class RitualForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RitualPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RitualPower>()
	];

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<RitualPower>(Owner.Creature, Stacked(DynamicVars["RitualPower"].BaseValue), Owner.Creature, null);
	}
}

public sealed class RegenForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RegenPower>(4m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RegenPower>()
	];

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<RegenPower>(Owner.Creature, Stacked(DynamicVars["RegenPower"].BaseValue), Owner.Creature, null);
	}
}

public sealed class BufferForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BufferPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<BufferPower>()
	];

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<BufferPower>(Owner.Creature, Stacked(DynamicVars["BufferPower"].BaseValue), Owner.Creature, null);
	}
}

public sealed class SlipperyForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<SlipperyPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<SlipperyPower>()
	];

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<SlipperyPower>(Owner.Creature, Stacked(DynamicVars["SlipperyPower"].BaseValue), Owner.Creature, null);
	}
}

public sealed class PrismaticArtifactForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ArtifactPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ArtifactPower>()
	];

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<ArtifactPower>(Owner.Creature, Stacked(DynamicVars["ArtifactPower"].BaseValue), Owner.Creature, null);
	}
}

public sealed class FortuneForge : HextechForgeBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new GoldVar(1000)
	];

	public override Task AfterObtained()
	{
		if (Owner == null)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
	}
}

public sealed class VoidForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VoidFormPower>(1m)
	];

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<VoidFormPower>(Owner.Creature, Stacked(DynamicVars["VoidFormPower"].BaseValue), Owner.Creature, null);
	}
}
