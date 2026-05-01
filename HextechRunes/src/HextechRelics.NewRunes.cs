using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
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

		CardModel card = Owner.RunState.Rng.CombatCardGeneration.NextInt(3) switch
		{
			0 => combatState.CreateCard<MinionStrike>(Owner),
			1 => combatState.CreateCard<MinionDiveBomb>(Owner),
			_ => combatState.CreateCard<MinionSacrifice>(Owner)
		};

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
	private static readonly FieldInfo? AttackCommandSingleTargetField = typeof(AttackCommand).GetField("_singleTarget", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo? AttackCommandCombatStateField = typeof(AttackCommand).GetField("_combatState", BindingFlags.Instance | BindingFlags.NonPublic);

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
