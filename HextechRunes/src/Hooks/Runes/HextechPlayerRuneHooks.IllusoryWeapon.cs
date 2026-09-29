using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Relics;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechPlayerRuneHooks
{
	internal const string FinisherCalculatedHitsKey = "CalculatedHits";

	private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

	// 苦无/手里剑/装饰扇的 AttacksPlayedThisTurn、钢笔尖的 AttackToDouble（私有 setter 的属性），
	// 以及双节棍/苦无/手里剑/装饰扇的 DoActivateVisuals()（私有方法），均为 0.107.1/0.110.0/0.111.0 原版成员。
	private static readonly PropertyInfo? KunaiAttacksPlayedThisTurnProperty = TryGetProperty(typeof(Kunai), "AttacksPlayedThisTurn");
	private static readonly PropertyInfo? ShurikenAttacksPlayedThisTurnProperty = TryGetProperty(typeof(Shuriken), "AttacksPlayedThisTurn");
	private static readonly PropertyInfo? OrnamentalFanAttacksPlayedThisTurnProperty = TryGetProperty(typeof(OrnamentalFan), "AttacksPlayedThisTurn");
	private static readonly PropertyInfo? PenNibAttackToDoubleProperty = TryGetProperty(typeof(PenNib), "AttackToDouble");
	private static readonly MethodInfo? NunchakuDoActivateVisualsMethod = TryGetMethod(typeof(Nunchaku), "DoActivateVisuals", InstanceNonPublic);
	private static readonly MethodInfo? KunaiDoActivateVisualsMethod = TryGetMethod(typeof(Kunai), "DoActivateVisuals", InstanceNonPublic);
	private static readonly MethodInfo? ShurikenDoActivateVisualsMethod = TryGetMethod(typeof(Shuriken), "DoActivateVisuals", InstanceNonPublic);
	private static readonly MethodInfo? OrnamentalFanDoActivateVisualsMethod = TryGetMethod(typeof(OrnamentalFan), "DoActivateVisuals", InstanceNonPublic);
	private static bool? _illusoryWeaponReflectionReady;

	/// <summary>
	/// 幻影武器要改写五个原版遗物的私有计数与视觉方法;任一缺失就整组停用并把符文标为本运行时不可用。
	/// IllusoryWeaponRune 的七个补丁类在 Prepare 里共用这一次判定。
	/// </summary>
	internal static bool IllusoryWeaponReflectionReady
	{
		get
		{
			if (_illusoryWeaponReflectionReady is bool cached)
			{
				return cached;
			}

			bool ready = KunaiAttacksPlayedThisTurnProperty != null
				&& ShurikenAttacksPlayedThisTurnProperty != null
				&& OrnamentalFanAttacksPlayedThisTurnProperty != null
				&& PenNibAttackToDoubleProperty != null
				&& NunchakuDoActivateVisualsMethod != null
				&& KunaiDoActivateVisualsMethod != null
				&& ShurikenDoActivateVisualsMethod != null
				&& OrnamentalFanDoActivateVisualsMethod != null;
			if (!ready)
			{
				HextechRuntimeRuneCompatibility.MarkPlayerRuneHookFailed<IllusoryWeaponRune>(
					"illusory weapon attack counters",
					new MissingMemberException("Illusory Weapon relic counters or activation visuals are missing in this game build."));
			}

			_illusoryWeaponReflectionReady = ready;
			return ready;
		}
	}

	internal static decimal CountFinisherAttackCardsPlayedThisTurn(CardModel card, Creature? _)
	{
		return HextechCombatHistoryHelper.CountOwnedAttackCardsPlayedThisTurn(
			card.Owner,
			card.CombatState as CombatState,
			firstInSeriesOnly: false,
			includeAutoPlay: true);
	}

	internal static async Task ResolveIllusoryWeaponNunchaku(Nunchaku nunchaku)
	{
		nunchaku.AttacksPlayed++;
		int cardsNeeded = nunchaku.DynamicVars.Cards.IntValue;
		if (cardsNeeded <= 0 || !CombatManager.Instance.IsInProgress || nunchaku.AttacksPlayed % cardsNeeded != 0)
		{
			return;
		}

		await PlayerCmd.GainEnergy(nunchaku.DynamicVars.Energy.BaseValue, nunchaku.Owner);
		// 遗物激活动画是纯表现层：原版 DoActivateVisuals 同样不参与结算，不等待它以免拖慢出牌链；失败只回退闪光。
		_ = TaskHelper.RunSafely(InvokePrivateRelicVisuals(nunchaku, NunchakuDoActivateVisualsMethod, nameof(Nunchaku)));
	}

	internal static async Task ResolveIllusoryWeaponKunai(Kunai kunai)
	{
		int attacksPlayed = IncrementIntProperty(kunai, KunaiAttacksPlayedThisTurnProperty);
		int cardsNeeded = kunai.DynamicVars.Cards.IntValue;
		if (cardsNeeded <= 0 || attacksPlayed % cardsNeeded != 0)
		{
			return;
		}

		await PowerCmd.Apply<DexterityPower>(kunai.Owner.Creature, kunai.DynamicVars.Dexterity.BaseValue, kunai.Owner.Creature, null);
		// 纯表现层，同上，不等待。
		_ = TaskHelper.RunSafely(InvokePrivateRelicVisuals(kunai, KunaiDoActivateVisualsMethod, nameof(Kunai)));
	}

	internal static async Task ResolveIllusoryWeaponShuriken(Shuriken shuriken)
	{
		int attacksPlayed = IncrementIntProperty(shuriken, ShurikenAttacksPlayedThisTurnProperty);
		int cardsNeeded = shuriken.DynamicVars.Cards.IntValue;
		if (cardsNeeded <= 0 || attacksPlayed % cardsNeeded != 0)
		{
			return;
		}

		await PowerCmd.Apply<StrengthPower>(shuriken.Owner.Creature, shuriken.DynamicVars.Strength.BaseValue, shuriken.Owner.Creature, null);
		// 纯表现层，同上，不等待。
		_ = TaskHelper.RunSafely(InvokePrivateRelicVisuals(shuriken, ShurikenDoActivateVisualsMethod, nameof(Shuriken)));
	}

	internal static async Task ResolveIllusoryWeaponOrnamentalFan(OrnamentalFan ornamentalFan)
	{
		int attacksPlayed = IncrementIntProperty(ornamentalFan, OrnamentalFanAttacksPlayedThisTurnProperty);
		int cardsNeeded = ornamentalFan.DynamicVars.Cards.IntValue;
		if (cardsNeeded <= 0 || attacksPlayed % cardsNeeded != 0)
		{
			return;
		}

		await CreatureCmd.GainBlock(ornamentalFan.Owner.Creature, ornamentalFan.DynamicVars.Block, null);
		// 纯表现层，同上，不等待。
		_ = TaskHelper.RunSafely(InvokePrivateRelicVisuals(ornamentalFan, OrnamentalFanDoActivateVisualsMethod, nameof(OrnamentalFan)));
	}

	internal static void ClearIllusoryWeaponPendingPenNib(Player? owner, CardModel card)
	{
		PenNib? penNib = owner?.GetRelic<PenNib>();
		if (penNib == null || !IsPenNibTracking(penNib, card))
		{
			return;
		}

		SetPenNibAttackToDouble(penNib, null);
	}

	internal static bool ShouldHandleIllusoryWeaponSkill(CardPlay cardPlay, Player? owner)
	{
		return owner != null
			&& cardPlay.Card.Type != CardType.Attack
			&& cardPlay.Card.Owner == owner
			&& IllusoryWeaponRune.IsAttackForEffects(cardPlay.Card, owner);
	}

	private static int IncrementIntProperty(object instance, PropertyInfo? property)
	{
		int value = property?.GetValue(instance) is int current ? current : 0;
		value++;
		property?.SetValue(instance, value);
		return value;
	}

	internal static bool IsPenNibTracking(PenNib penNib, CardModel card)
	{
		return ReferenceEquals(PenNibAttackToDoubleProperty?.GetValue(penNib), card);
	}

	internal static void SetPenNibAttackToDouble(PenNib penNib, CardModel? card)
	{
		PenNibAttackToDoubleProperty?.SetValue(penNib, card);
	}

	private static async Task InvokePrivateRelicVisuals(RelicModel relic, MethodInfo? method, string relicName)
	{
		if (method == null)
		{
			return;
		}

		try
		{
			await (method.Invoke(relic, null) as Task ?? Task.CompletedTask);
		}
		catch (Exception ex)
		{
			HextechLog.Warn("IllusoryWeapon", $"Failed to run {relicName} activation visuals: {ex.GetType().Name}: {ex.Message}");
			try
			{
				relic.Flash();
			}
			catch (Exception flashException)
			{
				HextechLog.Warn("IllusoryWeapon", $"Fallback flash failed for {relicName}: {flashException.Message}");
			}
		}
	}
}
