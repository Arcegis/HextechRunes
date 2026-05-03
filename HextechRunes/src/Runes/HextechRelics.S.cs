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

public sealed class SerpentsFangRune : HextechRelicBase
{
	private int _poisonApplicationsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPoisonApplicationsThisCombat
	{
		get => _poisonApplicationsThisCombat;
		set
		{
			_poisonApplicationsThisCombat = Math.Max(0, value);
			UpdateDisplay();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? Math.Max(0, DynamicVars["PoisonApplications"].IntValue - _poisonApplicationsThisCombat) : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<EnvenomPower>(1m),
		new DynamicVar("PoisonApplications", 2m),
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<EnvenomPower>(),
		HoverTipFactory.FromPower<PoisonPower>(),
		HoverTipFactory.FromCard<Shiv>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		ResetCounter();
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<EnvenomPower>(Owner.Creature, DynamicVars["EnvenomPower"].BaseValue, Owner.Creature, null);
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetCounter();
		return Task.CompletedTask;
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (power is not PoisonPower
			|| Owner == null
			|| Owner.Creature.IsDead
			|| !TryGetOwnedEnemyDebuffTarget(power, amount, applier, out Creature? target))
		{
			return;
		}

		_poisonApplicationsThisCombat++;
		int poisonApplicationsNeeded = DynamicVars["PoisonApplications"].IntValue;
		int shivsToCreate = 0;
		while (_poisonApplicationsThisCombat >= poisonApplicationsNeeded)
		{
			_poisonApplicationsThisCombat -= poisonApplicationsNeeded;
			shivsToCreate += DynamicVars.Cards.IntValue;
		}

		UpdateDisplay();
		if (shivsToCreate <= 0)
		{
			return;
		}

		Flash(target == null ? Array.Empty<Creature>() : [target]);
		await AddCardCopiesToCombatHand<Shiv>(shivsToCreate);
	}

	private void ResetCounter()
	{
		_poisonApplicationsThisCombat = 0;
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		Status = _poisonApplicationsThisCombat == DynamicVars["PoisonApplications"].IntValue - 1
			? RelicStatus.Active
			: RelicStatus.Normal;
		InvokeDisplayAmountChanged();
	}
}

public sealed class ServantMasterRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<NecroMasteryPower>(1m),
		new SummonVar(3m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<NecroMasteryPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || !IsNecrobinderPlayer(Owner))
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<NecroMasteryPower>(Owner.Creature, DynamicVars["NecroMasteryPower"].BaseValue, Owner.Creature, null);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
#else
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
#endif
	{
		if (player != Owner || Owner.Creature.IsDead || player.Creature.CombatState == null || !IsNecrobinderPlayer(player))
		{
			return;
		}

		Flash();
		await OstyCmd.Summon(choiceContext, player, DynamicVars.Summon.BaseValue, this);
	}
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

public sealed class ShrinkRayRune : HextechRelicBase
{
	private bool _applyingShrinkRay;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ShrinkPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ShrinkPower>()
	];

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (_applyingShrinkRay
			|| Owner == null
			|| target.Side != CombatSide.Enemy
			|| result.UnblockedDamage <= 0
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		Flash([target]);
		_applyingShrinkRay = true;
		try
		{
			await PowerCmd.Apply<ShrinkPower>(target, DynamicVars["ShrinkPower"].BaseValue, Owner.Creature, cardSource);
		}
		finally
		{
			_applyingShrinkRay = false;
		}
	}
}

public sealed class SingularityAIRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		List<CardModel> powerPool = Owner.Character.CardPool
			.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
			.Where(static card => card.Type == CardType.Power)
			.ToList();
		if (powerPool.Count == 0)
		{
			return;
		}

		CardModel canonicalCard = HextechStableRandom.Pick(
			powerPool,
			(RunState)Owner.RunState,
			HextechStableRandom.CardKey,
			"singularity-ai-player-power",
			HextechStableRandom.PlayerKey(Owner),
			combatState.RoundNumber.ToString(),
			CountOwnedCardsDrawnFromHistory().ToString());
		CardModel card = combatState.CreateCard(canonicalCard, Owner);

		card.SetToFreeThisTurn();
		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}

public sealed class SlapRune : LimitedDebuffProcRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m)
	];

	protected override Task OnEnemyDebuffApplied(Creature target)
	{
		return PowerCmd.Apply<StrengthPower>(Owner!.Creature, DynamicVars.Strength.BaseValue, Owner!.Creature, null);
	}
}

public sealed class SlowCookRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("BurnPercent", 5m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner)
		{
			return;
		}

		int burnAmount = FloorToInt(player.Creature.MaxHp * (DynamicVars["BurnPercent"].BaseValue / 100m));
		if (burnAmount <= 0)
		{
			return;
		}

		HextechCombatState? combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = combatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		foreach (Creature enemy in enemies)
		{
			await PowerCmd.Apply<HextechBurnPower>(enemy, burnAmount, player.Creature, null);
		}
	}
}

public sealed class SnakebiteRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Snakebite>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || combatState.RoundNumber > 1)
		{
			return;
		}

		CardModel card = combatState.CreateCard<Snakebite>(Owner);
		card.SetToFreeThisCombat();
		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}

public sealed class SomethingFromNothingRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || cardPlay.Card.Owner != Owner || !cardPlay.Card.Keywords.Contains(CardKeyword.Ethereal))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}
}

public sealed class SonataRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
		new EnergyVar(1),
		new HealVar(1m),
		new BlockVar(2m, ValueProp.Unpowered)
	];

	public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		List<Player> players = combatState.Players
			.Where(static combatPlayer => combatPlayer.Creature.IsAlive)
			.ToList();
		if (players.Count == 0)
		{
			return;
		}

		Flash(players.Select(static combatPlayer => combatPlayer.Creature).ToArray());
		if (combatState.RoundNumber % 2 == 1)
		{
			foreach (Player combatPlayer in players)
			{
				await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, combatPlayer, fromHandDraw: false);
			}

			return;
		}

		foreach (Player combatPlayer in players)
		{
			await CreatureCmd.Heal(combatPlayer.Creature, DynamicVars.Heal.BaseValue);
			await CreatureCmd.GainBlock(combatPlayer.Creature, DynamicVars.Block, null);
		}
	}

	public override Task AfterEnergyResetLate(Player player)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| player.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| !ReferenceEquals(Owner.Creature.CombatState, combatState)
			|| combatState.RoundNumber % 2 != 1)
		{
			return Task.CompletedTask;
		}

		return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
	}
}

public sealed class SoulCallingRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Soul>()
	];

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		IEnumerable<Soul> souls = Soul.Create(Owner, DynamicVars.Cards.IntValue, combatState);
		await HextechCardGeneration.AddGeneratedCardsToCombat(
			souls,
			PileType.Discard,
			addedByPlayer: true,
			position: CardPilePosition.Top);
	}
}

public sealed class SoulEaterRune : HextechRelicBase
{
	private int _debuffsThisCombat;
	private int _hpGainedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedDebuffsThisCombat
	{
		get => _debuffsThisCombat;
		set => _debuffsThisCombat = Math.Max(0, value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedHpGainedThisCombat
	{
		get => _hpGainedThisCombat;
		set => _hpGainedThisCombat = Math.Max(0, value);
	}

	public override Task BeforeCombatStart()
	{
		_debuffsThisCombat = 0;
		_hpGainedThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_debuffsThisCombat = 0;
		_hpGainedThisCombat = 0;
		return Task.CompletedTask;
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (!TryGetOwnedEnemyDebuffTarget(power, amount, applier, out _))
		{
			return;
		}

		_debuffsThisCombat++;
		if (Owner == null || _hpGainedThisCombat >= 10 || _debuffsThisCombat % 3 != 0)
		{
			return;
		}

		_hpGainedThisCombat++;
		Flash();
		await CreatureCmd.GainMaxHp(Owner.Creature, 1m);
	}
}

public sealed class SpeedDemonRune : HextechRelicBase
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
			|| result.UnblockedDamage <= 0
			|| (!IsOwnerOrPet(dealer) && cardSource?.Owner != Owner))
		{
			return;
		}

		_triggeredThisTurn = true;
		UpdateTurnScopedStateIdentity();
		Flash([target]);
		await CardPileCmd.Draw(choiceContext, 2m, Owner);
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

public sealed class SpeedsterRune : HextechRelicBase
{
	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner)
		{
			return count;
		}

		return count + (player.PlayerCombatState?.MaxEnergy ?? 0) / 2;
	}
}

public sealed class StarlightSplendorRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StarsVar(2)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return Task.CompletedTask;
		}

		decimal stars = Owner.Creature.CombatState.RoundNumber * DynamicVars.Stars.BaseValue;
		if (stars <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainStars(stars, Owner);
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

public sealed class StatsOnStatsOnStatsRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ForgeCount", 6m)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await HextechForgeGrantHelper.ObtainRandomForges(Owner, DynamicVars["ForgeCount"].IntValue);
	}
}

public sealed class StatsOnStatsRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ForgeCount", 4m)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await HextechForgeGrantHelper.ObtainRandomForges(Owner, DynamicVars["ForgeCount"].IntValue);
	}
}

public sealed class StatsRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ForgeCount", 2m)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await HextechForgeGrantHelper.ObtainRandomForges(Owner, DynamicVars["ForgeCount"].IntValue);
	}
}
