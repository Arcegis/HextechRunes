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

public sealed class HextechBurnPower : HextechPowerBase
{
	private const decimal StackDecayPercent = 0.1m;
	private static int _resolveDepth;

	internal static bool IsResolvingDamage => _resolveDepth > 0;

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (Owner.Side == CombatSide.Player || side != Owner.Side)
		{
			return;
		}

		await ResolveBurn(new ThrowingPlayerChoiceContext(), blockable: false);
	}

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner.Side != CombatSide.Player || side != Owner.Side)
		{
			return;
		}

		await ResolveBurn(choiceContext, blockable: true);
	}

	private async Task ResolveBurn(PlayerChoiceContext choiceContext, bool blockable)
	{
		if (Amount <= 0 || !Owner.IsAlive)
		{
			return;
		}

		int stacks = Amount;
		int percentHpLoss = Math.Max(1, (int)Math.Floor(Owner.CurrentHp * stacks / 100m));
		int hpLoss = Math.Max(stacks, percentHpLoss);
		int stackLoss = Math.Max(1, (int)Math.Ceiling(stacks * StackDecayPercent));
		Flash();
		try
		{
			_resolveDepth++;
			ValueProp valueProps = ValueProp.Unpowered;
			if (!blockable)
			{
				valueProps |= ValueProp.Unblockable;
			}

			await CreatureCmd.Damage(choiceContext, Owner, hpLoss, valueProps, null, null);
		}
		finally
		{
			_resolveDepth--;
		}

		if (Owner.IsAlive)
		{
			await PowerCmd.Apply<HextechBurnPower>(Owner, -stackLoss, null, null);
		}
		else
		{
			await Cmd.CustomScaledWait(0.1f, 0.25f);
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

public sealed class HextechPowerShieldTemporaryStrengthPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => ModelDb.Relic<PowerShieldRune>();

	protected override bool IsVisibleInternal => false;
}

public sealed class HextechAttackReplayPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (!ShouldReplay(card))
		{
			return playCount;
		}

		return playCount + Amount;
	}

	public override async Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (!ShouldReplay(card))
		{
			return;
		}

		Flash();
		await PowerCmd.Remove(this);
	}

	private bool ShouldReplay(CardModel card)
	{
		return Amount > 0m
			&& card.Owner?.Creature == Owner
			&& IllusoryWeaponRune.IsAttackForEffects(card, card.Owner);
	}
}

public sealed class HextechPlayerSlowPower : HextechPowerBase
{
	internal const decimal CardPlaySlowIncrease = 9m;
	private int _cardsPlayedThisTurn;

	public int SavedCardsPlayedThisTurn
	{
		get => _cardsPlayedThisTurn;
		set => _cardsPlayedThisTurn = Math.Max(0, value);
	}

	public override PowerType Type => Amount < 0m ? PowerType.Buff : PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool AllowNegative => true;

	public override int DisplayAmount => (int)decimal.Round(Amount, 0, MidpointRounding.AwayFromZero);

	public override Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (side == Owner.Side)
		{
			SavedCardsPlayedThisTurn = 0;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner?.Creature != Owner)
		{
			return;
		}

		SavedCardsPlayedThisTurn++;
		await HextechPowerCmdCompat.Apply<HextechPlayerSlowPower>(Owner, CardPlaySlowIncrease, Owner, cardPlay.Card, silent: true);
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != Owner || Amount == 0m || (props & ValueProp.Unpowered) != 0)
		{
			return 1m;
		}

		decimal multiplier = 1m + Amount / 100m;
		return Math.Max(0m, multiplier);
	}

	public override Task AfterModifyingDamageAmount(CardModel? cardSource)
	{
		if (Amount != 0m)
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}

public sealed class HextechTemporarySlowPower : HextechPowerBase, ITemporaryPower
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
