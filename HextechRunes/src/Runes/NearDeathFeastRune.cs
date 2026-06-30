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
	private bool _nearDeathActive;
	private int _nearDeathDebt;
	private int _nearDeathStrengthBonus;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedNearDeathActive
	{
		get => _nearDeathActive;
		set => _nearDeathActive = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedNearDeathDebt
	{
		get => _nearDeathDebt;
		set => _nearDeathDebt = Math.Max(0, value);
	}

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
		NearDeathFeastRune? rune = GetRune(creature);
		return rune != null
			&& rune._nearDeathActive
			&& creature.CurrentHp > 0
			&& rune._nearDeathDebt < GetDeathNegativeHpLimit(creature);
	}

	/// <summary>
	/// 纯只读:供特效层轮询「濒死狂宴」是否激活及其强度(负血债务 / 死亡上限,0..1)。
	/// 不修改任何状态,仅反映已同步的运行状态,各端轮询结果一致。
	/// </summary>
	internal static bool TryGetFeastIntensity(Creature creature, out float intensity)
	{
		intensity = 0f;
		NearDeathFeastRune? rune = GetRune(creature);
		if (rune == null || !rune._nearDeathActive || creature.CurrentHp < 1)
		{
			return false;
		}

		int limit = GetDeathNegativeHpLimit(creature);
		intensity = limit > 0 ? Math.Clamp(rune._nearDeathDebt / (float)limit, 0f, 1f) : 0f;
		return true;
	}

	internal static bool ShouldPreventSustain(Creature creature)
	{
		return IsDyingButAlive(creature);
	}

	internal static bool ShouldInterceptLoseHp(Creature creature, decimal amount)
	{
		NearDeathFeastRune? rune = GetRune(creature);
		if (rune == null || amount <= 0m)
		{
			return false;
		}

		int hpLoss = (int)Math.Min(amount, 999999999m);
		return rune._nearDeathActive || creature.CurrentHp - hpLoss < 1;
	}

	internal static DamageResult LoseHpAllowingDying(Creature creature, decimal amount, ValueProp props)
	{
		NearDeathFeastRune? rune = GetRune(creature);
		if (rune == null)
		{
			return CreateDamageResult(creature, props, 0, false, 0);
		}

		if (amount <= 0m)
		{
			return CreateDamageResult(creature, props, 0, false, 0);
		}

		int oldEffectiveHp = rune._nearDeathActive ? -rune._nearDeathDebt : creature.CurrentHp;
		int hpLoss = (int)Math.Min(amount, 999999999m);
		int newEffectiveHp = oldEffectiveHp - hpLoss;
		int deathLimit = GetDeathNegativeHpLimit(creature);
		bool killed = newEffectiveHp <= -deathLimit;

		if (killed)
		{
			rune._nearDeathActive = false;
			rune._nearDeathDebt = deathLimit;
			creature.SetCurrentHpInternal(0);
			return CreateDamageResult(creature, props, hpLoss, true, Math.Max(0, -deathLimit - newEffectiveHp));
		}

		rune._nearDeathActive = newEffectiveHp < 1;
		rune._nearDeathDebt = Math.Max(0, -newEffectiveHp);
		int safeHp = rune._nearDeathActive ? 1 : newEffectiveHp;
		bool hpChanged = creature.CurrentHp != safeHp;
		creature.SetCurrentHpInternal(safeHp);
		if (!hpChanged)
		{
			_ = rune.SyncNearDeathStrength();
		}

		return CreateDamageResult(creature, props, hpLoss, false, 0);
	}

	internal static void ForceDeathThresholdForKill(Creature creature)
	{
		NearDeathFeastRune? rune = GetRune(creature);
		if (rune != null)
		{
			rune._nearDeathActive = false;
			rune._nearDeathDebt = GetDeathNegativeHpLimit(creature);
			creature.SetCurrentHpInternal(0);
		}
	}

	internal static void PreserveNegativeHpAsDyingState(Creature creature, int requestedHp)
	{
		NearDeathFeastRune? rune = GetRune(creature);
		if (rune == null)
		{
			creature.SetCurrentHpInternal(Math.Max(0, requestedHp));
			return;
		}

		int deathLimit = GetDeathNegativeHpLimit(creature);
		int debt = Math.Max(0, -requestedHp);
		if (debt >= deathLimit)
		{
			rune._nearDeathActive = false;
			rune._nearDeathDebt = deathLimit;
			creature.SetCurrentHpInternal(0);
			return;
		}

		rune._nearDeathActive = true;
		rune._nearDeathDebt = debt;
		creature.SetCurrentHpInternal(1);
		_ = rune.SyncNearDeathStrength();
	}

	internal static int GetDeathNegativeHpLimit(Creature creature)
	{
		return Math.Max(1, FloorToInt(creature.MaxHp / (decimal)DeathNegativeMaxHpDivisor));
	}

	internal static bool TryGetDisplayedHp(Creature creature, out int displayedHp)
	{
		NearDeathFeastRune? rune = GetRune(creature);
		if (rune != null && rune._nearDeathActive)
		{
			displayedHp = -rune._nearDeathDebt;
			return true;
		}

		displayedHp = 0;
		return false;
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
		if (Owner == null || creature != Owner.Creature || !_nearDeathActive)
		{
			return;
		}

		await SyncNearDeathStrength();
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner != null && (_nearDeathActive || Owner.Creature.CurrentHp < 1))
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

		int desiredBonus = _nearDeathActive
			? _nearDeathDebt * (int)DynamicVars["StrengthPerNegativeHp"].BaseValue
			: 0;
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
		_nearDeathActive = false;
		_nearDeathDebt = 0;
		_nearDeathStrengthBonus = 0;
	}

	private static NearDeathFeastRune? GetRune(Creature creature)
	{
		return creature.Player?.GetRelic<NearDeathFeastRune>();
	}

	private static DamageResult CreateDamageResult(Creature creature, ValueProp props, int unblockedDamage, bool wasTargetKilled, int overkillDamage)
	{
		DamageResult result = new(creature, props);
		object boxed = result;
		SetDamageResultValue(boxed, nameof(DamageResult.UnblockedDamage), unblockedDamage);
		SetDamageResultValue(boxed, nameof(DamageResult.WasTargetKilled), wasTargetKilled);
		SetDamageResultValue(boxed, nameof(DamageResult.OverkillDamage), overkillDamage);
		return (DamageResult)boxed;
	}

	private static void SetDamageResultValue(object result, string memberName, object value)
	{
		Type type = result.GetType();
		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		PropertyInfo? property = type.GetProperty(memberName, flags);
		if (property?.SetMethod != null)
		{
			property.SetValue(result, ConvertDamageResultValue(value, property.PropertyType));
			return;
		}

		FieldInfo? field = type.GetField($"<{memberName}>k__BackingField", flags)
			?? type.GetField(memberName, flags)
			?? type.GetField($"_{char.ToLowerInvariant(memberName[0])}{memberName[1..]}", flags);
		field?.SetValue(result, ConvertDamageResultValue(value, field.FieldType));
	}

	private static object ConvertDamageResultValue(object value, Type targetType)
	{
		Type actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
		return Convert.ChangeType(value, actualType);
	}
}
