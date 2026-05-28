using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

public sealed class NearDeathFeastRune : HextechRelicBase
{
	private const int DeathNegativeMaxHpDivisor = 2;
	private int _nearDeathStrengthBonus;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedNearDeathStrengthBonus
	{
		get => _nearDeathStrengthBonus;
		set => _nearDeathStrengthBonus = Math.Max(0, value);
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DeathNegativeMaxHpPercent", 50m),
		new DynamicVar("StrengthPerNegativeHp", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override bool ShowCounter => Owner != null && !IsCanonical;

	public override int DisplayAmount => Owner != null ? GetDeathNegativeHpLimit(Owner.Creature) : 0;

	internal static bool HasDyingState(Creature creature)
	{
		return creature.Player?.GetRelic<NearDeathFeastRune>() != null;
	}

	internal static bool IsDyingButAlive(Creature creature)
	{
		return HasDyingState(creature)
			&& creature.CurrentHp <= 0
			&& creature.CurrentHp > -GetDeathNegativeHpLimit(creature);
	}

	internal static bool ShouldPreventSustain(Creature creature)
	{
		return IsDyingButAlive(creature);
	}

	internal static DamageResult LoseHpAllowingDying(Creature creature, decimal amount, ValueProp props)
	{
		if (amount <= 0m)
		{
			return new DamageResult(creature, props);
		}

		int oldHp = creature.CurrentHp;
		int hpLoss = (int)Math.Min(amount, 999999999m);
		int newHp = oldHp - hpLoss;
		int deathLimit = GetDeathNegativeHpLimit(creature);
		creature.SetCurrentHpInternal(newHp);

		bool killed = oldHp > -deathLimit && newHp <= -deathLimit;
		return new DamageResult(creature, props)
		{
			UnblockedDamage = oldHp - newHp,
			WasTargetKilled = killed,
			OverkillDamage = killed ? Math.Max(0, -deathLimit - newHp) : 0
		};
	}

	internal static void ForceDeathThresholdForKill(Creature creature)
	{
		int deathLimit = GetDeathNegativeHpLimit(creature);
		if (HasDyingState(creature) && creature.CurrentHp > -deathLimit)
		{
			creature.SetCurrentHpInternal(-deathLimit);
		}
	}

	internal static int GetDeathNegativeHpLimit(Creature creature)
	{
		return Math.Max(1, FloorToInt(creature.MaxHp / (decimal)DeathNegativeMaxHpDivisor));
	}

	internal void RefreshDeathLimitDisplay()
	{
		InvokeDisplayAmountChanged();
	}

	public override Task AfterObtained()
	{
		RefreshDeathLimitDisplay();
		return Task.CompletedTask;
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		RefreshDeathLimitDisplay();
		return Task.CompletedTask;
	}

	public override Task BeforeCombatStart()
	{
		ResetNearDeathState();
		RefreshDeathLimitDisplay();
		return Task.CompletedTask;
	}

	public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (Owner == null || creature != Owner.Creature || creature.CurrentHp > 0 || creature.CurrentHp <= -GetDeathNegativeHpLimit(creature))
		{
			return;
		}

		await SyncNearDeathStrength();
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner != null && Owner.Creature.CurrentHp < 1)
		{
			Flash(Array.Empty<Creature>());
			Owner.Creature.SetCurrentHpInternal(1);
		}

		ResetNearDeathState();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetNearDeathState();
		return Task.CompletedTask;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature && ShouldPreventSustain(target) ? 0m : 1m;
	}

	private async Task SyncNearDeathStrength()
	{
		if (Owner == null)
		{
			return;
		}

		int desiredBonus = Math.Max(0, -Owner.Creature.CurrentHp) * (int)DynamicVars["StrengthPerNegativeHp"].BaseValue;
		int delta = desiredBonus - _nearDeathStrengthBonus;
		if (delta <= 0)
		{
			_nearDeathStrengthBonus = desiredBonus;
			return;
		}

		_nearDeathStrengthBonus = desiredBonus;
		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, delta, Owner.Creature, null);
	}

	private void ResetNearDeathState()
	{
		_nearDeathStrengthBonus = 0;
	}
}
