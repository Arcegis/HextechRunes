using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HextechRunes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes.Tests;

internal static partial class Program
{
	private static readonly List<(Player Player, int Gold)> UpgradeGoldRewards = [];

	private static T UpgradeTestPower<T>(Creature owner, int amount) where T : PowerModel
	{
		T power = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
		AccessTools.Field(typeof(AbstractModel), "<IsMutable>k__BackingField").SetValue(power, true);
		AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(power, owner);
		AccessTools.Field(typeof(PowerModel), "_amount").SetValue(power, amount);
		return power;
	}

	private static void RoyaltiesUpgradePaysImmediatelyAndPreservesLegacyAccrual()
	{
		WithImmediateGoldFixture((first, second, _, listener) =>
		{
			RoyaltiesUpgradeRune rune = CreateMutableTestModel<RoyaltiesUpgradeRune>();
			rune.Owner = first;
			RoyaltiesPower power = UpgradeTestPower<RoyaltiesPower>(first.Creature, 19);
			AccessTools.Field(typeof(Creature), "_powers").SetValue(first.Creature, new List<PowerModel> { power });
			rune.BeforeCombatStart().GetAwaiter().GetResult();
			rune.AfterPlayerTurnStart(null!, second).GetAwaiter().GetResult();
			Equal(0, listener.Calls, "teammate's turn does not grant royalties");
			int gold = first.Gold;
			rune.AfterPlayerTurnStart(null!, first).GetAwaiter().GetResult();
			rune.AfterPlayerTurnStart(null!, first).GetAwaiter().GetResult();
			Equal(0, rune.SavedCountThisCombat, "immediate income does not accrue for a second payout");
			Equal(gold + 18, first.Gold, "floor fifty percent each turn, paid immediately with no compounding");
			Equal(19, power.Amount, "original combat-end royalties remain intact");
			rune.AfterCombatEnd((CombatRoom)RuntimeHelpers.GetUninitializedObject(typeof(CombatRoom))).GetAwaiter().GetResult();
			Equal(0, UpgradeGoldRewards.Count, "no battle-end duplicate");
			// 旧版本保存的尚未领取计数仍能还原并结算一次。
			rune.SavedCountThisCombat = 13;
#if STS2_109_OR_NEWER
			MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache.CacheSavedPropertiesForTypeDebug(typeof(RoyaltiesUpgradeRune));
#else
			HextechSavedPropertyBootstrap.InjectModelType(typeof(RoyaltiesUpgradeRune));
#endif
			SerializableRelic saved = rune.ToSerializable();
			int count = saved.Props!.ints!.Single(p => p.name == nameof(RoyaltiesUpgradeRune.SavedCountThisCombat)).value;
			RoyaltiesUpgradeRune loaded = CreateMutableTestModel<RoyaltiesUpgradeRune>();
			loaded.Owner = first;
			loaded.SavedCountThisCombat = count;
			loaded.AfterCombatEnd((CombatRoom)RuntimeHelpers.GetUninitializedObject(typeof(CombatRoom))).GetAwaiter().GetResult();
			loaded.AfterCombatEnd((CombatRoom)RuntimeHelpers.GetUninitializedObject(typeof(CombatRoom))).GetAwaiter().GetResult();
			Equal(1, UpgradeGoldRewards.Count, "one fixed combat reward");
			Equal((first, 13), UpgradeGoldRewards[0], "legacy saved accrual belongs only to its owner");
			Equal(0, loaded.SavedCountThisCombat, "payout clears accrued counter");
			loaded.BeforeCombatStart().GetAwaiter().GetResult();
			Equal(0, loaded.SavedCountThisCombat, "next combat starts empty");
		});
	}

	private static bool CaptureUpgradeGoldReward(Player player, Reward reward)
	{
		UpgradeGoldRewards.Add((player, ((GoldReward)reward).Amount));
		return false;
	}

	private static void PlayerUpgradeKeywordsAndNoDrawStayOwnerScoped()
	{
		var (_, first, second) = CreatePrismaticEnemyFixture();
		BulletTimeUpgradeRune bullet = CreateMutableTestModel<BulletTimeUpgradeRune>(); bullet.Owner = first;
		BulletTime card = CreateMutableTestModel<BulletTime>(); card.Owner = first;
		NoDrawPower noDraw = UpgradeTestPower<NoDrawPower>(first.Creature, 1);
		Equal(0m, bullet.ModifyPowerAmountGivenMultiplicative(noDraw, first.Creature, 1, first.Creature, card), "suppress only Bullet Time's No Draw");
		Equal(1m, bullet.ModifyPowerAmountGivenMultiplicative(noDraw, first.Creature, 1, first.Creature, null), "other No Draw remains");
		BulletTime foreignBullet = CreateMutableTestModel<BulletTime>(); foreignBullet.Owner = second;
		Equal(1m, bullet.ModifyPowerAmountGivenMultiplicative(noDraw, second.Creature, 1, second.Creature, foreignBullet), "teammate unaffected");
		RebootUpgradeRune reboot = CreateMutableTestModel<RebootUpgradeRune>(); reboot.Owner = first;
		Reboot rebootCard = CreateMutableTestModel<Reboot>(); rebootCard.Owner = first;
		HashSet<CardKeyword> keywords = [CardKeyword.Exhaust];
		Expect(reboot.TryModifyKeywordsInCombat(rebootCard, keywords) && !keywords.Contains(CardKeyword.Exhaust), "owner's Reboot loses exhaust");
		Reboot foreignReboot = CreateMutableTestModel<Reboot>(); foreignReboot.Owner = second; keywords.Add(CardKeyword.Exhaust);
		Expect(!reboot.TryModifyKeywordsInCombat(foreignReboot, keywords) && keywords.Contains(CardKeyword.Exhaust), "teammate Reboot keeps exhaust");
		HangUpgradeRune hang = CreateMutableTestModel<HangUpgradeRune>(); hang.Owner = first;
		Hang hangCard = CreateMutableTestModel<Hang>(); hangCard.Owner = first;
		keywords.Clear();
		Expect(hang.TryModifyKeywordsInCombat(hangCard, keywords) && keywords.Contains(CardKeyword.Exhaust), "upgraded Hang exhausts");
		HextechHangPower power = UpgradeTestPower<HextechHangPower>(second.Creature, 4);
		Equal(4m, power.ModifyDamageMultiplicativeCompat(second.Creature, 3, ValueProp.Unpowered, null, null), "Hang amplifies non-card damage");
		Equal(4m, power.ModifyDamageMultiplicativeCompat(second.Creature, 3, ValueProp.Move, first.Creature, card), "Hang amplifies other attacks");
		Equal(1m, power.ModifyDamageMultiplicativeCompat(first.Creature, 3, ValueProp.Move, second.Creature, card), "Hang is target scoped");
		Equal(8, 4 + HangUpgradeRune.NextIncrease(4), "repeated Hang doubles the multiplier");
	}

	private static void ClawUpgradeSeparatesPermanentGrowthFromNativeCombatGrowth()
	{
		var (_, first, second) = CreatePrismaticEnemyFixture();
		ClawUpgradeRune rune = CreateMutableTestModel<ClawUpgradeRune>(); rune.Owner = first;
		Claw deck = CreateMutableTestModel<Claw>(); deck.Owner = first;
		Claw combat = CreateMutableTestModel<Claw>(); combat.Owner = first; combat.DeckVersion = deck;
		Claw otherCopy = CreateMutableTestModel<Claw>(); otherCopy.Owner = first; otherCopy.DeckVersion = deck;
		Claw foreign = CreateMutableTestModel<Claw>(); foreign.Owner = second;
		AccessTools.Method(typeof(Claw), "BuffFromClawPlay").Invoke(combat, [2m]);
		AccessTools.Method(typeof(Claw), "BuffFromClawPlay").Invoke(otherCopy, [2m]);
		rune.GrowClaws([deck, combat, combat, otherCopy, foreign]);
		Equal(4m, deck.DynamicVars.Damage.BaseValue, "deck grows once despite multiple combat copies");
		Equal(6m, combat.DynamicVars.Damage.BaseValue, "native plus-two is kept only in combat");
		Equal(6m, otherCopy.DynamicVars.Damage.BaseValue, "every current combat copy grows once");
		Equal(3m, foreign.DynamicVars.Damage.BaseValue, "teammate's Claw is unchanged");
		SerializableCard saved = new();
		Type store = typeof(HextechSelfUpgradeCardStore);
		AccessTools.Method(store.GetNestedType("ToSerializablePatch", BindingFlags.NonPublic), "Postfix").Invoke(null, [deck, saved]);
		Equal(1, saved.Props!.ints!.Single(p => p.name == HextechSelfUpgradeCardStore.DamageBonusSavedPropertyName).value, "save contains permanent one, not native combat two");
		Claw loaded = CreateMutableTestModel<Claw>(); loaded.Owner = first;
		AccessTools.Method(store.GetNestedType("FromSerializablePatch", BindingFlags.NonPublic), "Postfix").Invoke(null, [saved, loaded]);
		Equal(4m, loaded.DynamicVars.Damage.BaseValue, "loading restores only permanent growth");
	}

	private static void PersistentPowerUpgradesDoNotAffectOtherPlayers()
	{
		var (_, first, second) = CreatePrismaticEnemyFixture();
		foreach (Player player in new[] { first, second })
		{
			AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(player.Creature, player);
			AccessTools.Field(typeof(Player), "_relics").SetValue(player, new List<RelicModel>());
		}
		RageUpgradeRune rage = CreateMutableTestModel<RageUpgradeRune>(); rage.Owner = first;
		ReflectUpgradeRune reflect = CreateMutableTestModel<ReflectUpgradeRune>(); reflect.Owner = first;
		((List<RelicModel>)AccessTools.Field(typeof(Player), "_relics").GetValue(first)!).AddRange([rage, reflect]);
		foreach (var (runeType, power, foreignPower) in new (Type, PowerModel, PowerModel)[]
		{
			(typeof(RageUpgradeRune), UpgradeTestPower<RagePower>(first.Creature, 3), UpgradeTestPower<RagePower>(second.Creature, 3)),
			(typeof(ReflectUpgradeRune), UpgradeTestPower<ReflectPower>(first.Creature, 3), UpgradeTestPower<ReflectPower>(second.Creature, 3))
		})
		{
			MethodInfo prefix = AccessTools.Method(runeType.GetNestedTypes(BindingFlags.NonPublic).Single(), "Prefix");
			object?[] args = [power, null];
			Equal(false, (bool)prefix.Invoke(null, args)!, "owner's cleanup is skipped");
			((Task)args[1]!).GetAwaiter().GetResult();
			Equal(true, (bool)prefix.Invoke(null, [foreignPower, null])!, "teammate keeps native cleanup");
		}
	}

	private static void CardUpgradeReplacementBodiesMatchReviewedVanilla()
	{
		(Type Type, string Name)[] targets =
		[
			(typeof(LoopPower), nameof(LoopPower.AfterPlayerTurnStart)),
			(typeof(RagePower), nameof(RagePower.AfterSideTurnEnd)),
			(typeof(ReflectPower), nameof(ReflectPower.AfterSideTurnStart)),
			(typeof(FlakCannon), "OnPlay"), (typeof(Hang), "OnPlay"),
			(typeof(InfernoPower), nameof(InfernoPower.AfterDamageReceived)),
			(typeof(FlameBarrierPower), nameof(FlameBarrierPower.AfterDamageReceived))
		];
		var expected = HextechVanillaCopyGuard.LoadExpectedHashes();
		List<string> rows = [];
		foreach (var (type, name) in targets)
		{
			MethodInfo entry = AccessTools.Method(type, name);
			IEnumerable<MethodInfo> methods = entry.GetCustomAttribute<AsyncStateMachineAttribute>() == null
				? [entry] : [entry, GetAsyncStateMachineMoveNext(entry)];
			foreach (MethodInfo method in methods)
			{
				string key = HextechVanillaCopyGuard.DescribeTarget(method);
				string hash = HextechVanillaCopyGuard.ComputeIlHash(method)!;
				rows.Add($"{key}={hash}");
				if (Environment.GetEnvironmentVariable("HEXTECH_WRITE_UPGRADE_GUARD") != "1")
					Expect(expected.TryGetValue(key, out string? frozen) && frozen == hash, "review native upgrade target after IL drift: " + key);
			}
		}
		if (Environment.GetEnvironmentVariable("HEXTECH_WRITE_UPGRADE_GUARD") == "1")
		{
			string path = Path.Combine(FindTestsSourceDirectory(), "..", $"vanilla_copy_guard.{ModInfo.TargetGameVersion}.txt");
			string[] old = File.Exists(path) ? File.ReadAllLines(path) : ["# 原版局部替换守卫：入口与异步结算体。"];
			File.WriteAllLines(path, old.Concat(rows).Distinct());
		}
	}
}
