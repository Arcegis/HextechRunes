using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class AutomationUpgradeRune : CardUpgradeRuneBase<Automation>
{
	private static readonly Type AutomationDataType = typeof(AutomationPower).GetNestedType("Data", BindingFlags.NonPublic)
		?? throw new InvalidOperationException("AutomationPower.Data was not found.");
	private static readonly MethodInfo PowerGetInternalDataMethod = typeof(PowerModel)
		.GetMethod("GetInternalData", BindingFlags.Instance | BindingFlags.NonPublic)
		?.MakeGenericMethod(AutomationDataType)
		?? throw new InvalidOperationException("PowerModel.GetInternalData was not found.");
	private static readonly MethodInfo PowerInvokeDisplayAmountChangedMethod = typeof(PowerModel)
		.GetMethod("InvokeDisplayAmountChanged", BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException("PowerModel.InvokeDisplayAmountChanged was not found.");
	private static readonly FieldInfo AutomationCardsLeftField = AutomationDataType.GetField("cardsLeft", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		?? throw new InvalidOperationException("AutomationPower.Data.cardsLeft was not found.");

	protected override bool IsAvailableForCharacter(Player player)
	{
		return true;
	}

	internal static bool ShouldUseUpgradedDraw(AutomationPower power, CardModel card)
	{
		Player? owner = power.Owner?.Player;
		return owner != null
			&& card.Owner == owner
			&& owner.GetRelic<AutomationUpgradeRune>() != null;
	}

	internal static async Task AfterCardDrawnUpgraded(PlayerChoiceContext choiceContext, AutomationPower power, CardModel card, bool fromHandDraw)
	{
		Player? owner = power.Owner.Player;
		if (owner == null)
		{
			return;
		}

		object data = PowerGetInternalDataMethod.Invoke(power, null)!;
		int cardsLeft = Math.Max(0, (int)AutomationCardsLeftField.GetValue(data)! - 1);
		AutomationCardsLeftField.SetValue(data, cardsLeft);
		PowerInvokeDisplayAmountChangedMethod.Invoke(power, null);
		if (cardsLeft > 0
			|| owner.Creature.IsDead
			|| owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		AutomationCardsLeftField.SetValue(data, 10);
		PowerInvokeDisplayAmountChangedMethod.Invoke(power, null);

		owner.GetRelic<AutomationUpgradeRune>()?.Flash();
		CardModel fuel = combatState.CreateCard<Fuel>(owner);
		await HextechCardGeneration.AddGeneratedCardToCombat(fuel, PileType.Hand, addedByPlayer: true);
	}
}
