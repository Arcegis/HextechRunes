using MegaCrit.Sts2.Core.Entities.Relics;

namespace HextechRunes;

public sealed class DawnbringersResolveRune : HextechRelicBase
{
	private bool _triggeredThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisCombat
	{
		get => _triggeredThisCombat;
		set => _triggeredThisCombat = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ThresholdPercent", 50m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RegenPower>()
	];

	public override Task BeforeCombatStart()
	{
		_triggeredThisCombat = false;
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_triggeredThisCombat = false;
		Status = RelicStatus.Normal;
		return Task.CompletedTask;
	}

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null
			|| target != Owner.Creature
			|| result.UnblockedDamage <= 0
			|| _triggeredThisCombat
			|| target.CurrentHp >= target.MaxHp * 0.5m)
		{
			return;
		}

		_triggeredThisCombat = true;
		Status = RelicStatus.Active;
		Flash();
		int regen = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * 0.15m));
		await PowerCmd.Apply<RegenPower>(Owner.Creature, regen, Owner.Creature, null);
	}
}
