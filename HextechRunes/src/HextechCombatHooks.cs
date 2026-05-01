using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static class HextechCombatHooks
{
	private static bool _handlingGoliathMaxHp;

	private readonly record struct HealPostState(Player? Player, Creature Creature, decimal Amount, bool ShouldProcess);

	public static void Install(Harmony harmony)
	{
		HarmonyMethod canPlayPostfix = new(typeof(HextechCombatHooks), nameof(CardCanPlayPostfix))
		{
			priority = Priority.Last
		};
		HarmonyMethod canPlayWithReasonPostfix = new(typeof(HextechCombatHooks), nameof(CardCanPlayWithReasonPostfix))
		{
			priority = Priority.Last
		};

		harmony.Patch(
			RequireMethod(typeof(CardPileCmd), nameof(CardPileCmd.Draw), BindingFlags.Public | BindingFlags.Static, typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(DrawPrefix)));
		harmony.Patch(
			RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.Heal), BindingFlags.Public | BindingFlags.Static, typeof(Creature), typeof(decimal), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(HealPrefix)),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(HealPostfix)));
		harmony.Patch(
			RequireMethod(typeof(CardModel), nameof(CardModel.CanPlay), BindingFlags.Instance | BindingFlags.Public),
			postfix: canPlayPostfix);
		harmony.Patch(
			RequireMethod(typeof(CardModel), nameof(CardModel.CanPlay), BindingFlags.Instance | BindingFlags.Public, typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()),
			postfix: canPlayWithReasonPostfix);
		harmony.Patch(
			RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.GainMaxHp), BindingFlags.Public | BindingFlags.Static, typeof(Creature), typeof(decimal)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(GainMaxHpPrefix)),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(ResetGoliathTaskPostfix)));
		harmony.Patch(
			RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.LoseMaxHp), BindingFlags.Public | BindingFlags.Static, typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(LoseMaxHpPrefix)),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(ResetGoliathTaskPostfix)));
		MethodInfo setMaxHpMethod = RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.SetMaxHp), BindingFlags.Public | BindingFlags.Static, typeof(Creature), typeof(decimal));
		harmony.Patch(
			setMaxHpMethod,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(SetMaxHpPrefix)),
			postfix: new HarmonyMethod(
				typeof(HextechCombatHooks),
				setMaxHpMethod.ReturnType == typeof(Task<decimal>)
					? nameof(ResetGoliathDecimalTaskPostfix)
					: nameof(ResetGoliathTaskPostfix)));
		harmony.Patch(
			RequireMethod(typeof(StormPower), nameof(StormPower.BeforeCardPlayed), BindingFlags.Public | BindingFlags.Instance, typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(StormBeforeCardPlayedPrefix)));
		harmony.Patch(
			RequireMethod(typeof(StormPower), nameof(StormPower.AfterCardPlayed), BindingFlags.Public | BindingFlags.Instance, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(StormAfterCardPlayedPrefix)));
	}

	private static bool DrawPrefix(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw, ref Task<IEnumerable<CardModel>> __result)
	{
		NoNonsenseRune? noNonsenseRune = player.GetRelic<NoNonsenseRune>();
		if (noNonsenseRune == null || fromHandDraw || count <= 0m || player.Creature.CombatState == null)
		{
			return true;
		}

		int drawsPrevented = (int)Math.Ceiling(count);
		if (drawsPrevented <= 0)
		{
			__result = Task.FromResult<IEnumerable<CardModel>>(Array.Empty<CardModel>());
			return false;
		}

		__result = DrawNoNonsense(noNonsenseRune, drawsPrevented, player);
		return false;
	}

	private static async Task<IEnumerable<CardModel>> DrawNoNonsense(NoNonsenseRune noNonsenseRune, int drawsPrevented, Player player)
	{
		await noNonsenseRune.HandlePreventedNonHandDraw(drawsPrevented);
		await PowerCmd.Apply<StrengthPower>(player.Creature, drawsPrevented, player.Creature, null);
		return Array.Empty<CardModel>();
	}

	private static bool HealPrefix(Creature creature, ref decimal amount, ref Task __result, out HealPostState __state)
	{
		Player? player = creature.Player;
		if (player != null && creature == player.Creature)
		{
			if (player.GetRelic<OverflowRune>() != null)
			{
				amount *= 2m;
			}

			if (player.GetRelic<FirstAidKitRune>() != null)
			{
				amount *= 1.25m;
			}

			if (player.GetRelic<SacrificeRune>() is SacrificeRune sacrificeRune)
			{
				amount *= sacrificeRune.SustainMultiplier;
			}

			if (player.GetRelic<BackToBasicsRune>() != null)
			{
				amount *= 1.4m;
			}

			if (player.GetRelic<GoliathRune>() != null)
			{
				amount *= 1.2m;
			}

			if (player.GetRelic<ProteinShakeRune>() is ProteinShakeRune proteinShakeRune)
			{
				amount *= proteinShakeRune.SustainMultiplier;
			}

			if (player.GetRelic<ProtectionForge>() is ProtectionForge protectionForge)
			{
				amount *= protectionForge.SustainMultiplier;
			}
		}

		if (player?.GetRelic<GlassCannonRune>() is GlassCannonRune glassCannonRune && creature == player.Creature)
		{
			int healCap = (int)Math.Floor(creature.MaxHp * glassCannonRune.HealCapPercent);
			amount = Math.Min(amount, Math.Max(0, healCap - creature.CurrentHp));
			if (amount <= 0m)
			{
				__state = default;
				__result = Task.CompletedTask;
				return false;
			}
		}

		if (creature.Side == CombatSide.Enemy
			&& creature.CombatState?.RunState is RunState runState
			&& GetMayhemModifier(runState) is HextechMayhemModifier modifier)
		{
			amount = modifier.ModifyEnemyHealAmount(creature, amount);
			if (amount <= 0m)
			{
				__state = default;
				__result = Task.CompletedTask;
				return false;
			}
		}

		if (amount <= 0m)
		{
			__state = default;
			__result = Task.CompletedTask;
			return false;
		}

		__state = new HealPostState(player, creature, amount, ShouldProcess: true);
		return true;
	}

	private static void HealPostfix(HealPostState __state, ref Task __result)
	{
		if (!__state.ShouldProcess)
		{
			return;
		}

		__result = HealAfterOriginal(__result, __state);
	}

	private static async Task HealAfterOriginal(Task original, HealPostState state)
	{
		await original;

		Player? player = state.Player;
		Creature creature = state.Creature;
		decimal amount = state.Amount;
		if (player?.GetRelic<HolyFireRune>() != null
			&& creature == player.Creature
			&& creature.CombatState != null)
		{
			List<Creature> enemies = creature.CombatState.Enemies.Where(static enemy => enemy.IsAlive).ToList();
			int burnAmount = (int)Math.Floor(amount);
			if (enemies.Count > 0 && burnAmount > 0)
			{
				Creature target = enemies[player.RunState.Rng.Niche.NextInt(enemies.Count)];
				await PowerCmd.Apply<HextechBurnPower>(target, burnAmount, player.Creature, null);
			}
		}

		if (player?.GetRelic<CircleOfDeathRune>() is CircleOfDeathRune circleOfDeathRune
			&& creature == player.Creature
			&& creature.CombatState != null)
		{
			await circleOfDeathRune.HandleSustainGained(amount);
		}
	}

	private static void CardCanPlayPostfix(CardModel __instance, ref bool __result)
	{
		if (__result && IsBlockedByBackToBasics(__instance))
		{
			__result = false;
		}
	}

	private static void CardCanPlayWithReasonPostfix(CardModel __instance, ref bool __result, ref UnplayableReason reason, ref AbstractModel preventer)
	{
		if (!__result)
		{
			return;
		}

		if (!IsBlockedByBackToBasics(__instance, out AbstractModel? backToBasicsPreventer))
		{
			return;
		}

		reason = default;
		preventer = backToBasicsPreventer!;
		__result = false;
	}

	private static bool GainMaxHpPrefix(Creature creature, ref decimal amount, ref Task __result, out bool __state)
	{
		__state = false;
		if (_handlingGoliathMaxHp || creature.Player?.GetRelic<GoliathRune>() is not GoliathRune rune)
		{
			return true;
		}

		rune.EnsureBaseMaxHpInitialized();
		int oldActual = creature.MaxHp;
		rune.BaseMaxHp += (int)amount;
		int newActual = rune.GetScaledMaxHp();
		int delta = Math.Max(0, newActual - oldActual);
		if (delta == 0)
		{
			__result = Task.CompletedTask;
			return false;
		}

		_handlingGoliathMaxHp = true;
		__state = true;
		amount = delta;
		return true;
	}

	private static bool LoseMaxHpPrefix(Creature creature, ref decimal amount, ref Task __result, out bool __state)
	{
		__state = false;
		if (_handlingGoliathMaxHp || creature.Player?.GetRelic<GoliathRune>() is not GoliathRune rune)
		{
			return true;
		}

		rune.EnsureBaseMaxHpInitialized();
		int oldActual = creature.MaxHp;
		rune.BaseMaxHp -= (int)amount;
		int newActual = rune.GetScaledMaxHp();
		int loss = Math.Max(0, oldActual - newActual);
		if (loss == 0)
		{
			__result = Task.CompletedTask;
			return false;
		}

		_handlingGoliathMaxHp = true;
		__state = true;
		amount = loss;
		return true;
	}

	private static bool SetMaxHpPrefix(Creature creature, ref decimal amount, out bool __state)
	{
		__state = false;
		if (_handlingGoliathMaxHp || creature.Player?.GetRelic<GoliathRune>() is not GoliathRune rune)
		{
			return true;
		}

		rune.BaseMaxHp = (int)Math.Max(1m, amount);
		_handlingGoliathMaxHp = true;
		__state = true;
		amount = rune.GetScaledMaxHp();
		return true;
	}

	private static bool StormBeforeCardPlayedPrefix(StormPower __instance, ref Task __result)
	{
		if (ShouldUseHextechStormHandling(__instance))
		{
			__result = Task.CompletedTask;
			return false;
		}

		return true;
	}

	private static bool StormAfterCardPlayedPrefix(StormPower __instance, ref Task __result)
	{
		if (ShouldUseHextechStormHandling(__instance))
		{
			__result = Task.CompletedTask;
			return false;
		}

		return true;
	}

	private static bool ShouldUseHextechStormHandling(StormPower stormPower)
	{
		return stormPower.Owner?.CombatState?.RunState is RunState runState
			&& GetMayhemModifier(runState) != null;
	}

	private static void ResetGoliathTaskPostfix(bool __state, ref Task __result)
	{
		if (__state)
		{
			__result = CompleteWithReset(__result);
		}
	}

	private static void ResetGoliathDecimalTaskPostfix(bool __state, ref Task<decimal> __result)
	{
		if (__state)
		{
			__result = CompleteWithReset(__result);
		}
	}

	private static async Task CompleteWithReset(Task task)
	{
		try
		{
			await task;
		}
		finally
		{
			_handlingGoliathMaxHp = false;
		}
	}

	private static async Task<decimal> CompleteWithReset(Task<decimal> task)
	{
		try
		{
			return await task;
		}
		finally
		{
			_handlingGoliathMaxHp = false;
		}
	}

	private static bool IsBlockedByBackToBasics(CardModel card)
	{
		return IsBlockedByBackToBasics(card, out _);
	}

	private static bool IsBlockedByBackToBasics(CardModel card, out AbstractModel? preventer)
	{
		preventer = null;
		if (card.Owner == null)
		{
			return false;
		}

		if (card.EnergyCost.CostsX || card.EnergyCost.GetAmountToSpend() < 3m)
		{
			return false;
		}

		BackToBasicsRune? rune = card.Owner.GetRelic<BackToBasicsRune>();
		if (rune != null)
		{
			preventer = rune;
			return true;
		}

		if (card.Owner.Creature.CombatState?.RunState is RunState runState
			&& card.Owner.Creature.Side == CombatSide.Player
			&& GetMayhemModifier(runState) is HextechMayhemModifier modifier
			&& modifier.HasActiveMonsterHex(MonsterHexKind.BackToBasics))
		{
			preventer = modifier;
			return true;
		}

		return false;
	}

	private static HextechMayhemModifier? GetMayhemModifier(RunState runState)
	{
		return runState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault();
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}
}
