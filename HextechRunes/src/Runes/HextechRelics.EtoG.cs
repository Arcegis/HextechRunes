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

public sealed class EurekaRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("RelicsNeeded", 6m),
		new EnergyVar(1)
	];

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != Owner)
		{
			return amount;
		}

		return amount + FloorToInt(player.Relics.Count / DynamicVars["RelicsNeeded"].BaseValue);
	}
}

public sealed class ExplosionArtRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("TurnStartCards", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<BigBang>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int cardsToCreate = DynamicVars["TurnStartCards"].IntValue;
		if (cardsToCreate <= 0)
		{
			return;
		}

		Flash();
		List<CardModel> cards = new(cardsToCreate);
		for (int i = 0; i < cardsToCreate; i++)
		{
			cards.Add(combatState.CreateCard<BigBang>(Owner));
		}

		await HextechCardGeneration.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
	}
}

public sealed class FanTheHammerRune : HextechRelicBase
{
	private bool _triggeredThisTurn;
	private bool _triggeredLastPlay;
	private HextechCombatState? _turnStateCombat;
	private int _turnStateRoundNumber = -1;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set
		{
			// Legacy save compatibility: this is turn-scoped runtime state and must not enter multiplayer checksums.
			_triggeredThisTurn = false;
			_triggeredLastPlay = false;
			_turnStateCombat = null;
			_turnStateRoundNumber = -1;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("NormalReplays", 1m),
		new DynamicVar("EliteReplays", 2m),
		new DynamicVar("BossReplays", 2m)
	];

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

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTurnState(combatState);
		}

		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		_triggeredLastPlay = false;
		if (card.Owner != Owner)
		{
			return playCount;
		}

		EnsureTurnStateCurrent();
		if (_triggeredThisTurn || !IsOwnedAttack(card))
		{
			return playCount;
		}

		_triggeredThisTurn = true;
		UpdateTurnStateIdentity();
		_triggeredLastPlay = true;
		return playCount + GetReplayCount();
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (_triggeredLastPlay && IsOwnedAttack(card))
		{
			Flash();
			_triggeredLastPlay = false;
		}

		return Task.CompletedTask;
	}

	private int GetReplayCount()
	{
		if (Owner?.RunState.CurrentRoom is CombatRoom { RoomType: RoomType.Boss })
		{
			return DynamicVars["BossReplays"].IntValue;
		}

		if (Owner?.RunState.CurrentRoom is CombatRoom { RoomType: RoomType.Elite })
		{
			return DynamicVars["EliteReplays"].IntValue;
		}

		return DynamicVars["NormalReplays"].IntValue;
	}

	private void ResetTurnState(HextechCombatState? combatState = null)
	{
		_triggeredThisTurn = false;
		_triggeredLastPlay = false;
		UpdateTurnStateIdentity(combatState);
	}

	private void EnsureTurnStateCurrent()
	{
		HextechCombatState? combatState = Owner?.Creature.CombatState;
		if (combatState == null)
		{
			_triggeredThisTurn = false;
			_triggeredLastPlay = false;
			_turnStateCombat = null;
			_turnStateRoundNumber = -1;
			return;
		}

		if (!ReferenceEquals(_turnStateCombat, combatState) || _turnStateRoundNumber != combatState.RoundNumber)
		{
			ResetTurnState(combatState);
		}
	}

	private void UpdateTurnStateIdentity(HextechCombatState? combatState = null)
	{
		combatState ??= Owner?.Creature.CombatState;
		_turnStateCombat = combatState;
		_turnStateRoundNumber = combatState?.RoundNumber ?? -1;
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
		HoverTipFactory.FromPower<VulnerablePower>(),
		HoverTipFactory.FromPower<HextechBurnPower>()
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

public sealed class FeyMagicRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set
		{
			// Legacy save compatibility: this is turn-scoped runtime state and must not enter multiplayer checksums.
			_triggeredThisTurn = false;
			UpdateTurnScopedStateIdentity(null);
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MinCost", 2m)
	];

	public override Task BeforeCombatStart()
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTurnState(combatState);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (_triggeredThisTurn
			|| Owner == null
			|| target.Side != CombatSide.Enemy
			|| result.TotalDamage <= 0m
			|| !IsOwnedNonXCardWithCostAtLeast(cardSource, DynamicVars["MinCost"].BaseValue))
		{
			return;
		}

		_triggeredThisTurn = true;
		UpdateTurnScopedStateIdentity();
		Flash([target]);
		await CreatureCmd.Stun(target, null);
	}

	private void ResetTurnState()
	{
		ResetTurnState(null);
	}

	private void ResetTurnState(HextechCombatState? combatState)
	{
		_triggeredThisTurn = false;
		UpdateTurnScopedStateIdentity(combatState);
	}
}

public sealed class FinalFormRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set
		{
			// Legacy save compatibility: this is turn-scoped runtime state and must not enter multiplayer checksums.
			_triggeredThisTurn = false;
			UpdateTurnScopedStateIdentity(null);
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MinCost", 2m),
		new DynamicVar("BlockPercent", 0.2m),
		new CardsVar(2)
	];

	public override Task BeforeCombatStart()
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTurnState(combatState);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (_triggeredThisTurn || Owner == null || !IsOwnedNonXCardWithCostAtLeast(cardPlay.Card, DynamicVars["MinCost"].BaseValue))
		{
			return;
		}

		_triggeredThisTurn = true;
		UpdateTurnScopedStateIdentity();
		int block = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["BlockPercent"].BaseValue));
		Flash();
		await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Unpowered, cardPlay, fast: false);
		await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}

	private void ResetTurnState()
	{
		ResetTurnState(null);
	}

	private void ResetTurnState(HextechCombatState? combatState)
	{
		_triggeredThisTurn = false;
		UpdateTurnScopedStateIdentity(combatState);
	}
}

public sealed class FirebrandRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null || target.Side != CombatSide.Enemy || !HextechSts2Compat.IsPoweredAttack(props) || !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		await PowerCmd.Apply<HextechBurnPower>(target, 2m, Owner.Creature, cardSource);
	}
}

public sealed class FirstAidKitRune : HextechRelicBase
{
	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? 1.25m : 1m;
	}
}

public sealed class FlawlessRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedCard(cardPlay.Card) || Owner == null || Owner.Creature.IsDead || !IsColorlessCard(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
	}

	private static bool IsColorlessCard(CardModel card)
	{
		CardPoolModel colorlessPool = ModelDb.CardPool<ColorlessCardPool>();
		return card.Pool == colorlessPool || card.VisualCardPool == colorlessPool;
	}
}

public sealed class FrostWraithRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("TurnsNeeded", 3m),
		new PowerVar<SlowPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<SlowPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		await ApplySlow(combatState);
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RoundNumber <= 1
			|| (combatState.RoundNumber - 1) % DynamicVars["TurnsNeeded"].IntValue != 0)
		{
			return;
		}

		await ApplySlow(combatState);
	}

	private async Task ApplySlow(HextechCombatState combatState)
	{
		IReadOnlyList<Creature> enemies = combatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		await PowerCmd.Apply<HextechTemporarySlowPower>(enemies, DynamicVars["SlowPower"].BaseValue, Owner.Creature, null);
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

public sealed class GetExcitedRune : HextechRelicBase
{
	private int _pendingEnergy;
	private int _pendingDraw;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPendingEnergy
	{
		get => _pendingEnergy;
		set => _pendingEnergy = Math.Max(0, value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPendingDraw
	{
		get => _pendingDraw;
		set => _pendingDraw = Math.Max(0, value);
	}

	public override Task BeforeCombatStart()
	{
		_pendingEnergy = 2;
		_pendingDraw = 2;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingEnergy = 0;
		_pendingDraw = 0;
		return Task.CompletedTask;
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (Owner == null
			|| wasRemovalPrevented
			|| target.Side == Owner.Creature.Side
			|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(target))
		{
			return Task.CompletedTask;
		}

		_pendingEnergy += 2;
		_pendingDraw += 2;
		Flash();
		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner)
		{
			return;
		}

		int energy = _pendingEnergy;
		int draw = _pendingDraw;
		_pendingEnergy = 0;
		_pendingDraw = 0;
		if (energy > 0)
		{
			await PlayerCmd.GainEnergy(energy, player);
		}

		if (draw > 0)
		{
			await CardPileCmd.Draw(choiceContext, draw, player);
		}
	}
}

public sealed class GhostFormRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<IntangiblePower>(3m),
		new PowerVar<NoBlockPower>(3m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<IntangiblePower>(),
		HoverTipFactory.FromPower<NoBlockPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<IntangiblePower>(Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<NoBlockPower>(Owner.Creature, DynamicVars["NoBlockPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class GiantSlayerRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new DynamicVar("HpGap", 6m),
		new DynamicVar("DamagePerStepPercent", 0.01m),
		new DynamicVar("MaxBonusPercent", 0.5m)
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner)
		{
			return count;
		}

		return count + DynamicVars.Cards.BaseValue;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null || target?.Side != CombatSide.Enemy || !IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		int hpGap = target.MaxHp - Owner.Creature.MaxHp;
		if (hpGap <= 0)
		{
			return 1m;
		}

		int steps = hpGap / DynamicVars["HpGap"].IntValue;
		decimal bonus = Math.Min(steps * DynamicVars["DamagePerStepPercent"].BaseValue, DynamicVars["MaxBonusPercent"].BaseValue);
		return 1m + bonus;
	}
}

public sealed class GlassCannonRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamageMultiplier", 1.5m),
		new DynamicVar("HealCapPercent", 0.7m)
	];

	public decimal HealCapPercent => DynamicVars["HealCapPercent"].BaseValue;

	public override async Task AfterObtained()
	{
		if (Owner?.Creature == null)
		{
			return;
		}

		int hpCap = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * HealCapPercent));
		if (Owner.Creature.CurrentHp > hpCap)
		{
			await CreatureCmd.SetCurrentHp(Owner.Creature, hpCap);
		}
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
