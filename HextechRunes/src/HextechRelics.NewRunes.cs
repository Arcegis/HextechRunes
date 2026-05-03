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

public sealed class ForbiddenGrimoireRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ForbiddenGrimoirePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ForbiddenGrimoirePower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ForbiddenGrimoirePower>(Owner.Creature, DynamicVars["ForbiddenGrimoirePower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class OneLaneBridgeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ImbalancedPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ImbalancedPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		await PowerCmd.Apply<ImbalancedPower>(enemies, DynamicVars["ImbalancedPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class OrbSymbiosisRune : HextechRelicBase
{
	private bool _duplicatingOrb;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbCount", 1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
	{
		if (_duplicatingOrb || player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		_duplicatingOrb = true;
		try
		{
			for (int i = 0; i < DynamicVars["OrbCount"].IntValue; i++)
			{
				OrbModel duplicate = ModelDb.GetById<OrbModel>(orb.Id).ToMutable();
				await OrbCmd.Channel(choiceContext, duplicate, Owner);
			}
		}
		finally
		{
			_duplicatingOrb = false;
		}
	}
}

public sealed class OldIdolRune : HextechRelicBase
{
	private bool _secondTurnStrengthGranted;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedSecondTurnStrengthGranted
	{
		get => _secondTurnStrengthGranted;
		set => _secondTurnStrengthGranted = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(10m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override Task BeforeCombatStart()
	{
		_secondTurnStrengthGranted = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_secondTurnStrengthGranted = false;
		return Task.CompletedTask;
	}

	public override decimal ModifyHandDrawLate(Player player, decimal count)
	{
		return player == Owner && player.Creature.CombatState?.RoundNumber == 1 ? 0m : count;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || _secondTurnStrengthGranted || combatState.RoundNumber != 2)
		{
			return;
		}

		_secondTurnStrengthGranted = true;
		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
	}
}

public sealed class MonarchsGazeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<MonarchsGazePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<MonarchsGazePower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<MonarchsGazePower>(Owner.Creature, DynamicVars["MonarchsGazePower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class HardBonesRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<CalcifyPower>(8m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<CalcifyPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || !IsNecrobinderPlayer(Owner))
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<CalcifyPower>(Owner.Creature, DynamicVars["CalcifyPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class SendThemInRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<MinionStrike>(),
		HoverTipFactory.FromCard<MinionDiveBomb>(),
		HoverTipFactory.FromCard<MinionSacrifice>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || !IsRegentPlayer(player))
		{
			return;
		}

		CardModel card = HextechStableRandom.CreateMinionCard(combatState, Owner, "send-them-in", combatState.RoundNumber);

		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}

public sealed class SwordsmanshipRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ParryPower>(12m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ParryPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || !IsRegentOwner)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ParryPower>(Owner.Creature, DynamicVars["ParryPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class EasyDoesItRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<MayhemPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<MayhemPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<MayhemPower>(Owner.Creature, DynamicVars["MayhemPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class SweepingBladeRune : HextechRelicBase
{
	private static readonly FieldInfo? AttackCommandSingleTargetField = TryGetField(typeof(AttackCommand), "_singleTarget");
	private static readonly FieldInfo? AttackCommandCombatStateField = TryGetField(typeof(AttackCommand), "_combatState");

	public override Task BeforeAttack(AttackCommand command)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| command.Attacker != Owner.Creature
			|| command.ModelSource is not CardModel card
			|| !IsOwnedAttack(card)
			|| !card.IsBasicStrikeOrDefend
			|| !command.IsSingleTargeted
			|| Owner.Creature.CombatState == null)
		{
			return Task.CompletedTask;
		}

		Flash();
		RetargetToAllOpponents(command, Owner.Creature.CombatState);
		return Task.CompletedTask;
	}

	private static void RetargetToAllOpponents(AttackCommand command, object combatState)
	{
		if (AttackCommandSingleTargetField == null || AttackCommandCombatStateField == null)
		{
			return;
		}

		AttackCommandSingleTargetField.SetValue(command, null);
		AttackCommandCombatStateField.SetValue(command, combatState);
	}
}

public sealed class AdvanceToRetreatRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<VulnerablePower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| target.Side != CombatSide.Enemy
			|| result.TotalDamage <= 0
			|| !IsOwnedAttack(cardSource)
			|| target.GetPowerAmount<VulnerablePower>() <= 0m)
		{
			return;
		}

		Flash([target]);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
	}
}

public sealed class NearDeathFeastRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CurrentHpLossPercent", 10m),
		new DynamicVar("StrengthPerHpLost", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || Owner.Creature.CurrentHp <= 1)
		{
			return;
		}

		decimal loss = Math.Floor(Owner.Creature.CurrentHp * DynamicVars["CurrentHpLossPercent"].BaseValue / 100m);
		loss = Math.Min(loss, Owner.Creature.CurrentHp - 1m);
		if (loss <= 0m)
		{
			return;
		}

		decimal strength = Math.Floor(loss * DynamicVars["StrengthPerHpLost"].BaseValue);
		if (strength <= 0m)
		{
			return;
		}

		Flash();
		await CreatureCmd.SetCurrentHp(Owner.Creature, Owner.Creature.CurrentHp - loss);
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, strength, Owner.Creature, null);
	}
}

public sealed class ChainInSleeveRune : HextechRelicBase
{
	private const int ShivsNeeded = 3;

	private int _shivsPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedShivsPlayedThisCombat
	{
		get => GetShivsPlayedThisCombat() % ShivsNeeded;
		set
		{
			_shivsPlayedThisCombat = Math.Max(0, value) % ShivsNeeded;
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

			int remainder = GetShivsPlayedThisCombat() % ShivsNeeded;
			return remainder == 0 ? ShivsNeeded : ShivsNeeded - remainder;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ShivsNeeded", ShivsNeeded),
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Shiv>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
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

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsCountedShivPlay(cardPlay))
		{
			return;
		}

		if (ShouldUseNetworkCombatHistory())
		{
			await ResolveShivProgressFromHistory();
			return;
		}

		_shivsPlayedThisCombat++;
		await ResolveShivRewards(previousShivsPlayed: _shivsPlayedThisCombat - 1, currentShivsPlayed: _shivsPlayedThisCombat);
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (ShouldUseNetworkCombatHistory() && IsCountedShivPlay(cardPlay))
		{
			await ResolveShivProgressFromHistory();
		}
	}

	private async Task ResolveShivProgressFromHistory()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int shivsPlayed = CountOwnedShivCardsPlayedFromHistory();
		int previousShivsPlayed = _shivsPlayedThisCombat;
		if (shivsPlayed <= previousShivsPlayed)
		{
			return;
		}

		_shivsPlayedThisCombat = shivsPlayed;
		await ResolveShivRewards(previousShivsPlayed, shivsPlayed);
	}

	private async Task ResolveShivRewards(int previousShivsPlayed, int currentShivsPlayed)
	{
		InvokeDisplayAmountChanged();
		int rewards = currentShivsPlayed / ShivsNeeded - previousShivsPlayed / ShivsNeeded;
		if (rewards <= 0 || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await AddCardCopiesToCombatHand<Shiv>(rewards * DynamicVars.Cards.IntValue);
	}

	private bool IsCountedShivPlay(CardPlay cardPlay)
	{
		return cardPlay.IsFirstInSeries
			&& !cardPlay.IsAutoPlay
			&& cardPlay.Card.Owner == Owner
			&& cardPlay.Card.Tags.Contains(CardTag.Shiv);
	}

	private int CountOwnedShivCardsPlayedFromHistory()
	{
		if (Owner == null)
		{
			return 0;
		}

		ulong ownerId = Owner.NetId;
		return CombatManager.Instance.History.Entries
			.OfType<CardPlayFinishedEntry>()
			.Count(entry =>
				entry.CardPlay.IsFirstInSeries
				&& !entry.CardPlay.IsAutoPlay
				&& entry.CardPlay.Card.Owner?.NetId == ownerId
				&& entry.CardPlay.Card.Tags.Contains(CardTag.Shiv));
	}

	private int GetShivsPlayedThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedShivCardsPlayedFromHistory()
			: _shivsPlayedThisCombat;
	}

	private void ResetCounter()
	{
		_shivsPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}

public sealed class RoyalCommandRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ForgeVar("ForgeAmount", 3)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterStarsGained(int amount, Player gainer)
	{
		return HandleStarsChanged(amount, gainer);
	}

	public override Task AfterStarsSpent(int amount, Player spender)
	{
		return HandleStarsChanged(amount, spender);
	}

	private Task HandleStarsChanged(int amount, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return ForgeCmd.Forge(DynamicVars["ForgeAmount"].BaseValue, Owner, this);
	}
}

public sealed class RoyalTrialRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<SovereignBlade>(),
		HoverTipFactory.FromCard<MinionStrike>(),
		HoverTipFactory.FromCard<MinionDiveBomb>(),
		HoverTipFactory.FromCard<MinionSacrifice>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not SovereignBlade
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		List<CardModel> cards = new(DynamicVars.Cards.IntValue);
		for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
			cards.Add(CreateRandomMinionCard(combatState, i));
		}

		Flash();
		await HextechCardGeneration.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
	}

	private CardModel CreateRandomMinionCard(HextechCombatState combatState, int ordinal)
	{
		return HextechStableRandom.CreateMinionCard(
			combatState,
			Owner!,
			"royal-trial",
			CombatManager.Instance.History.Entries.Count() + ordinal);
	}
}

public sealed class UnsealedThroneRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterStarsGained(int amount, Player gainer)
	{
		return HandleStarsChanged(amount, gainer);
	}

	public override Task AfterStarsSpent(int amount, Player spender)
	{
		return HandleStarsChanged(amount, spender);
	}

	private Task HandleStarsChanged(int amount, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}
}

public sealed class CoreOverloadRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FocusPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Evoke),
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
	{
		if (Owner == null || Owner.Creature.IsDead || orb.Owner != Owner || !IsDefectOwner)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class BoneGuardRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("BlockMultiplier", 0.5m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
	{
		if (summoner != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0m)
		{
			return Task.CompletedTask;
		}

		decimal block = Math.Floor(amount * DynamicVars["BlockMultiplier"].BaseValue);
		if (block <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Unpowered, null);
	}
}

public sealed class PlasterRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new SummonVar(1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
	{
		if (summoner != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| amount <= 0m
			|| !Owner.IsOstyAlive
			|| Owner.Osty == null)
		{
			return Task.CompletedTask;
		}

		Flash([Owner.Osty]);
		return CreatureCmd.Heal(Owner.Osty, amount);
	}
}
