using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

public abstract class DragonSoulCardBase : CardModel
{
	public override CardPoolModel Pool => IsMutable && Owner != null
		? Owner.Character.CardPool
		: ModelDb.CardPool<TokenCardPool>();

	public override CardPoolModel VisualCardPool => Pool;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	protected DragonSoulCardBase(int cost)
		: base(cost, CardType.Power, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: true)
	{
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}

public sealed class OceanDragonSoulCard : DragonSoulCardBase
{
	public override string PortraitPath => HextechAssets.OceanDragonSoulCardPortraitPath;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new HealVar(3m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechOceanDragonSoulPower>()
	];

	public OceanDragonSoulCard()
		: base(0)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return PowerCmd.Apply<HextechOceanDragonSoulPower>(Owner.Creature, DynamicVars.Heal.BaseValue, Owner.Creature, this);
	}
}

public sealed class InfernalDragonSoulCard : DragonSoulCardBase
{
	public override string PortraitPath => HextechAssets.InfernalDragonSoulCardPortraitPath;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("BurnPower", 6m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechInfernalDragonSoulPower>(),
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	public InfernalDragonSoulCard()
		: base(0)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return PowerCmd.Apply<HextechInfernalDragonSoulPower>(Owner.Creature, DynamicVars["BurnPower"].BaseValue, Owner.Creature, this);
	}
}

public sealed class HextechDragonSoulCard : DragonSoulCardBase
{
	public override string PortraitPath => HextechAssets.HextechDragonSoulCardPortraitPath;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechDragonSoulPower>()
	];

	public HextechDragonSoulCard()
		: base(0)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return PowerCmd.Apply<HextechDragonSoulPower>(Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
	}
}

public sealed class MountainDragonSoulCard : DragonSoulCardBase
{
	public override string PortraitPath => HextechAssets.MountainDragonSoulCardPortraitPath;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<PlatingPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechMountainDragonSoulPower>(),
		HoverTipFactory.FromPower<PlatingPower>()
	];

	public MountainDragonSoulCard()
		: base(0)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return PowerCmd.Apply<HextechMountainDragonSoulPower>(Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
	}
}

public sealed class ChemtechDragonSoulCard : DragonSoulCardBase
{
	public override string PortraitPath => HextechAssets.ChemtechDragonSoulCardPortraitPath;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("PotionCount", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechChemtechDragonSoulPower>()
	];

	public ChemtechDragonSoulCard()
		: base(1)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return PowerCmd.Apply<HextechChemtechDragonSoulPower>(Owner.Creature, DynamicVars["PotionCount"].BaseValue, Owner.Creature, this);
	}
}

public sealed class CloudDragonSoulCard : DragonSoulCardBase
{
	public override string PortraitPath => HextechAssets.CloudDragonSoulCardPortraitPath;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechCloudDragonSoulPower>()
	];

	public CloudDragonSoulCard()
		: base(0)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return PowerCmd.Apply<HextechCloudDragonSoulPower>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
	}
}

public sealed class HextechOceanDragonSoulPower : HextechPowerBase
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != Owner.Side || Amount <= 0m || !Owner.IsAlive)
		{
			return;
		}

		Flash();
		await CreatureCmd.Heal(Owner, Amount);
	}
}

public sealed class HextechInfernalDragonSoulPower : HextechPowerBase
{
	private bool _triggeredThisTurn;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (side == Owner.Side)
		{
			_triggeredThisTurn = false;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (HasTriggeredThisTurn()
			|| Amount <= 0m
			|| !Owner.IsAlive
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| cardPlay.Card.Owner?.Creature != Owner
			|| cardPlay.Card.Type != CardType.Attack)
		{
			return;
		}

		List<Creature> targets = GetTargets(cardPlay).ToList();
		if (targets.Count == 0)
		{
			return;
		}

		if (!TryConsumeTriggerThisTurn())
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<HextechBurnPower>(targets, Amount, Owner, cardPlay.Card);
	}

	private bool HasTriggeredThisTurn()
	{
		return TryGetNetworkTriggerCount(out int count) ? count > 0 : _triggeredThisTurn;
	}

	private bool TryConsumeTriggerThisTurn()
	{
		if (Owner.Player is Player player
			&& HextechPlayerContextHelper.IsNetworkMultiplayerRun()
			&& CombatManager.Instance?.IsInProgress == true
			&& player.RunState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is HextechMayhemModifier modifier)
		{
			return modifier.TryConsumePlayerRuneProcThisTurn(player, nameof(HextechInfernalDragonSoulPower), 1);
		}

		if (_triggeredThisTurn)
		{
			return false;
		}

		_triggeredThisTurn = true;
		return true;
	}

	private bool TryGetNetworkTriggerCount(out int count)
	{
		count = 0;
		if (Owner.Player is not Player player
			|| !HextechPlayerContextHelper.IsNetworkMultiplayerRun()
			|| CombatManager.Instance?.IsInProgress != true
			|| player.RunState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is not HextechMayhemModifier modifier)
		{
			return false;
		}

		count = modifier.GetPlayerRuneProcsThisTurn(player, nameof(HextechInfernalDragonSoulPower));
		return true;
	}

	private IEnumerable<Creature> GetTargets(CardPlay cardPlay)
	{
		if (cardPlay.Target is { Side: CombatSide.Enemy, IsAlive: true } target)
		{
			yield return target;
			yield break;
		}

		if (cardPlay.Card.TargetType != TargetType.AllEnemies || Owner.CombatState == null)
		{
			yield break;
		}

		foreach (Creature enemy in Owner.CombatState.HittableEnemies)
		{
			yield return enemy;
		}
	}
}

public sealed class HextechDragonSoulPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Task AfterEnergyResetLate(Player player)
	{
		if (player.Creature != Owner || Amount <= 0m || !Owner.IsAlive)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainEnergy(Amount, player);
	}
}

public sealed class HextechMountainDragonSoulPower : HextechPowerBase
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (side != Owner.Side || Amount <= 0m || !Owner.IsAlive)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<PlatingPower>(Owner, Amount, Owner, null);
	}
}

public sealed class HextechChemtechDragonSoulPower : HextechPowerBase
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (side != Owner.Side || Amount <= 0m || !Owner.IsAlive || Owner.Player is not Player player)
		{
			return;
		}

		List<PotionModel> candidates = PotionFactory.GetPotionOptions(player, Array.Empty<PotionModel>()).ToList();
		if (candidates.Count == 0)
		{
			return;
		}

			Flash();
			for (int i = 0; i < (int)Amount; i++)
			{
				PotionModel potion = HextechStableRandom.Pick(
					candidates,
					(RunState)player.RunState,
					HextechStableRandom.PotionKey,
					"chemtech-dragon-soul-potion",
					HextechStableRandom.PlayerKey(player),
					combatState.RoundNumber.ToString(),
					i.ToString()).ToMutable();
				await PotionCmd.TryToProcure(potion, player);
			}
	}
}

public sealed class HextechCloudDragonSoulPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player.Creature == Owner && Owner.IsAlive ? count + Amount : count;
	}
}
