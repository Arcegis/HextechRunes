using static HextechRunes.HextechHookReflection;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HextechRunes;

/// <summary>
/// 升级：自动化——触发自动化效果(每抽 10 张的能量结算)时,额外抽 2 张牌。
/// 原版触发逻辑原样复刻(prefix 替换),仅在触发点追加抽牌;计数器读写走反射
/// (AutomationPower.Data.cardsLeft 为私有嵌套类型)。
/// </summary>
public sealed class AutomationUpgradeRune : CardUpgradeRuneBase<Automation>
{
	private const int TriggerThreshold = 10;

	// 私有访问均对照原版 0.107.1~0.111.0：AutomationPower 私有嵌套类 Data 的公有字段 cardsLeft，
	// PowerModel 的 protected GetInternalData<T>()、InvokeDisplayAmountChanged()、Flash()。
	// 任一缺失时进启动摘要，ShouldUseUpgradedDraw 返回 false，自动化回落原版(不追加抽牌)。
	private static readonly Type? AutomationDataType = TryGetNestedType(typeof(AutomationPower), "Data");
	private static readonly MethodInfo? PowerGetInternalDataMethod = AutomationDataType == null
		? null
		: TryGetMethod(typeof(PowerModel), "GetInternalData", BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)
			?.MakeGenericMethod(AutomationDataType);
	private static readonly FieldInfo? AutomationCardsLeftField = AutomationDataType == null
		? null
		: TryGetField(AutomationDataType, "cardsLeft", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly MethodInfo? PowerInvokeDisplayAmountChangedMethod = TryGetMethod(
		typeof(PowerModel),
		"InvokeDisplayAmountChanged",
		BindingFlags.Instance | BindingFlags.NonPublic,
		Type.EmptyTypes);
	private static readonly MethodInfo? PowerFlashMethod = TryGetMethod(
		typeof(PowerModel),
		"Flash",
		BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
		Type.EmptyTypes);

	private static bool IsReflectionAvailable => PowerGetInternalDataMethod != null
		&& AutomationCardsLeftField != null
		&& PowerInvokeDisplayAmountChangedMethod != null
		&& PowerFlashMethod != null;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return true;
	}

	internal static bool ShouldUseUpgradedDraw(AutomationPower power, CardModel card)
	{
		Player? owner = power.Owner?.Player;
		return IsReflectionAvailable
			&& owner != null
			&& card.Owner == owner
			&& owner.GetRelic<AutomationUpgradeRune>() != null;
	}

	internal static async Task AfterCardDrawnUpgraded(PlayerChoiceContext choiceContext, AutomationPower power, CardModel card, bool fromHandDraw)
	{
		Player? owner = power.Owner.Player;
		if (owner == null
			|| PowerGetInternalDataMethod == null
			|| AutomationCardsLeftField == null
			|| PowerInvokeDisplayAmountChangedMethod == null
			|| PowerFlashMethod == null
			|| PowerGetInternalDataMethod.Invoke(power, null) is not { } data
			|| AutomationCardsLeftField.GetValue(data) is not int storedCardsLeft)
		{
			return;
		}

		int cardsLeft = Math.Max(0, storedCardsLeft - 1);
		AutomationCardsLeftField.SetValue(data, cardsLeft);
		PowerInvokeDisplayAmountChangedMethod.Invoke(power, null);
		if (cardsLeft > 0)
		{
			return;
		}

		// 原版触发:回能量并重置计数。
		PowerFlashMethod.Invoke(power, null);
		await PlayerCmd.GainEnergy(power.Amount, owner);
		AutomationCardsLeftField.SetValue(data, TriggerThreshold);
		PowerInvokeDisplayAmountChangedMethod.Invoke(power, null);

		// 符文追加:额外抽 2 张。
		if (owner.Creature.IsDead || owner.Creature.CombatState == null)
		{
			return;
		}

		AutomationUpgradeRune? rune = owner.GetRelic<AutomationUpgradeRune>();
		rune?.Flash();
		await CardPileCmd.Draw(choiceContext, rune?.DynamicVars.Cards.BaseValue ?? 2m, owner, fromHandDraw: false);
	}

	[HarmonyPatch(typeof(AutomationPower), nameof(AutomationPower.AfterCardDrawn), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool))]
	[HextechPatch("rune.automation", "升级自动化", Rune = typeof(AutomationUpgradeRune))]
	private static class AutomationPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(AutomationPower __instance, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw, ref Task __result)
		{
			if (!AutomationUpgradeRune.ShouldUseUpgradedDraw(__instance, card))
			{
				return true;
			}

			__result = AutomationUpgradeRune.AfterCardDrawnUpgraded(choiceContext, __instance, card, fromHandDraw);
			return false;
		}
	}
}
