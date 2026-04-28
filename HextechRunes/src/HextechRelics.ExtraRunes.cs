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

public sealed class FirstAidKitRune : HextechRelicBase
{
	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? 1.25m : 1m;
	}
}

public sealed class HomeguardRune : HextechRelicBase
{
	private bool _tookUnblockedDamageSinceLastTurn;
	private bool _hasPreviousTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTookUnblockedDamageSinceLastTurn
	{
		get => _tookUnblockedDamageSinceLastTurn;
		set => _tookUnblockedDamageSinceLastTurn = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedHasPreviousTurn
	{
		get => _hasPreviousTurn;
		set => _hasPreviousTurn = value;
	}

	public override Task BeforeCombatStart()
	{
		_tookUnblockedDamageSinceLastTurn = false;
		_hasPreviousTurn = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_tookUnblockedDamageSinceLastTurn = false;
		_hasPreviousTurn = false;
		return Task.CompletedTask;
	}

	public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner != null && target == Owner.Creature && result.UnblockedDamage > 0)
		{
			_tookUnblockedDamageSinceLastTurn = true;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner)
		{
			return;
		}

		if (_hasPreviousTurn && !_tookUnblockedDamageSinceLastTurn)
		{
			Flash();
			await CardPileCmd.Draw(choiceContext, 2m, player);
		}

		_hasPreviousTurn = true;
		_tookUnblockedDamageSinceLastTurn = false;
	}
}

public sealed class LightEmUpRune : HextechRelicBase
{
	private const int AttacksPerReplay = 4;

	private int _attacksPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisCombat
	{
		get => _attacksPlayedThisCombat;
		set
		{
			_attacksPlayedThisCombat = Math.Max(0, value) % AttacksPerReplay;
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			return _attacksPlayedThisCombat;
		}
	}

	public override Task BeforeCombatStart()
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (!IsOwnedAttack(card))
		{
			return playCount;
		}

		_attacksPlayedThisCombat++;
		if (_attacksPlayedThisCombat >= AttacksPerReplay)
		{
			_attacksPlayedThisCombat = 0;
			InvokeDisplayAmountChanged();
			return playCount + 1;
		}

		InvokeDisplayAmountChanged();
		return playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (IsOwnedAttack(card))
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private void ResetAttacksPlayedThisCombat()
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}

public sealed class HolyFireRune : HextechRelicBase
{
}

public sealed class ShrinkEngineRune : HextechRelicBase
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

	public override Task AfterObtained()
	{
		Shrink();
		return Task.CompletedTask;
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		Shrink();
		return Task.CompletedTask;
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		SavedStacks++;
		Flash(Array.Empty<Creature>());
		Shrink();
		return Task.CompletedTask;
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + FloorToInt(_stacks / 4m) : count;
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return player == Owner ? amount + FloorToInt(_stacks / 8m) : amount;
	}

	private void Shrink()
	{
		if (Owner == null)
		{
			return;
		}

		float scale = Math.Max(0.2f, 1f - _stacks * 0.02f);
		NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.SetDefaultScaleTo(scale, 0f);
	}
}

public sealed class BackToBasicsRune : HextechRelicBase
{
	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		return card.Owner != Owner
			|| card.EnergyCost.CostsX
			|| card.EnergyCost.GetAmountToSpend() < 3m;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? 1.4m : 1m;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwner(dealer, cardSource) ? 1.4m : 1m;
	}
}

public sealed class DrawYourSwordRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[
				new DynamicVar("HpGainPercent", 0.3m),
				new PowerVar<StrengthPower>(3m),
				new PowerVar<DexterityPower>(3m),
				new PowerVar<FocusPower>(3m)
			];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		int hpGain = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["HpGainPercent"].BaseValue));
		await CreatureCmd.GainMaxHp(Owner.Creature, hpGain);
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return player == Owner ? Math.Max(0m, amount - 1m) : amount;
	}

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class FeelTheBurnRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<WeakPower>(2m),
		new PowerVar<VulnerablePower>(2m),
		new DynamicVar("BurnPercent", 10m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<VulnerablePower>()
	];

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null
			|| target != Owner.Creature
			|| result.UnblockedDamage <= 0
			|| Owner.Creature.CombatState == null)
		{
			return;
		}

		List<Creature> enemies = Owner.Creature.CombatState.Enemies.Where(static enemy => enemy.IsAlive).ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		int burnAmount = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["BurnPercent"].BaseValue / 100m));
		await PowerCmd.Apply<WeakPower>(enemies, DynamicVars.Weak.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<VulnerablePower>(enemies, DynamicVars.Vulnerable.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<HextechBurnPower>(enemies, burnAmount, Owner.Creature, null);
	}
}

public sealed class StartupRoutineRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(15m, ValueProp.Unpowered)
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
	}
}

public sealed class CollectorRune : HextechRelicBase
{
	private int _countThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCountThisCombat
	{
		get => _countThisCombat;
		set
		{
			_countThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? _countThisCombat : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CountPerDeath", 5m)
	];

	public override Task BeforeCombatStart()
	{
		ResetCount();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		if (Owner != null && _countThisCombat > 0)
		{
			room.AddExtraReward(Owner, new GoldReward(_countThisCombat, Owner));
		}

		ResetCount();
		return Task.CompletedTask;
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented
			|| Owner == null
			|| Owner.Creature.IsDead
			|| target.Side == Owner.Creature.Side
			|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(target))
		{
			return Task.CompletedTask;
		}

		_countThisCombat += DynamicVars["CountPerDeath"].IntValue;
		InvokeDisplayAmountChanged();
		Flash();
		return Task.CompletedTask;
	}

	private void ResetCount()
	{
		_countThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}

public sealed class LifeFlowRune : HextechRelicBase
{
	private int _procsThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedProcsThisTurn
	{
		get => _procsThisTurn;
		set
		{
			_procsThisTurn = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? Math.Max(0, DynamicVars["MaxProcsPerTurn"].IntValue - _procsThisTurn) : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HealPercent", 0.05m),
		new DynamicVar("MaxProcsPerTurn", 3m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetProcs();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetProcs();
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetProcs();
		}

		return Task.CompletedTask;
	}

	public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (!IsOwnedCard(card)
			|| Owner == null
			|| Owner.Creature.IsDead
			|| _procsThisTurn >= DynamicVars["MaxProcsPerTurn"].IntValue)
		{
			return Task.CompletedTask;
		}

		_procsThisTurn++;
		InvokeDisplayAmountChanged();
		int healAmount = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["HealPercent"].BaseValue));
		Flash();
		return CreatureCmd.Heal(Owner.Creature, healAmount);
	}

	private void ResetProcs()
	{
		_procsThisTurn = 0;
		InvokeDisplayAmountChanged();
	}
}

public sealed class TrickLicenseRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Sly)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPlayForFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPlayForFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	private bool ShouldPlayForFree(CardModel card)
	{
		return Owner != null
			&& card.Owner == Owner
			&& card.IsSlyThisTurn
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}
}

public sealed class GalacticGiftRune : HextechRelicBase
{
	private int _starsSpentThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedStarsSpentThisCombat
	{
		get => _starsSpentThisCombat;
		set
		{
			_starsSpentThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			int starsNeeded = DynamicVars["StarsSpent"].IntValue;
			int remainder = _starsSpentThisCombat % starsNeeded;
			return remainder == 0 ? starsNeeded : starsNeeded - remainder;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StarsVar("StarsSpent", 3),
		new StarsVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetStarsSpent();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetStarsSpent();
		return Task.CompletedTask;
	}

	public override Task AfterStarsSpent(int amount, Player spender)
	{
		if (spender != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0)
		{
			return Task.CompletedTask;
		}

		int starsNeeded = DynamicVars["StarsSpent"].IntValue;
		_starsSpentThisCombat += amount;
		int rewards = _starsSpentThisCombat / starsNeeded;
		_starsSpentThisCombat %= starsNeeded;
		InvokeDisplayAmountChanged();
		if (rewards <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainStars(rewards * DynamicVars.Stars.BaseValue, Owner);
	}

	private void ResetStarsSpent()
	{
		_starsSpentThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}

public sealed class SomethingFromNothingRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || cardPlay.Card.Owner != Owner || cardPlay.Card is not MegaCrit.Sts2.Core.Models.Cards.Void)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}
}

public sealed class LubricantRune : HextechRelicBase
{
	private bool _usedThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedUsedThisTurn
	{
		get => _usedThisTurn;
		set
		{
			_usedThisTurn = value;
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical && !_usedThisTurn ? 1 : 0;

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetTurnState();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState();
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTurnState();
		}

		return Task.CompletedTask;
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPowerCardBeFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPowerCardBeFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (_usedThisTurn
			|| cardPlay.IsAutoPlay
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card.Type != CardType.Power)
		{
			return Task.CompletedTask;
		}

		_usedThisTurn = true;
		InvokeDisplayAmountChanged();
		Flash();
		return Task.CompletedTask;
	}

	private bool ShouldPowerCardBeFree(CardModel card)
	{
		return !_usedThisTurn
			&& Owner != null
			&& card.Owner == Owner
			&& card.Type == CardType.Power
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}

	private void ResetTurnState()
	{
		_usedThisTurn = false;
		InvokeDisplayAmountChanged();
	}
}

public sealed class HubrisRune : HextechRelicBase
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
		new DynamicVar("StacksPerBonus", 3m),
		new PowerVar<StrengthPower>(1m),
		new CardsVar(1)
	];

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner != null && !Owner.Creature.IsDead)
		{
			SavedStacks++;
			Flash(Array.Empty<Creature>());
		}

		return Task.CompletedTask;
	}

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null)
		{
			return;
		}

		int bonus = GetBonusAmount();
		if (bonus <= 0)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, bonus * DynamicVars.Strength.BaseValue, Owner.Creature, null);
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner || player.Creature.CombatState?.RoundNumber > 1)
		{
			return count;
		}

		return count + GetBonusAmount() * DynamicVars.Cards.BaseValue;
	}

	private int GetBonusAmount()
	{
		return FloorToInt(_stacks / DynamicVars["StacksPerBonus"].BaseValue);
	}
}

public sealed class GoldenSpatulaRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
			new PowerVar<StrengthPower>(1m),
			new PowerVar<DexterityPower>(1m),
			new PowerVar<FocusPower>(1m),
			new DynamicVar("ForgeRewardChance", 50m)
		];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, null);
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| Owner.PlayerRng.Rewards.NextInt(100) >= DynamicVars["ForgeRewardChance"].IntValue)
		{
			return Task.CompletedTask;
		}

		if (HextechForgeGrantHelper.AddRandomForgeReward(Owner, room))
		{
			Flash(Array.Empty<Creature>());
		}

		return Task.CompletedTask;
	}
}

public sealed class UnyieldingArmorRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>()
	];

	public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		if (Owner == null || target != Owner.Creature || canonicalPower is not PlatingPower || amount >= 0m)
		{
			return false;
		}

		modifiedAmount = 0m;
		Flash();
		return true;
	}
}

public sealed class NightParadeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
			CardModel card = ModelDb.Card<Soul>().ToMutable();
			card.Owner = Owner;
			await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: true, position: CardPilePosition.Random);
			SaveManager.Instance.MarkCardAsSeen(card);
		}
	}
}

public sealed class DrainRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CalamityMultiplier", 2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<CalamityPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
	{
		if (summoner != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0m || Owner.Creature.CombatState == null)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		await PowerCmd.Apply<CalamityPower>(enemies, amount * DynamicVars["CalamityMultiplier"].BaseValue, Owner.Creature, null);
	}
}

public sealed class LethalTempoRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedCard(cardPlay.Card) || !cardPlay.Card.Tags.Contains(CardTag.Shiv) || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<HextechLethalTempoTemporaryStrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, cardPlay.Card);
	}
}

public sealed class EmergenceRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbCount", 2m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		for (int i = 0; i < DynamicVars["OrbCount"].IntValue; i++)
		{
			OrbModel orb = OrbModel.GetRandomOrb(Owner.RunState.Rng.CombatOrbGeneration).ToMutable();
			await OrbCmd.Channel(choiceContext, orb, Owner);
		}
	}
}

public sealed class PrecisionCognitionRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FocusPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class HastyScribbleRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(5)
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + DynamicVars.Cards.BaseValue : count;
	}
}

public sealed class ClownCollegeRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<TrickMagicCard>(DynamicVars.Cards.IntValue);
	}
}

public sealed class BladeWaltzRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<BladeWaltzCard>(DynamicVars.Cards.IntValue);
	}
}

public sealed class TrickMagicCard : CardModel
{
	public override CardPoolModel Pool => Owner?.Character.CardPool ?? ModelDb.CardPool<ColorlessCardPool>();

	public override CardPoolModel VisualCardPool => Pool;

	public override string PortraitPath => ModelDb.Card<Acrobatics>().PortraitPath;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust
	];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new PowerVar<BufferPower>(2m),
		new DynamicVar("Replays", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<BufferPower>(),
		HoverTipFactory.FromPower<HextechAttackReplayPower>()
	];

	public TrickMagicCard()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
		await PowerCmd.Apply<BufferPower>(Owner.Creature, DynamicVars["BufferPower"].BaseValue, Owner.Creature, this);
		await PowerCmd.Apply<HextechAttackReplayPower>(Owner.Creature, DynamicVars["Replays"].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars["Replays"].UpgradeValueBy(1m);
	}
}

public sealed class BladeWaltzCard : CardModel
{
	public override CardPoolModel Pool => Owner?.Character.CardPool ?? ModelDb.CardPool<ColorlessCardPool>();

	public override CardPoolModel VisualCardPool => Pool;

	public override string PortraitPath => ModelDb.Card<Ricochet>().PortraitPath;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	public override IEnumerable<CardKeyword> CanonicalKeywords
	{
		get
		{
			if (IsUpgraded)
			{
				return [CardKeyword.Retain, CardKeyword.Exhaust];
			}

			return [CardKeyword.Exhaust];
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(3m, ValueProp.Move),
		new DynamicVar("Hits", 9m),
		new PowerVar<IntangiblePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<IntangiblePower>()
	];

	public BladeWaltzCard()
		: base(2, CardType.Attack, CardRarity.Token, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CombatState combatState = Owner.Creature.CombatState
			?? throw new InvalidOperationException("Blade Waltz played outside combat.");
		for (int i = 0; i < DynamicVars["Hits"].IntValue; i++)
		{
			List<Creature> enemies = combatState.HittableEnemies.ToList();
			if (enemies.Count == 0)
			{
				break;
			}

			Creature enemy = enemies[Owner.RunState.Rng.Niche.NextInt(enemies.Count)];
			await CreatureCmd.Damage(choiceContext, enemy, DynamicVars.Damage, Owner.Creature, this);
		}

		await PowerCmd.Apply<IntangiblePower>(Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, this);
	}
}

public sealed class TransmuteChaosRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Player player = Owner;
		Flash();
		await HextechRuneGrantHelper.ConsumeAndObtainRandomRunes(this, player, ModInfo.GetAllSelectableRuneTypes(), 2);
	}
}

public sealed class TransmutePrismaticRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Player player = Owner;
		Flash();
		await HextechRuneGrantHelper.ConsumeAndObtainRandomRunes(this, player, ModInfo.GetPlayerRuneTypesForRarity(HextechRarityTier.Prismatic), 1);
	}
}

public sealed class TransmuteGoldRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Player player = Owner;
		Flash();
		await HextechRuneGrantHelper.ConsumeAndObtainRandomRunes(this, player, ModInfo.GetPlayerRuneTypesForRarity(HextechRarityTier.Gold), 1);
	}
}
