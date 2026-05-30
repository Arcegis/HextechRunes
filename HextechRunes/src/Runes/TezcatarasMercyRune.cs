using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class TezcatarasMercyRune : HextechRelicBase, IHextechSharedCombatVictoryRune
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

	public override Task AfterCombatVictory(CombatRoom room)
	{
		return IsNetworkMultiplayer()
			? Task.CompletedTask
			: ApplySharedCombatVictory(room);
	}

	public async Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
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
		if (Owner == null)
		{
			return;
		}

		RelicModel? relic = Owner.Relics.FirstOrDefault(static relic => relic.IsWax && !relic.IsMelted);
		if (relic != null)
		{
			await RelicCmd.Melt(relic);
		}
	}
}
