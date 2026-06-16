using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class NineDragonPowerRune : HextechRelicBase
{
	private int _stacks;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedStacks
	{
		get => _stacks;
		set
		{
			_stacks = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => true;

	public override int DisplayAmount => !IsCanonical ? _stacks : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RegenPower>(1m),
		new DynamicVar("StackBonusPercent", 3m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RegenPower>()
	];

	public decimal SustainMultiplier => 1m + _stacks * DynamicVars["StackBonusPercent"].BaseValue / 100m;

	internal float BodyScaleDelta => _stacks * (float)(DynamicVars["StackBonusPercent"].BaseValue / 100m);

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		Grow();
		return Task.CompletedTask;
	}

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || _stacks <= 0)
		{
			return Task.CompletedTask;
		}

		return PowerCmd.Apply<RegenPower>(Owner.Creature, _stacks * DynamicVars["RegenPower"].BaseValue, Owner.Creature, null);
	}

	public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
	{
		if (Owner == null || Owner.Creature.IsDead || !IsPotionUseOwnedByOrTargetingOwner(potion, target))
		{
			return;
		}

		SavedStacks++;
		Flash();
		int hpGain = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["StackBonusPercent"].BaseValue / 100m));
		await CreatureCmd.GainMaxHp(Owner.Creature, hpGain);
		Grow();
	}

	private void Grow()
	{
		HextechPlayerBodyScaleHelper.Update(Owner);
	}
}
