using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class RegretRune : HextechRelicBase
{
	private readonly HashSet<uint> _pendingEnemyRevives = [];
	private bool _pendingPlayerRevive;

	private int _damageBonusPercent;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedDamageBonusPercent
	{
		get => _damageBonusPercent;
		set
		{
			_damageBonusPercent = value;
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => !IsCanonical;

	public override int DisplayAmount => _damageBonusPercent;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("EnemyReviveChance", 30m),
		new DynamicVar("EnemyReviveHpPercent", 50m),
		new DynamicVar("DamageGainPercent", 5m),
		new DynamicVar("PlayerReviveChance", 50m),
		new DynamicVar("DamageLossPercent", 10m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return !IsNetworkMultiplayerRun();
	}

	public override Task BeforeCombatStart()
	{
		_pendingEnemyRevives.Clear();
		_pendingPlayerRevive = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingEnemyRevives.Clear();
		_pendingPlayerRevive = false;
		return Task.CompletedTask;
	}

	public override Task BeforeDeath(Creature creature)
	{
		if (Owner == null || IsNetworkMultiplayerRun())
		{
			return Task.CompletedTask;
		}

		if (creature == Owner.Creature)
		{
			if (!_pendingPlayerRevive && RollPercent(DynamicVars["PlayerReviveChance"].IntValue))
			{
				_pendingPlayerRevive = true;
			}

			return Task.CompletedTask;
		}

		if (creature.Side != CombatSide.Enemy
			|| creature.CombatId == null
			|| creature.HasPower<MinionPower>()
			|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(creature)
			|| !RollPercent(DynamicVars["EnemyReviveChance"].IntValue))
		{
			return Task.CompletedTask;
		}

		_pendingEnemyRevives.Add(creature.CombatId.Value);
		return Task.CompletedTask;
	}

	public override bool ShouldDie(Creature creature)
	{
		if (Owner == null || IsNetworkMultiplayerRun())
		{
			return true;
		}

		if (creature == Owner.Creature)
		{
			return !_pendingPlayerRevive;
		}

		return creature.CombatId == null || !_pendingEnemyRevives.Contains(creature.CombatId.Value);
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (Owner == null || !wasRemovalPrevented || IsNetworkMultiplayerRun())
		{
			return;
		}

		if (target == Owner.Creature && _pendingPlayerRevive)
		{
			_pendingPlayerRevive = false;
			SavedDamageBonusPercent -= DynamicVars["DamageLossPercent"].IntValue;
			Flash([Owner.Creature]);
			await CreatureCmd.SetCurrentHp(Owner.Creature, Owner.Creature.MaxHp);
			return;
		}

		if (target.CombatId == null || !_pendingEnemyRevives.Remove(target.CombatId.Value))
		{
			return;
		}

		int reviveHp = Math.Max(1, FloorToInt(target.MaxHp * DynamicVars["EnemyReviveHpPercent"].BaseValue / 100m));
		SavedDamageBonusPercent += DynamicVars["DamageGainPercent"].IntValue;
		Flash([target]);
		await CreatureCmd.SetCurrentHp(target, reviveHp);
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (_damageBonusPercent == 0m || !IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		return Math.Max(0.1m, 1m + _damageBonusPercent / 100m);
	}

	private bool RollPercent(int chance)
	{
		chance = Math.Clamp(chance, 0, 100);
		return chance > 0 && Owner?.RunState.Rng.Niche.NextInt(100) < chance;
	}
}
