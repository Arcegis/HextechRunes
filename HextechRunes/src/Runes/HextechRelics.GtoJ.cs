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
			|| !HextechStableRandom.PercentChance(
				(RunState)Owner.RunState,
				DynamicVars["ForgeRewardChance"].IntValue,
				"gacha-addict-forge-reward",
				HextechStableRandom.PlayerKey(Owner),
				Owner.Relics.Count.ToString()))
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

public sealed class GoldrendRune : HextechRelicBase
{
	private int _countThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CountPerHit", 10m)
	];

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

	public override Task BeforeCombatStart()
	{
		_countThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		if (Owner != null && _countThisCombat > 0)
		{
			room.AddExtraReward(Owner, new GoldReward(_countThisCombat, Owner));
		}

		_countThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (target.Side != CombatSide.Enemy || result.TotalDamage <= 0 || !IsDamageFromOwner(dealer, cardSource))
		{
			return Task.CompletedTask;
		}

		_countThisCombat += DynamicVars["CountPerHit"].IntValue;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

}

public sealed class GoliathRune : HextechRelicBase
{
	private int _baseMaxHp;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedBaseMaxHp
	{
		get => _baseMaxHp;
		set => _baseMaxHp = Math.Max(0, value);
	}

	public int BaseMaxHp
	{
		get => _baseMaxHp;
		set => _baseMaxHp = Math.Max(1, value);
	}

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpGainPercent", 0.35m),
		new DynamicVar("DamageMultiplier", 1.2m),
		new DynamicVar("SustainMultiplier", 1.2m),
		new DynamicVar("Scale", 1.35m)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		EnsureBaseMaxHpInitialized(assumeAlreadyScaled: false);
		await CreatureCmdCompat.SetMaxHp(Owner.Creature, BaseMaxHp);
		await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp - Owner.Creature.CurrentHp);
		Grow();
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (Owner != null)
		{
			EnsureBaseMaxHpInitialized(assumeAlreadyScaled: true);
		}

		Grow();
		return Task.CompletedTask;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwner(dealer, cardSource) ? DynamicVars["DamageMultiplier"].BaseValue : 1m;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? DynamicVars["SustainMultiplier"].BaseValue : 1m;
	}

	private void Grow()
	{
		if (Owner == null)
		{
			return;
		}

		NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.SetDefaultScaleTo((float)DynamicVars["Scale"].BaseValue, 0f);
	}

	public void EnsureBaseMaxHpInitialized(bool assumeAlreadyScaled = true)
	{
		if (Owner != null && _baseMaxHp <= 0)
		{
			_baseMaxHp = assumeAlreadyScaled
				? Math.Max(1, FloorToInt(Owner.Creature.MaxHp / DynamicVars["Scale"].BaseValue))
				: Owner.Creature.MaxHp;
		}
	}

	public int GetScaledMaxHp()
	{
		return FloorToInt(BaseMaxHp * DynamicVars["Scale"].BaseValue);
	}
}

public sealed class GroundedRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead || Owner.Creature.Block <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, Owner.Creature.Block, ValueProp.Unpowered, null);
	}
}

public sealed class GrowingStrongerRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (Owner == null
			|| power.Owner != Owner.Creature
			|| power.GetType() != typeof(StrengthPower)
			|| amount <= 0m
			|| Owner.PlayerCombatState == null)
		{
			return Task.CompletedTask;
		}

		int cardsToFree = FloorToInt(amount);
		if (cardsToFree <= 0)
		{
			return Task.CompletedTask;
		}

		bool freedAny = false;
		for (int i = 0; i < cardsToFree; i++)
		{
			CardModel? card = PickCardToMakeFree(i, cardsToFree);
			if (card == null)
			{
				break;
			}

			card.SetToFreeThisTurn();
			freedAny = true;
		}

		if (freedAny)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private CardModel? PickCardToMakeFree(int ordinal, int total)
	{
		if (Owner?.PlayerCombatState == null)
		{
			return null;
		}

		IReadOnlyList<CardModel> handCards = PileType.Hand.GetPile(Owner).Cards;
		return PickCardToMakeFreeFromCandidates(
				handCards.Where(static card => card.CostsEnergyOrStars(includeGlobalModifiers: false)).ToList(),
				ordinal,
				total,
				includeGlobalModifiers: false)
			?? PickCardToMakeFreeFromCandidates(
				handCards.Where(static card => card.CostsEnergyOrStars(includeGlobalModifiers: true)).ToList(),
				ordinal,
				total,
				includeGlobalModifiers: true);
	}

	private CardModel? PickCardToMakeFreeFromCandidates(IReadOnlyList<CardModel> candidates, int ordinal, int total, bool includeGlobalModifiers)
	{
		if (Owner == null || candidates.Count == 0)
		{
			return null;
		}

		int index = HextechStableRandom.Index(
			(RunState)Owner.RunState,
			candidates.Count,
			"guinsoos-rageblade-free-card",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			ordinal.ToString(),
			total.ToString(),
			includeGlobalModifiers ? "global" : "base",
			HextechStableRandom.CardPileKey(candidates));
		return candidates[index];
	}
}

public sealed class HailToTheKingRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("InitialForgeCount", 2m),
		new DynamicVar("EliteForgeCount", 1m),
		new DynamicVar("BossForgeCount", 1m)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await HextechForgeGrantHelper.ObtainRandomForges(Owner, DynamicVars["InitialForgeCount"].IntValue);
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		if (room.RoomType == RoomType.Elite)
		{
			Flash(Array.Empty<Creature>());
			for (int i = 0; i < DynamicVars["EliteForgeCount"].IntValue; i++)
			{
				HextechForgeGrantHelper.AddRandomForgeReward(Owner, room, HextechRarityTier.Gold);
			}
		}
		else if (room.RoomType == RoomType.Boss)
		{
			Flash(Array.Empty<Creature>());
			for (int i = 0; i < DynamicVars["BossForgeCount"].IntValue; i++)
			{
				HextechForgeGrantHelper.AddRandomForgeReward(Owner, room, HextechRarityTier.Prismatic);
			}
		}

		return Task.CompletedTask;
	}
}

public sealed class HandOfBaronRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamageMultiplier", 1.2m),
		new DynamicVar("Shrink", 2m)
	];

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwner(dealer, cardSource) ? DynamicVars["DamageMultiplier"].BaseValue : 1m;
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ShrinkPower>(combatState.HittableEnemies, DynamicVars["Shrink"].BaseValue, Owner.Creature, null);
	}
}

public sealed class HappyAccidentRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbCount", 1m),
		new PowerVar<FocusPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FocusPower>()
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
		if (!addedByPlayer
			|| card.Owner != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState == null
			|| card.Type != CardType.Status)
		{
			return;
		}

		Flash();
		PlayerChoiceContext choiceContext = new BlockingPlayerChoiceContext();
		for (int i = 0; i < DynamicVars["OrbCount"].IntValue; i++)
		{
			OrbModel orb = HextechStableRandom.CreateOrb(
				(RunState)Owner.RunState,
				Owner,
				"happy-accident-status-orb",
				CombatManager.Instance.History.Entries.Count() + i,
				Owner.Creature.CombatState.RoundNumber);
			await OrbCmd.Channel(choiceContext, orb, Owner);
		}

		await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, card);
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

public sealed class HeavyHitterRune : HextechRelicBase
{
	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null || !IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		return 1m + Math.Min(30m, Math.Floor(Owner.Creature.MaxHp / 6m)) / 100m;
	}
}

public sealed class HolyFireRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];
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

public sealed class ImmortalBoneRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HealPercent", 50m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || !Owner.IsOstyAlive || Owner.Osty == null)
		{
			return Task.CompletedTask;
		}

		Flash([Owner.Osty]);
		int healAmount = Math.Max(1, FloorToInt(Owner.Osty.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m));
		return CreatureCmd.Heal(Owner.Osty, healAmount);
	}
}

public sealed class InfernalConduitRune : HextechRelicBase
{
	private int _pendingEnergy;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPendingEnergy
	{
		get => _pendingEnergy;
		set => _pendingEnergy = Math.Max(0, value);
	}

	public override Task BeforeCombatStart()
	{
		_pendingEnergy = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingEnergy = 0;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || cardPlay.Card.Owner != Owner || cardPlay.Target is not { Side: CombatSide.Enemy } enemy)
		{
			return;
		}

		await PowerCmd.Apply<HextechBurnPower>(enemy, 2m, Owner.Creature, cardPlay.Card);
	}

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.CombatState == null)
		{
			return Task.CompletedTask;
		}

		_pendingEnergy = Owner.Creature.CombatState.Enemies
			.Where(enemy => enemy.IsAlive)
			.Sum(enemy => Math.Max(0, enemy.GetPowerAmount<HextechBurnPower>()) / 6);
		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || _pendingEnergy <= 0)
		{
			return;
		}

		int energy = _pendingEnergy;
		_pendingEnergy = 0;
		Flash();
		await PlayerCmd.GainEnergy(energy, player);
	}
}

public sealed class InfiniteLoopRune : HextechRelicBase
{
	private int _combatVictories;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCombatVictories
	{
		get => _combatVictories;
		set => _combatVictories = Math.Max(0, value);
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1),
		new DynamicVar("Combats", 4m)
	];

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != Owner)
		{
			return amount;
		}

		return amount + DynamicVars.Energy.BaseValue + FloorToInt(_combatVictories / DynamicVars["Combats"].BaseValue);
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner != null && !Owner.Creature.IsDead)
		{
			SavedCombatVictories++;
		}

		return Task.CompletedTask;
	}
}

public sealed class JeweledGauntletRune : HextechRelicBase
{
	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (Owner == null || card.Owner != Owner)
		{
			return playCount;
		}

		bool shouldReplay = HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			33,
			"jeweled-gauntlet-replay",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			CombatManager.Instance.History.Entries.Count().ToString(),
			HextechStableRandom.CardKey(card));
		return shouldReplay ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (Owner != null && card.Owner == Owner)
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}

public sealed class JudicatorRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1),
		new DynamicVar("DamageMultiplier", 1.25m)
	];

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!IsDamageFromOwner(dealer, cardSource) || target?.Side != CombatSide.Enemy || target.CurrentHp * 2 >= target.MaxHp)
		{
			return 1m;
		}

		return DynamicVars["DamageMultiplier"].BaseValue;
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented
			|| Owner == null
			|| Owner.Creature.IsDead
			|| target.Side == Owner.Creature.Side
			|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(target))
		{
			return;
		}

		Flash(Array.Empty<Creature>());
		if (Owner.PlayerCombatState != null)
		{
			await PlayerCmd.SetEnergy(Owner.PlayerCombatState.MaxEnergy, Owner);
		}
	}
}
