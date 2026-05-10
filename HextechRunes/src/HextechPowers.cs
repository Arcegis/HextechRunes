using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class HextechBurnPower : PowerModel
{
	private static int _damageResolveDepth;

	internal static bool IsResolvingDamage => _damageResolveDepth > 0;

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != CombatSide.Player || Amount <= 0 || !Owner.IsAlive)
		{
			return;
		}

		try
		{
			_damageResolveDepth++;
			await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, Applier, null);
		}
		finally
		{
			_damageResolveDepth--;
		}
	}
}

public sealed class HextechTemporaryStrengthPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => ModelDb.Relic<MasterOfDualityRune>();

	protected override bool IsVisibleInternal => false;
}

public sealed class HextechTemporaryDexterityPower : TemporaryDexterityPower
{
	public override AbstractModel OriginModel => ModelDb.Relic<MasterOfDualityRune>();

	protected override bool IsVisibleInternal => false;
}

public sealed class HextechTemporaryStrengthLossPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => ModelDb.Relic<MasterOfDualityRune>();

	protected override bool IsVisibleInternal => false;

	protected override bool IsPositive => false;
}

public sealed class HextechTemporaryDexterityLossPower : TemporaryDexterityPower
{
	public override AbstractModel OriginModel => ModelDb.Relic<MasterOfDualityRune>();

	protected override bool IsVisibleInternal => false;

	protected override bool IsPositive => false;
}

public sealed class HextechLethalTempoTemporaryStrengthPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => ModelDb.Relic<LethalTempoRune>();

	protected override bool IsVisibleInternal => false;
}

public sealed class HextechBloodPactTemporaryStrengthPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => ModelDb.Relic<BloodPactRune>();

	protected override bool IsVisibleInternal => false;
}

public sealed class HextechAttackReplayPower : PowerModel
{
	private bool _triggeredLastPlay;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		_triggeredLastPlay = false;
		if (Amount <= 0m
			|| card.Owner?.Creature != Owner
			|| card.Type != CardType.Attack)
		{
			return playCount;
		}

		_triggeredLastPlay = true;
		return playCount + Amount;
	}

	public override async Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (!_triggeredLastPlay || card.Owner?.Creature != Owner || card.Type != CardType.Attack)
		{
			return;
		}

		_triggeredLastPlay = false;
		Flash();
		await PowerCmd.Remove(this);
	}
}

public sealed class HextechTemporarySlowPower : PowerModel, ITemporaryPower
{
	private bool _shouldIgnoreNextInstance;

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override bool IsVisibleInternal => false;

	public AbstractModel OriginModel => ModelDb.Relic<FrostWraithRune>();

	public PowerModel InternallyAppliedPower => ModelDb.Power<SlowPower>();

	public override LocString Title => ModelDb.Power<SlowPower>().Title;

	public override LocString Description => ModelDb.Power<SlowPower>().Description;

	public void IgnoreNextInstance()
	{
		_shouldIgnoreNextInstance = true;
	}

	public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (_shouldIgnoreNextInstance)
		{
			_shouldIgnoreNextInstance = false;
			return;
		}

		await PowerCmd.Apply<SlowPower>(target, amount, applier, cardSource, silent: true);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (power != this || amount == Amount)
		{
			return;
		}

		if (_shouldIgnoreNextInstance)
		{
			_shouldIgnoreNextInstance = false;
			return;
		}

		await PowerCmd.Apply<SlowPower>(Owner, amount, applier, cardSource, silent: true);
	}

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (side != Owner.Side)
		{
			return;
		}

		await PowerCmd.Remove(this);
		await PowerCmd.Apply<SlowPower>(Owner, -Amount, Owner, null, silent: true);
	}
}
