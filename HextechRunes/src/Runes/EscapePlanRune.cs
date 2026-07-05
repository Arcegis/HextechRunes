using MegaCrit.Sts2.Core.Entities.Relics;

namespace HextechRunes;

public sealed class EscapePlanRune : HextechRelicBase
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
		new DynamicVar("ThresholdPercent", 50m),
		new DynamicVar("BlockPercent", 60m)
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
			|| _pendingTrigger
			|| target.CurrentHp >= target.MaxHp * 0.5m)
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
		int blockAmount = FloorToInt(player.Creature.MaxHp * 0.6m);
		Flash();
		if (blockAmount > 0)
		{
			await CreatureCmd.GainBlock(player.Creature, blockAmount, ValueProp.Unpowered, null);
		}

		await PowerCmd.Apply<ShrinkPower>(player.Creature, 1m, player.Creature, null);
	}
}
