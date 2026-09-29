using System.Reflection;
using HextechRunes;

namespace HextechRunes.Tests;

internal static partial class Program
{
	[HextechTest]
	private static void NearDeathHpLossResolvesDyingAndKillThresholds()
	{
		HextechNearDeathHpLoss survive = HextechNearDeathHpLoss.Resolve(wasDying: false, debt: 0, currentHp: 10, amount: 4m, deathLimit: 20);
		Equal(4, survive.HpLoss, "positive-HP loss");
		Expect(!survive.Dying && !survive.Killed, "staying above 1 HP is neither dying nor killed");
		Equal(6, survive.CurrentHp, "remaining HP");
		Equal(0, survive.Debt, "no debt above 1 HP");

		HextechNearDeathHpLoss enterDying = HextechNearDeathHpLoss.Resolve(false, 0, 3, 10m, 20);
		Expect(enterDying.Dying && !enterDying.Killed, "dropping below 1 HP enters the dying state");
		Equal(1, enterDying.CurrentHp, "dying keeps HP pinned at 1");
		Equal(7, enterDying.Debt, "debt is the negative effective HP");

		HextechNearDeathHpLoss deeper = HextechNearDeathHpLoss.Resolve(true, 7, 1, 5m, 20);
		Expect(deeper.Dying, "further loss while dying keeps dying");
		Equal(12, deeper.Debt, "debt accumulates from the previous debt, not the pinned HP");
		Equal(5, deeper.HpLoss, "HP loss is still reported while HP stays at 1");

		HextechNearDeathHpLoss killed = HextechNearDeathHpLoss.Resolve(true, 15, 1, 10m, 20);
		Expect(killed.Killed && !killed.Dying, "reaching the death limit kills");
		Equal(0, killed.CurrentHp, "killed sets HP to 0");
		Equal(20, killed.Debt, "killed debt is clamped to the limit");
		Equal(5, killed.OverkillDamage, "overkill beyond the death limit");

		Equal(HextechCreatureStatLimits.StatHardCap, HextechNearDeathHpLoss.ToHpLoss(decimal.MaxValue), "HP loss uses the vanilla hard cap");
	}

	[HextechTest]
	private static void RuneFamilyHelpersKeepThresholdAndRoundSemantics()
	{
		Equal(0, HextechRelicBase.CountThresholdCrossings(0, 9, 10), "below the first threshold");
		Equal(1, HextechRelicBase.CountThresholdCrossings(9, 10, 10), "crossing one threshold");
		Equal(3, HextechRelicBase.CountThresholdCrossings(5, 35, 10), "history replay can cross several thresholds");
		Equal(0, HextechRelicBase.CountThresholdCrossings(10, 5, 10), "progress never counts backwards");
		Equal(4, HextechRelicBase.CountThresholdCrossings(0, 4, 0), "non-positive thresholds fall back to one");

		int lastRound = -1;
		Expect(HextechRoundInterval.TryClaimRound(ref lastRound, 3), "first claim in a round succeeds");
		Equal(3, lastRound, "claim records the round");
		Expect(!HextechRoundInterval.TryClaimRound(ref lastRound, 3), "re-entrant claim in the same round fails");
		Expect(HextechRoundInterval.TryClaimRound(ref lastRound, 4), "next round can be claimed");
	}

	[HextechTest]
	private static void RuneFamilyTypesUseSharedBases()
	{
		foreach (Type type in new[]
		{
			typeof(OverflowRune), typeof(FirstAidKitRune), typeof(SacrificeRune), typeof(BackToBasicsRune),
			typeof(GoliathRune), typeof(ProteinShakeRune), typeof(GoldenSpatulaRune), typeof(NineDragonPowerRune)
		})
		{
			Expect(typeof(IHextechHealingMultiplierProvider).IsAssignableFrom(type), $"{type.Name} should report its healing multiplier as a provider");
		}

		foreach (Type type in new[]
		{
			typeof(LifeFlowRune), typeof(LubricantRune), typeof(MagicMissileRune), typeof(PowerShieldRune),
			typeof(SomethingForNothingRune), typeof(SpeedDemonRune), typeof(SwordFlightRune), typeof(TriPrismRune),
			typeof(CerberusRune), typeof(FinalFormRune), typeof(FanTheHammerRune), typeof(FirstTypedCardReplayRuneBase),
			typeof(LimitedDebuffProcRelicBase)
		})
		{
			Expect(typeof(TurnScopedRelicBase).IsAssignableFrom(type), $"{type.Name} should reset turn state through TurnScopedRelicBase");
		}

		// SavedProperty 必须仍声明在各符文自己的类型上（net-id 布局由属性集合决定）。
		foreach (Type type in new[] { typeof(SwiftAndSafeRune), typeof(NightstalkingRune), typeof(WarmogsSpiritRune), typeof(DeathWarrantRune) })
		{
			Expect(typeof(DrawThresholdRuneBase).IsAssignableFrom(type), $"{type.Name} should share the draw-threshold base");
			PropertyInfo? saved = type.GetProperty("SavedCardsDrawnThisCombat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			Expect(saved != null && saved.PropertyType == typeof(int), $"{type.Name} should keep SavedCardsDrawnThisCombat on its own type");
		}
	}
}
