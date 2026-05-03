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
using MegaCrit.Sts2.Core.Localization;
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
using MegaCrit.Sts2.Core.Models.Monsters;

namespace HextechRunes;

public sealed class AdamantRune : LimitedDebuffProcRelicBase
{
	protected override int MaxProcsPerTurn => 5;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	protected override Task OnEnemyDebuffApplied(Creature target)
	{
		return CreatureCmd.GainBlock(Owner!.Creature, DynamicVars.Block, null);
	}
}

public sealed class AdaptiveCapacitorRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbSlots", 1m)
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
		await OrbCmd.AddSlots(Owner, DynamicVars["OrbSlots"].IntValue);
	}
}

public sealed class AncientWineRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new HealVar(1m)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedSkill(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.Heal(Owner!.Creature, DynamicVars.Heal.BaseValue);
	}
}

public sealed class ArcanePunchRune : HextechRelicBase
{
	private int _attacksPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisCombat
	{
		get => IsNetworkMultiplayer() ? 0 : GetAttacksPlayedThisCombat();
		set
		{
			_attacksPlayedThisCombat = Math.Max(0, value);
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

			int remainder = GetAttacksPlayedThisCombat() % 2;
			return remainder == 0 ? 2 : 1;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("AttacksPerEnergy", 2m),
		new EnergyVar(1)
	];

	public override Task BeforeCombatStart()
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedAttack(cardPlay.Card))
		{
			return;
		}

		if (ShouldUseNetworkCombatHistory())
		{
			await ResolveAttackProgressFromHistory();
			return;
		}

		int attacksPlayed = _attacksPlayedThisCombat + 1;
		_attacksPlayedThisCombat = attacksPlayed;
		InvokeDisplayAmountChanged();
		if (attacksPlayed % 2 != 0)
		{
			return;
		}

		await GainEnergyForAttackThreshold();
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (ShouldUseNetworkCombatHistory() && IsOwnedAttack(cardPlay.Card))
		{
			await ResolveAttackProgressFromHistory();
		}
	}

	private async Task ResolveAttackProgressFromHistory()
	{
		int attacksPlayed = CountOwnedAttackCardsPlayedFromHistory(firstInSeriesOnly: false, includeAutoPlay: true);
		int previousAttacksPlayed = _attacksPlayedThisCombat;
		if (attacksPlayed <= previousAttacksPlayed)
		{
			return;
		}

		_attacksPlayedThisCombat = attacksPlayed;
		InvokeDisplayAmountChanged();
		int energyTriggers = attacksPlayed / 2 - previousAttacksPlayed / 2;
		for (int i = 0; i < energyTriggers; i++)
		{
			await GainEnergyForAttackThreshold();
		}
	}

	private async Task GainEnergyForAttackThreshold()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(1m, Owner);
	}

	private int GetAttacksPlayedThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedAttackCardsPlayedFromHistory(firstInSeriesOnly: false, includeAutoPlay: true)
			: _attacksPlayedThisCombat;
	}
}

public sealed class AstralBodyRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new MaxHpVar(50m),
		new DynamicVar("DamageMultiplier", 0.9m)
	];

	public override Task AfterObtained()
	{
		return CreatureCmd.GainMaxHp(Owner!.Creature, DynamicVars.MaxHp.BaseValue);
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		return DynamicVars["DamageMultiplier"].BaseValue;
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

public sealed class BadTasteRune : LimitedDebuffProcRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new HealVar(1m)
	];

	protected override Task OnEnemyDebuffApplied(Creature target)
	{
		return CreatureCmd.Heal(Owner!.Creature, DynamicVars.Heal.BaseValue);
	}
}

public sealed class BadgeBrothersRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FreeAttackPower>(1m),
		new PowerVar<FreeSkillPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FreeAttackPower>(),
		HoverTipFactory.FromPower<FreeSkillPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<FreeAttackPower>(Owner.Creature, DynamicVars["FreeAttackPower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<FreeSkillPower>(Owner.Creature, DynamicVars["FreeSkillPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class BeginningAndEndRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<LethalityPower>(25m),
		new PowerVar<ReaperFormPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<LethalityPower>(),
		HoverTipFactory.FromPower<ReaperFormPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<LethalityPower>(Owner.Creature, DynamicVars["LethalityPower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<ReaperFormPower>(Owner.Creature, DynamicVars["ReaperFormPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class BigStrengthRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamageMultiplier", 1.2m)
	];

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwner(dealer, cardSource) ? DynamicVars["DamageMultiplier"].BaseValue : 1m;
	}
}

public sealed class BladeWaltzCard : CardModel
{
	public override CardPoolModel Pool => IsMutable && Owner != null
		? Owner.Character.CardPool
		: ModelDb.CardPool<TokenCardPool>();

	public override CardPoolModel VisualCardPool => Pool;

	public override string PortraitPath => HextechAssets.BladeWaltzCardPortraitPath;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust
	];

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
		: base(1, CardType.Attack, CardRarity.Token, TargetType.AllEnemies, shouldShowInCardLibrary: true)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		HextechCombatState combatState = Owner.Creature.CombatState
			?? throw new InvalidOperationException("Blade Waltz played outside combat.");
		for (int i = 0; i < DynamicVars["Hits"].IntValue; i++)
		{
			List<Creature> enemies = combatState.HittableEnemies.ToList();
			if (enemies.Count == 0)
			{
				break;
			}

			Creature enemy = enemies[HextechStableRandom.Index(
				(RunState)Owner.RunState,
				enemies.Count,
				"blade-waltz-target",
				HextechStableRandom.PlayerKey(Owner),
				combatState.RoundNumber.ToString(),
				i.ToString(),
				CombatManager.Instance.History.Entries.Count().ToString(),
				HextechStableRandom.CardKey(this))];
			await CreatureCmd.Damage(choiceContext, enemy, DynamicVars.Damage, Owner.Creature, this);
		}

		await PowerCmd.Apply<IntangiblePower>(Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		_ = Keywords;
		RemoveKeyword(CardKeyword.Exhaust);
	}
}

public sealed class BladeWaltzRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<BladeWaltzCard>()
	];

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<BladeWaltzCard>(DynamicVars.Cards.IntValue);
	}
}

public sealed class BloodPactRune : HextechRelicBase
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
		return IsIroncladPlayer(player);
	}

	public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (Owner == null
			|| creature != Owner.Creature
			|| delta >= 0m
			|| Owner.Creature.IsDead
			|| !HextechSts2Compat.IsPartOfPlayerTurn(Owner))
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<HextechBloodPactTemporaryStrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
	}
}

public abstract class FirstTypedCardReplayRuneBase : HextechRelicBase
{
	private bool _triggeredThisTurn;
	private bool _triggeredLastPlay;

	protected abstract CardType TargetCardType { get; }

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 1m)
	];

	public override Task BeforeCombatStart()
	{
		ResetTriggered(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTriggered(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTriggered(combatState);
		}

		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		_triggeredLastPlay = false;
		EnsureTurnScopedStateCurrent(ResetTriggered);
		if (_triggeredThisTurn || !IsOwnedTargetType(card))
		{
			return playCount;
		}

		_triggeredThisTurn = true;
		_triggeredLastPlay = true;
		UpdateTurnScopedStateIdentity();
		return playCount + DynamicVars["Replays"].IntValue;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (_triggeredLastPlay && IsOwnedTargetType(card))
		{
			Flash();
			_triggeredLastPlay = false;
		}

		return Task.CompletedTask;
	}

	private bool IsOwnedTargetType(CardModel? card)
	{
		return card?.Owner == Owner && card.Type == TargetCardType;
	}

	private void ResetTriggered()
	{
		ResetTriggered(null);
	}

	private void ResetTriggered(HextechCombatState? combatState)
	{
		_triggeredThisTurn = false;
		_triggeredLastPlay = false;
		UpdateTurnScopedStateIdentity(combatState);
	}
}

public sealed class BreadAndButterRune : FirstTypedCardReplayRuneBase
{
	protected override CardType TargetCardType => CardType.Attack;
}

public sealed class BreadAndCheeseRune : FirstTypedCardReplayRuneBase
{
	protected override CardType TargetCardType => CardType.Power;
}

public sealed class BreadAndJamRune : FirstTypedCardReplayRuneBase
{
	protected override CardType TargetCardType => CardType.Skill;
}

public sealed class ByproductRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
#else
	public override async Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
#endif
	{
#if STS2_104_OR_NEWER
		bool addedByPlayer = creator == Owner;
#endif
		if (!addedByPlayer || card.Owner != Owner || Owner == null || Owner.Creature.IsDead || card.Type != CardType.Status)
		{
			return;
		}

		Flash();
		await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}
}

public sealed class CantTouchThisRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MinCost", 2m),
		new PowerVar<BufferPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<BufferPower>()
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card.EnergyCost.CostsX
			|| cardPlay.Card.EnergyCost.GetAmountToSpend() < DynamicVars["MinCost"].BaseValue)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<BufferPower>(Owner.Creature, DynamicVars["BufferPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class CerberusRune : HextechRelicBase
{
	private int _attacksPlayedThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisTurn
	{
		get
		{
			EnsureTurnScopedStateCurrent(ResetAttacksPlayedThisTurn);
			return _attacksPlayedThisTurn;
		}
		set
		{
			_attacksPlayedThisTurn = Math.Max(0, value);
			InvokeDisplayAmountChanged();
			UpdateTurnScopedStateIdentity();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? Math.Max(0, 3 - _attacksPlayedThisTurn) : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("FreeAttacks", 3m)
	];

	public override Task BeforeCombatStart()
	{
		ResetAttacksPlayedThisTurn(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetAttacksPlayedThisTurn(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetAttacksPlayedThisTurn(combatState);
		}

		return Task.CompletedTask;
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPlayAttackForFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPlayAttackForFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		EnsureTurnScopedStateCurrent(ResetAttacksPlayedThisTurn);
		if (!cardPlay.IsFirstInSeries || cardPlay.IsAutoPlay || !IsOwnedAttack(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		_attacksPlayedThisTurn++;
		InvokeDisplayAmountChanged();
		UpdateTurnScopedStateIdentity();
		if (_attacksPlayedThisTurn <= DynamicVars["FreeAttacks"].IntValue)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private bool ShouldPlayAttackForFree(CardModel card)
	{
		EnsureTurnScopedStateCurrent(ResetAttacksPlayedThisTurn);
		return Owner != null
			&& card.Owner == Owner
			&& card.Type == CardType.Attack
			&& card.Pile?.Type == PileType.Hand
			&& !card.EnergyCost.CostsX
			&& _attacksPlayedThisTurn < DynamicVars["FreeAttacks"].IntValue;
	}

	private void ResetAttacksPlayedThisTurn()
	{
		ResetAttacksPlayedThisTurn(null);
	}

	private void ResetAttacksPlayedThisTurn(HextechCombatState? combatState)
	{
		_attacksPlayedThisTurn = 0;
		InvokeDisplayAmountChanged();
		UpdateTurnScopedStateIdentity(combatState);
	}
}

public sealed class CircleOfDeathRune : HextechRelicBase
{
	public Task HandleSustainGained(decimal amount)
	{
		if (Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null || amount <= 0m)
		{
			return Task.CompletedTask;
		}

		int damage = FloorToInt(amount);
		if (damage <= 0)
		{
			return Task.CompletedTask;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies
			.Where(static enemy => enemy.IsAlive)
			.ToList();
		if (enemies.Count == 0)
		{
			return Task.CompletedTask;
		}

		Creature target = enemies[HextechStableRandom.Index(
			(RunState)Owner.RunState,
			enemies.Count,
			"circle-of-death-target",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState.RoundNumber.ToString(),
			damage.ToString(),
			CombatManager.Instance.History.Entries.Count().ToString())];
		Flash([target]);
		return CreatureCmd.Damage(new BlockingPlayerChoiceContext(), target, damage, ValueProp.Unpowered, Owner.Creature, null);
	}

	public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		return creature == Owner?.Creature ? HandleSustainGained(amount) : Task.CompletedTask;
	}
}

public sealed class ClownCollegeRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<TrickMagicCard>()
	];

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<TrickMagicCard>(DynamicVars.Cards.IntValue);
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
		new DynamicVar("CountPerDeath", 10m),
		new DynamicVar("DamageMultiplier", 1.1m)
	];

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return target?.Side == CombatSide.Enemy && IsDamageFromOwner(dealer, cardSource)
			? DynamicVars["DamageMultiplier"].BaseValue
			: 1m;
	}

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

public sealed class CondensedRadianceRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
		new StarsVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
#else
	public override async Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
#endif
	{
#if STS2_104_OR_NEWER
		bool addedByPlayer = creator == Owner;
#endif
		if (!addedByPlayer || card.Owner != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainStars(DynamicVars.Stars.BaseValue, Owner);
	}
}

public sealed class CourageOfColossusRune : LimitedDebuffProcRelicBase
{
	protected override int MaxProcsPerTurn => 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Plating", 3m)
	];

	protected override Task OnEnemyDebuffApplied(Creature target)
	{
		return PowerCmd.Apply<PlatingPower>(Owner!.Creature, DynamicVars["Plating"].BaseValue, Owner!.Creature, null);
	}
}
