using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class ShoulderVakuRune : HextechRelicBase
{
	private int _lastControlledRound;
	private bool _controllingTurn;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(2),
		new CardsVar(2),
		new DynamicVar("HealPercent", 10m)
	];

	public override Task BeforeCombatStart()
	{
		_lastControlledRound = 0;
		_controllingTurn = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_lastControlledRound = 0;
		_controllingTurn = false;
		return Task.CompletedTask;
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + DynamicVars.Cards.BaseValue : count;
	}

	public override Task AfterEnergyResetLate(Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (!IsOddOwnerTurn(player, out _))
		{
			return;
		}

		int heal = Math.Max(1, FloorToInt(Owner!.Creature.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m));
		Flash();
		await CreatureCmd.Heal(Owner.Creature, heal);
	}

	public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (!IsOddOwnerTurn(player, out int round) || _lastControlledRound == round || _controllingTurn)
		{
			return;
		}

		_lastControlledRound = round;
		_controllingTurn = true;
		try
		{
			Flash();
			using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
			{
				await AutoPlayPlayableHand(choiceContext, round);
			}
		}
		finally
		{
			_controllingTurn = false;
		}
	}

	private bool IsOddOwnerTurn(Player player, out int round)
	{
		round = 0;
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return false;
		}

		round = Owner.Creature.CombatState.RoundNumber;
		return round > 0 && round % 2 == 1;
	}

	private async Task AutoPlayPlayableHand(PlayerChoiceContext choiceContext, int round)
	{
		if (Owner == null || Owner.Creature.CombatState == null)
		{
			return;
		}

		List<CardModel> cards = PileType.Hand.GetPile(Owner).Cards
			.Where(card => card.Owner == Owner && card.Type is CardType.Attack or CardType.Skill or CardType.Power)
			.ToList();
		for (int i = 0; i < cards.Count; i++)
		{
			CardModel card = cards[i];
			if (card.Pile?.Type != PileType.Hand || !card.CanPlay())
			{
				continue;
			}

			Creature? target = PickRandomEnemy("shoulder-vaku", round, i);
			await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default);
		}
	}

	private Creature? PickRandomEnemy(string scope, int round, int ordinal)
	{
		if (Owner?.Creature.CombatState == null)
		{
			return null;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return null;
		}

		return enemies[HextechStableRandom.Index(
			(RunState)Owner.RunState,
			enemies.Count,
			scope,
			HextechStableRandom.PlayerKey(Owner),
			round.ToString(),
			ordinal.ToString(),
			CombatManager.Instance.History.Entries.Count().ToString())];
	}
}

public sealed class BlueCandleMedkitRune : HextechRelicBase
{
	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!CanAffect(card) || card.EnergyCost.CostsX)
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!CanAffect(card) || card.HasStarCostX)
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
	{
		return CanAffect(card) ? (PileType.Exhaust, position) : (pileType, position);
	}

	internal static bool AllowsPlaying(CardModel card)
	{
		return card.Owner?.GetRelic<BlueCandleMedkitRune>() is BlueCandleMedkitRune rune
			&& rune.CanAffect(card);
	}

	private bool CanAffect(CardModel card)
	{
		return Owner != null
			&& card.Owner == Owner
			&& card.Pile?.Type is PileType.Hand or PileType.Play
			&& card.Type is CardType.Status or CardType.Curse;
	}
}

public sealed class BigHandsRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Multiplier", 2m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
	{
		return summoner == Owner ? amount * DynamicVars["Multiplier"].BaseValue : amount;
	}
}

public sealed class DizzySpinningRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Dazed>()
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + DynamicVars.Cards.BaseValue : count;
	}

	public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		CardModel dazed = combatState.CreateCard<Dazed>(Owner);
		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(dazed, PileType.Draw, addedByPlayer: true, CardPilePosition.Random);
	}
}

public sealed class WarmupExerciseRune : HextechRelicBase
{
	private bool _dealtDamageThisTurn;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(2m),
		new PowerVar<DexterityPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>()
	];

	public override Task BeforeCombatStart()
	{
		_dealtDamageThisTurn = false;
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			_dealtDamageThisTurn = false;
		}

		return Task.CompletedTask;
	}

	public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner != null && target.Side == CombatSide.Enemy && result.UnblockedDamage > 0 && IsDamageFromOwner(dealer, cardSource))
		{
			_dealtDamageThisTurn = true;
		}

		return Task.CompletedTask;
	}

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || Owner.Creature.IsDead || side != Owner.Creature.Side || _dealtDamageThisTurn)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, null);
	}
}

public sealed class PrimitiveMadnessRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<RollingBoulder>(),
		HoverTipFactory.FromCard<GiantRock>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await AddCardCopiesToDeckOrHand<RollingBoulder>(1);

		List<CardModel> attackCards = Owner.Deck.Cards
			.Where(static card => card.Type == CardType.Attack && card.IsTransformable)
			.ToList();
		if (attackCards.Count == 0)
		{
			return;
		}

		IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckGeneric(
			Owner,
			new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, attackCards.Count)
			{
				Cancelable = true,
				RequireManualConfirmation = true
			},
			card => card.Type == CardType.Attack && card.IsTransformable);

		List<CardTransformation> transformations = selected
			.Select(card => new CardTransformation(card, Owner.RunState.CreateCard<GiantRock>(Owner)))
			.ToList();
		if (transformations.Count > 0)
		{
			await CardCmd.Transform(transformations, Owner.RunState.Rng.CombatCardSelection, CardPreviewStyle.GridLayout);
		}
	}
}

public sealed class PacifistRune : HextechRelicBase
{
	private readonly Dictionary<uint, decimal> _pendingDoomByTarget = new();

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("SustainMultiplier", 1.3m),
		new PowerVar<DoomPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<DoomPower>()
	];

	public decimal SustainMultiplier => DynamicVars["SustainMultiplier"].BaseValue;

	public override Task BeforeCombatStart()
	{
		_pendingDoomByTarget.Clear();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingDoomByTarget.Clear();
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		_pendingDoomByTarget.Clear();
		return Task.CompletedTask;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null || target?.Side != CombatSide.Enemy || amount <= 0m || !IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		if (target.CombatId is uint combatId)
		{
			_pendingDoomByTarget[combatId] = Math.Max(_pendingDoomByTarget.GetValueOrDefault(combatId), amount);
		}

		return 0m;
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null || target.Side != CombatSide.Enemy || target.CombatId is not uint combatId)
		{
			return;
		}

		if (!_pendingDoomByTarget.Remove(combatId, out decimal doom) || doom <= 0m)
		{
			return;
		}

		Flash([target]);
		await PowerCmd.Apply<DoomPower>(target, Math.Max(1, Math.Floor(doom)), Owner.Creature, cardSource);
	}
}

public sealed class JinlianBoxRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardModel> rareOptions = Owner.Character.CardPool
			.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
			.Where(static card => card.Rarity == CardRarity.Rare)
			.ToList();
		if (rareOptions.Count == 0)
		{
			return;
		}

		List<CardTransformation> transformations = Owner.Deck.Cards
			.Where(static card => card.IsTransformable)
			.Select(card => new CardTransformation(card, rareOptions))
			.ToList();
		if (transformations.Count == 0)
		{
			return;
		}

		Flash();
		await CardCmd.Transform(transformations, Owner.RunState.Rng.CombatCardSelection, CardPreviewStyle.GridLayout);
	}
}

public sealed class FleshAndBoneRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpLoss", 3m),
		new SummonVar(12m)
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
		if (Owner.Creature.CurrentHp > 1)
		{
			decimal nextHp = Math.Max(1m, Owner.Creature.CurrentHp - DynamicVars["HpLoss"].BaseValue);
			await CreatureCmd.SetCurrentHp(Owner.Creature, nextHp);
		}

		await OstyCmd.Summon(new BlockingPlayerChoiceContext(), Owner, DynamicVars.Summon.BaseValue, this);
	}
}

public sealed class ThoughtOverwriteRune : HextechRelicBase
{
	private bool _triggeredLastPlay;

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterObtained()
	{
		if (Owner == null || !IsNecrobinderPlayer(Owner))
		{
			return;
		}

		List<CardModel> selectable = Owner.Deck.Cards.ToList();
		if (selectable.Count == 0)
		{
			return;
		}

		IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckGeneric(
			Owner,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, selectable.Count)
			{
				Cancelable = true,
				RequireManualConfirmation = true
			});

		List<CardModel> selectedCards = selected.ToList();
		if (selectedCards.Count == 0)
		{
			return;
		}

		Flash();
		foreach (CardModel card in selectedCards)
		{
			CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
		}
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		_triggeredLastPlay = false;
		if (Owner == null || card.Owner != Owner || !card.Keywords.Contains(CardKeyword.Ethereal))
		{
			return playCount;
		}

		_triggeredLastPlay = true;
		return playCount + DynamicVars["Replays"].IntValue;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (_triggeredLastPlay)
		{
			Flash();
		}

		_triggeredLastPlay = false;
		return Task.CompletedTask;
	}
}

public sealed class MobileHomeRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	private static readonly Type[] RelicTypes =
	[
		typeof(MeatCleaver),
		typeof(Shovel),
		typeof(Girya),
		typeof(MiniatureTent)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		foreach (Type type in RelicTypes)
		{
			RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(type)).ToMutable();
			SaveManager.Instance.MarkRelicAsSeen(relic);
			await RelicCmd.Obtain(relic, Owner);
		}
	}
}

public sealed class MoreTheMerrierRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("PercentPerRelic", 2m)
	];

	public decimal SustainMultiplier => 1m + CountRelics() * DynamicVars["PercentPerRelic"].BaseValue / 100m;

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwner(dealer, cardSource) ? SustainMultiplier : 1m;
	}

	private int CountRelics()
	{
		return Owner?.Relics.Count ?? 0;
	}
}

public sealed class CrackTheEggRune : HextechRelicBase
{
	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || Owner.Creature.IsDead || side != Owner.Creature.Side || Owner.Creature.Block <= 0)
		{
			return;
		}

		Creature? target = PickRandomEnemy(combatState);
		if (target == null)
		{
			return;
		}

		int block = Owner.Creature.Block;
		Flash([target]);
		await CreatureCmd.Damage(choiceContext, target, block, ValueProp.Unpowered, Owner.Creature, null);
	}

	private Creature? PickRandomEnemy(HextechCombatState combatState)
	{
		if (Owner == null)
		{
			return null;
		}

		List<Creature> enemies = combatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return null;
		}

		return enemies[HextechStableRandom.Index(
			(RunState)Owner.RunState,
			enemies.Count,
			"crack-the-egg",
			HextechStableRandom.PlayerKey(Owner),
			combatState.RoundNumber.ToString(),
			CombatManager.Instance.History.Entries.Count().ToString())];
	}
}

public sealed class MirrorReflectionRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardPileAddResult> results = new();
		List<CardModel> cards = Owner.Deck.Cards
			.Where(static card => !card.IsBasicStrikeOrDefend && card.Type != CardType.Curse)
			.ToList();
		if (cards.Count == 0)
		{
			return;
		}

		Flash();
		foreach (CardModel card in cards)
		{
			CardModel copy = Owner.RunState.CloneCard(card);
			results.Add(await CardPileCmd.Add(copy, PileType.Deck));
			SaveManager.Instance.MarkCardAsSeen(copy);
		}

		CardCmd.PreviewCardPileAdd(results, 2f);
	}
}

public sealed class KakaRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RitualPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RitualPower>()
	];

	internal static bool BlocksAttack(CardModel card)
	{
		return card.Type == CardType.Attack
			&& card.Owner?.GetRelic<KakaRune>() != null
			&& card.Owner.Creature.CombatState?.RoundNumber == 1;
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState?.RoundNumber != 2)
		{
			return;
		}

		int act = Math.Max(1, Owner.RunState.CurrentActIndex + 1);
		Flash();
		await PowerCmd.Apply<RitualPower>(Owner.Creature, act, Owner.Creature, null);
	}
}

public sealed class BrutalForceRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new PowerVar<StrengthPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner && player.Creature.CombatState?.RoundNumber == 1
			? count + DynamicVars.Cards.BaseValue
			: count;
	}

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
	}
}

public sealed class QuantumComputingRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamagePercent", 10m),
		new DamageVar(30m, ValueProp.Unpowered),
		new DynamicVar("HealPercent", 10m)
	];

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return;
		}

		int round = Owner.Creature.CombatState.RoundNumber;
		if (round <= 0 || round % 2 != 0)
		{
			return;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		int totalDamage = 0;
		foreach (Creature enemy in enemies)
		{
			decimal damage = Math.Max(DynamicVars.Damage.BaseValue, Math.Floor(enemy.MaxHp * DynamicVars["DamagePercent"].BaseValue / 100m));
			IEnumerable<DamageResult> results = await CreatureCmd.Damage(choiceContext, enemy, damage, ValueProp.Unpowered, Owner.Creature, null);
			totalDamage += results.Sum(static result => result.UnblockedDamage);
		}

		int heal = FloorToInt(totalDamage * DynamicVars["HealPercent"].BaseValue / 100m);
		if (heal > 0 && !Owner.Creature.IsDead)
		{
			await CreatureCmd.Heal(Owner.Creature, heal);
		}
	}
}

public sealed class MindOverMatterRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		List<CardModel> pool = Owner.Character.CardPool
			.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
			.Where(static card => card.Rarity is not CardRarity.Basic and not CardRarity.Ancient && card.CanBeGeneratedInCombat)
			.ToList();
		if (pool.Count == 0)
		{
			return;
		}

		CardModel canonicalCard = HextechStableRandom.Pick(
			pool,
			(RunState)Owner.RunState,
			HextechStableRandom.CardKey,
			"mind-over-matter",
			HextechStableRandom.PlayerKey(Owner),
			combatState.RoundNumber.ToString(),
			CountOwnedCardsDrawnFromHistory().ToString());
		CardModel card = combatState.CreateCard(canonicalCard, Owner);
		card.AddKeyword(CardKeyword.Ethereal);
		card.SetToFreeThisTurn();

		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}

public sealed class BloodArmorRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<PlatingPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>()
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
			|| !IsIroncladPlayer(Owner)
			|| !HextechSts2Compat.IsPartOfPlayerTurn(Owner))
		{
			return Task.CompletedTask;
		}

		decimal amount = Math.Floor(-delta) * DynamicVars["PlatingPower"].BaseValue;
		if (amount <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<PlatingPower>(Owner.Creature, amount, Owner.Creature, null);
	}
}

public sealed class ScaredStiffRune : HextechRelicBase
{
	private bool _autoPlaying;

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (_autoPlaying || Owner == null || Owner.Creature.IsDead || side != Owner.Creature.Side || !IsIroncladPlayer(Owner))
		{
			return;
		}

		List<CardModel> attacks = PileType.Hand.GetPile(Owner).Cards
			.Where(card => card.Owner == Owner && card.Type == CardType.Attack)
			.ToList();
		if (attacks.Count == 0)
		{
			return;
		}

		_autoPlaying = true;
		try
		{
			Flash();
			for (int i = 0; i < attacks.Count; i++)
			{
				CardModel card = attacks[i];
				if (card.Pile?.Type != PileType.Hand)
				{
					continue;
				}

				card.ExhaustOnNextPlay = true;
				Creature? target = PickRandomEnemy(i);
				await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default);
			}
		}
		finally
		{
			_autoPlaying = false;
		}
	}

	private Creature? PickRandomEnemy(int ordinal)
	{
		if (Owner?.Creature.CombatState == null)
		{
			return null;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return null;
		}

		return enemies[HextechStableRandom.Index(
			(RunState)Owner.RunState,
			enemies.Count,
			"scared-stiff",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState.RoundNumber.ToString(),
			ordinal.ToString(),
			CombatManager.Instance.History.Entries.Count().ToString())];
	}
}

public sealed class DuffsVintageRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CostReduction", 1m)
	];

	public override bool ShouldFlush(Player player)
	{
		return player != Owner;
	}

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		List<CardModel> cards = PileType.Hand.GetPile(Owner).Cards
			.Where(static card => !card.EnergyCost.CostsX)
			.ToList();
		if (cards.Count == 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		foreach (CardModel card in cards)
		{
			int nextCost = Math.Max(0, card.EnergyCost.GetAmountToSpend() - DynamicVars["CostReduction"].IntValue);
			card.EnergyCost.SetThisCombat(nextCost, reduceOnly: true);
		}

		return Task.CompletedTask;
	}
}

public sealed class TanksShieldRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedAttack(cardPlay.Card) || Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
	}
}
