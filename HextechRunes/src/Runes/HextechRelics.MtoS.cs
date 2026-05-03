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

public sealed class MountainSoulRune : HextechRelicBase
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
			decimal block = Math.Max(1, FloorToInt(player.Creature.MaxHp * 0.1m));
			await CreatureCmd.GainBlock(player.Creature, block, ValueProp.Unpowered, null);
		}

		_hasPreviousTurn = true;
		_tookUnblockedDamageSinceLastTurn = false;
	}
}

public sealed class MysteryRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<MayhemPower>(2m),
		new PowerVar<EntropyPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<MayhemPower>(),
		HoverTipFactory.FromPower<EntropyPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<MayhemPower>(Owner.Creature, DynamicVars["MayhemPower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<EntropyPower>(Owner.Creature, DynamicVars["EntropyPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class NightParadeRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Soul>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await AddCardCopiesToDeckOrHand<Soul>(DynamicVars.Cards.IntValue);
	}
}

public sealed class NightstalkingRune : HextechRelicBase
{
	private int _cardsDrawnThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCardsDrawnThisCombat
	{
		get => IsNetworkMultiplayer() ? 0 : GetCardsDrawnThisCombat();
		set
		{
			_cardsDrawnThisCombat = Math.Max(0, value);
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

			int cardsNeeded = DynamicVars["CardsNeeded"].IntValue;
			int remainder = GetCardsDrawnThisCombat() % cardsNeeded;
			return remainder == 0 ? cardsNeeded : cardsNeeded - remainder;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CardsNeeded", 15m),
		new PowerVar<IntangiblePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<IntangiblePower>()
	];

	public override Task BeforeCombatStart()
	{
		_cardsDrawnThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_cardsDrawnThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card.Owner != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		if (ShouldUseNetworkCombatHistory())
		{
			await ResolveDrawProgressFromHistory();
			return;
		}

		_cardsDrawnThisCombat++;
		InvokeDisplayAmountChanged();
		if (_cardsDrawnThisCombat % DynamicVars["CardsNeeded"].IntValue != 0)
		{
			return;
		}

		await ApplyDrawThresholdReward();
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (ShouldUseNetworkCombatHistory() && cardPlay.Card.Owner == Owner)
		{
			await ResolveDrawProgressFromHistory();
		}
	}

	public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (ShouldUseNetworkCombatHistory() && player == Owner)
		{
			await ResolveDrawProgressFromHistory();
		}
	}

#if !STS2_104_OR_NEWER
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (ShouldUseNetworkCombatHistory() && player == Owner)
		{
			await ResolveDrawProgressFromHistory();
		}
	}
#endif

	private async Task ResolveDrawProgressFromHistory()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int cardsDrawn = CountOwnedCardsDrawnFromHistory();
		int previousCardsDrawn = _cardsDrawnThisCombat;
		if (cardsDrawn <= previousCardsDrawn)
		{
			return;
		}

		int cardsNeeded = DynamicVars["CardsNeeded"].IntValue;
		_cardsDrawnThisCombat = cardsDrawn;
		InvokeDisplayAmountChanged();
		int rewards = cardsDrawn / cardsNeeded - previousCardsDrawn / cardsNeeded;
		for (int i = 0; i < rewards; i++)
		{
			await ApplyDrawThresholdReward();
		}
	}

	private async Task ApplyDrawThresholdReward()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<IntangiblePower>(Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, null);
	}

	private int GetCardsDrawnThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedCardsDrawnFromHistory()
			: _cardsDrawnThisCombat;
	}
}

public sealed class NimbleRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner)
		{
			return count;
		}

		return count + DynamicVars.Cards.BaseValue;
	}
}

public sealed class NoNonsenseRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m)
	];

	public async Task HandlePreventedNonHandDraw(int drawsPrevented)
	{
		if (Owner == null || Owner.Creature.IsDead || drawsPrevented <= 0)
		{
			return;
		}

		Flash();
		await Cmd.CustomScaledWait(0.05f, 0.1f);
	}
}

public sealed class OkBoomerangRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("TurnsNeeded", 2m),
		new DynamicVar("DamagePercent", 20m)
	];

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RoundNumber <= 1
			|| combatState.RoundNumber % DynamicVars["TurnsNeeded"].IntValue != 0)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = combatState.HittableEnemies.ToList();
		int damage = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["DamagePercent"].BaseValue / 100m));
		if (enemies.Count == 0 || damage <= 0)
		{
			return;
		}

		Flash(enemies);
		foreach (Creature enemy in enemies)
		{
			await CreatureCmd.Damage(
				choiceContext,
				enemy,
				damage,
				ValueProp.Unpowered | ValueProp.SkipHurtAnim,
				Owner.Creature,
				cardSource: null);
		}
	}
}

public sealed class OverflowRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return player == Owner ? amount + DynamicVars.Energy.BaseValue : amount;
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (Owner == null || card.Owner != Owner || card.Pile?.Type != PileType.Hand || card.EnergyCost.CostsX)
		{
			return false;
		}

		modifiedCost = originalCost + 1m;
		return true;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? 2m : 1m;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsOwnerOrPet(dealer) || cardSource?.Owner == Owner ? 2m : 1m;
	}
}

public sealed class PandorasBoxRune : HextechRelicBase
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
		await HextechRuneGrantHelper.ReplaceOwnedHextechRunesWithRandomRunes(
			player,
			HextechCatalog.GetPlayerRuneTypesForRarity(HextechRarityTier.Prismatic),
			new HashSet<ModelId> { ModelDb.GetId<PandorasBoxRune>() });
	}
}

public sealed class PlateletRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (Owner == null
			|| creature != Owner.Creature
			|| delta >= 0m
			|| Owner.Creature.IsDead
			|| !HextechSts2Compat.IsPartOfPlayerTurn(Owner))
		{
			return Task.CompletedTask;
		}

		decimal block = Math.Floor(-delta) * DynamicVars.Block.BaseValue;
		if (block <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Unpowered, null);
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

public sealed class ProtectiveVeilRune : HextechRelicBase
{
	private int _turnsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedTurnsThisCombat
	{
		get => _turnsThisCombat;
		set => _turnsThisCombat = Math.Max(0, value);
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ArtifactPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ArtifactPower>()
	];

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null)
		{
			return Task.CompletedTask;
		}

		_turnsThisCombat = 0;
		Flash();
		return PowerCmd.Apply<ArtifactPower>(Owner.Creature, DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, null);
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_turnsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side)
		{
			return;
		}

		_turnsThisCombat++;
		if (_turnsThisCombat % 2 != 0)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ArtifactPower>(Owner.Creature, DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class ProteinShakeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MaxHpPerStep", 2m),
		new DynamicVar("SustainPercentPerStep", 1m)
	];

	public decimal SustainMultiplier => Owner == null
		? 1m
		: 1m + Math.Floor(Owner.Creature.MaxHp / DynamicVars["MaxHpPerStep"].BaseValue) * DynamicVars["SustainPercentPerStep"].BaseValue / 100m;

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}
}

public sealed class QueenRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FrailPower>(2m),
		new PowerVar<WeakPower>(2m),
		new PowerVar<VulnerablePower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FrailPower>(),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<VulnerablePower>()
	];

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<FrailPower>(combatState.HittableEnemies, DynamicVars["FrailPower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<WeakPower>(combatState.HittableEnemies, DynamicVars.Weak.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<VulnerablePower>(combatState.HittableEnemies, DynamicVars.Vulnerable.BaseValue, Owner.Creature, null);
	}
}

public sealed class RedEnvelopeRune : HextechRelicBase
{
	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash(Array.Empty<Creature>());
		if (HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			75,
			"red-envelope-reward",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Relics.Count.ToString()))
		{
			room.AddExtraReward(Owner, new GoldReward(20, 50, Owner));
		}
		else
		{
			HextechForgeGrantHelper.AddRandomForgeReward(Owner, room);
		}

		return Task.CompletedTask;
	}
}

public sealed class RekindleRune : HextechRelicBase
{
	private int _exhaustedCardsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedExhaustedCardsThisCombat
	{
		get => _exhaustedCardsThisCombat;
		set
		{
			_exhaustedCardsThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? Math.Max(0, DynamicVars.Cards.IntValue - _exhaustedCardsThisCombat) : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new EnergyVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (!IsOwnedCard(card) || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		_exhaustedCardsThisCombat++;
		int energyToGain = 0;
		while (_exhaustedCardsThisCombat >= DynamicVars.Cards.IntValue)
		{
			_exhaustedCardsThisCombat -= DynamicVars.Cards.IntValue;
			energyToGain++;
		}

		InvokeDisplayAmountChanged();
		if (energyToGain <= 0)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue * energyToGain, Owner);
	}

	private void ResetCounter()
	{
		_exhaustedCardsThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}

public sealed class RenewalRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		if (!IsOwnedCard(card) || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}
}

public sealed class RepulsorRune : HextechRelicBase
{
	private bool _pendingTrigger;
	private bool _triggeredThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedPendingTrigger
	{
		get => _pendingTrigger;
		set => _pendingTrigger = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisCombat
	{
		get => _triggeredThisCombat;
		set => _triggeredThisCombat = value;
	}

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
		_pendingTrigger = false;
		_triggeredThisCombat = false;
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingTrigger = false;
		_triggeredThisCombat = false;
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null
			|| target != Owner.Creature
			|| result.UnblockedDamage <= 0
			|| _triggeredThisCombat
			|| _pendingTrigger)
		{
			return Task.CompletedTask;
		}

		_pendingTrigger = true;
		_triggeredThisCombat = true;
		Status = RelicStatus.Active;
		Flash();
		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || !_pendingTrigger)
		{
			return;
		}

		_pendingTrigger = false;
		Status = RelicStatus.Normal;
		Flash();
		await PowerCmd.Apply<SlipperyPower>(player.Creature, DynamicVars["SlipperyPower"].BaseValue, player.Creature, null);
	}
}

public sealed class SacrificeRune : HextechRelicBase
{
	private int _countThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CountPerEnemy", 5m),
		new DynamicVar("SustainMultiplier", 1.1m)
	];

	public decimal SustainMultiplier => DynamicVars["SustainMultiplier"].BaseValue;

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

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || player.Creature.CombatState == null)
		{
			return Task.CompletedTask;
		}

		_countThisCombat += player.Creature.CombatState.Enemies.Count(static enemy => enemy.IsAlive && enemy.Side == CombatSide.Enemy) * DynamicVars["CountPerEnemy"].IntValue;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}

}
