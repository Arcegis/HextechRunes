using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static readonly Dictionary<CardModel, Stack<int>> ActivePlayEnergyValues = new();
	private static readonly Dictionary<CardModel, int> PendingManualPlayEnergyValues = new();

	internal static bool TryGetActivePlayEnergyValue(CardModel? card, out decimal energyValue)
	{
		energyValue = 0m;
		if (card == null)
		{
			return false;
		}

		if (!ActivePlayEnergyValues.TryGetValue(card, out Stack<int>? energyValues) || energyValues.Count == 0)
		{
			return false;
		}

		energyValue = energyValues.Peek();
		return true;
	}

	internal static decimal GetEnergyCostForCurrentCardPlay(CardModel card)
	{
		return TryGetActivePlayEnergyValue(card, out decimal energyValue)
			? energyValue
			: card.EnergyCost.GetAmountToSpend();
	}

	private static bool CardSpendResourcesPrefix(CardModel __instance, ref Task<ValueTuple<int, int>> __result)
	{
		PendingManualPlayEnergyValues[__instance] = __instance.EnergyCost.GetAmountToSpend();
		if (!StardustUpgradeRune.ShouldPreserveStars(__instance))
		{
			return true;
		}

		__result = SpendResourcesPreservingStars(__instance);
		return false;
	}

	private static async Task<ValueTuple<int, int>> SpendResourcesPreservingStars(CardModel card)
	{
		var owner = card.Owner!;
		var combatState = card.CombatState!;
		var playerCombatState = owner.PlayerCombatState!;
		int energy = playerCombatState.Energy;
		int energyToSpend = card.EnergyCost.GetAmountToSpend();
		int starsToSpend = Math.Max(0, card.GetStarCostWithModifiers());
		if (energyToSpend > energy && Hook.ShouldPayExcessEnergyCostWithStars(combatState, owner))
		{
			starsToSpend += (energyToSpend - energy) * 2;
			energyToSpend = energy;
		}

		if (card.EnergyCost.CostsX)
		{
			card.EnergyCost.CapturedXValue = energyToSpend;
		}

		if (energyToSpend > 0)
		{
			CombatManager.Instance.History.EnergySpent(combatState, energyToSpend, owner);
			playerCombatState.LoseEnergy(Math.Max(0, energyToSpend));
		}

		await Hook.AfterEnergySpent(combatState, card, energyToSpend);
		card.LastStarsSpent = starsToSpend;
		owner.GetRelic<StardustUpgradeRune>()?.Flash();
		return new ValueTuple<int, int>(energyToSpend, starsToSpend);
	}

	private static void CardOnPlayWrapperPrefix(CardModel __instance, ResourceInfo resources)
	{
		int energyValue = resources.EnergyValue;
		if (PendingManualPlayEnergyValues.Remove(__instance, out int pendingEnergyValue))
		{
			energyValue = pendingEnergyValue;
		}

		PushActivePlayEnergyValue(__instance, energyValue);
	}

	private static void CardOnPlayWrapperPostfix(CardModel __instance, ref Task __result)
	{
		__result = PopActivePlayEnergyValueWhenDone(__instance, __result);
	}

	private static void PushActivePlayEnergyValue(CardModel card, int energyValue)
	{
		if (!ActivePlayEnergyValues.TryGetValue(card, out Stack<int>? energyValues))
		{
			energyValues = new Stack<int>();
			ActivePlayEnergyValues[card] = energyValues;
		}

		energyValues.Push(Math.Max(0, energyValue));
	}

	private static async Task PopActivePlayEnergyValueWhenDone(CardModel card, Task task)
	{
		try
		{
			await task;
		}
		finally
		{
			PopActivePlayEnergyValue(card);
		}
	}

	private static void PopActivePlayEnergyValue(CardModel card)
	{
		if (!ActivePlayEnergyValues.TryGetValue(card, out Stack<int>? energyValues) || energyValues.Count == 0)
		{
			return;
		}

		energyValues.Pop();
		if (energyValues.Count == 0)
		{
			ActivePlayEnergyValues.Remove(card);
		}
	}
}
