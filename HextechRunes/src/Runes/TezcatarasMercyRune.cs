using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public sealed class TezcatarasMercyRune : HextechSharedCombatVictoryRuneBase
{
	private int _combatCounter;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCombatCounter
	{
		get => _combatCounter;
		set => _combatCounter = Math.Max(0, value);
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Relics", 1m),
		new DynamicVar("CombatInterval", 3m)
	];

	public override async Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner.Creature.IsDead)
		{
			return;
		}

		RelicModel waxRelic = HextechAncientRelicHelper.CreateRepeatableWaxRelic(Owner, "tezcataras-mercy-wax-relic", _combatCounter);
		SaveManager.Instance.MarkRelicAsSeen(waxRelic);
		room.AddExtraReward(Owner, new HextechWaxRelicReward(waxRelic, Owner));

		SavedCombatCounter++;
		if (_combatCounter >= DynamicVars["CombatInterval"].IntValue)
		{
			SavedCombatCounter = 0;
			await MeltLeftmostWaxRelic();
		}

		Flash(Array.Empty<Creature>());
	}

	private async Task MeltLeftmostWaxRelic()
	{
		RelicModel? relic = Owner.Relics.FirstOrDefault(static relic => relic.IsWax && !relic.IsMelted);
		if (relic != null)
		{
			await RelicCmd.Melt(relic);
		}
	}
}
