using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes.Tests;

internal static partial class Program
{
	/// <summary>
	/// 写入只读/私有 set 自动属性的编译器支持字段（<c>&lt;Name&gt;k__BackingField</c>）。
	/// 从实例的运行时类型沿继承链查找；链上出现多个同名支持字段（派生类覆写了自动属性）时直接报错，
	/// 避免悄悄写到另一层。
	/// </summary>
	private static void SetAutoProperty(object target, string propertyName, object? value)
	{
		string fieldName = $"<{propertyName}>k__BackingField";
		FieldInfo? found = null;
		for (Type? type = target.GetType(); type != null; type = type.BaseType)
		{
			FieldInfo? field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
			if (field == null)
			{
				continue;
			}

			if (found != null)
			{
				throw new InvalidOperationException($"{target.GetType().FullName}.{propertyName} has backing fields on both {found.DeclaringType?.FullName} and {type.FullName}");
			}

			found = field;
		}

		if (found == null)
		{
			throw new InvalidOperationException($"{target.GetType().FullName}.{propertyName} has no auto-property backing field");
		}

		found.SetValue(target, value);
	}

	private static (HextechEnemyHexContext Context, Player First, Player Second) CreatePrismaticEnemyFixture()
	{
		RunState run = (RunState)RuntimeHelpers.GetUninitializedObject(typeof(RunState));
		Player first = CreateOrdinalTestPlayer(1), second = CreateOrdinalTestPlayer(2);
		AccessTools.Field(typeof(RunState), "_players").SetValue(run, new List<Player> { first, second });
		FieldInfo history = AccessTools.Field(typeof(RunState), "_mapPointHistory");
		history.SetValue(run, Activator.CreateInstance(history.FieldType));
		AccessTools.Property(typeof(RunState), "Rng").SetValue(run, new RunRngSet("FOUR-ENEMY-HEXES"));
		CombatState combat = new(runState: run);
		foreach (Player player in new[] { first, second })
		{
			AccessTools.Field(typeof(Player), "_runState").SetValue(player, run);
			SetAutoProperty(player, nameof(Player.Creature), CreatePrismaticTestCreature(CombatSide.Player, combat));
			PlayerCombatState state = (PlayerCombatState)RuntimeHelpers.GetUninitializedObject(typeof(PlayerCombatState));
			SetAutoProperty(state, nameof(PlayerCombatState.Hand), new CardPile(PileType.Hand));
			SetAutoProperty(state, nameof(PlayerCombatState.DrawPile), new CardPile(PileType.Draw));
			SetAutoProperty(state, nameof(PlayerCombatState.DiscardPile), new CardPile(PileType.Discard));
			SetAutoProperty(state, nameof(PlayerCombatState.ExhaustPile), new CardPile(PileType.Exhaust));
			SetAutoProperty(state, nameof(PlayerCombatState.PlayPile), new CardPile(PileType.Play));
			SetAutoProperty(player, nameof(Player.Deck), new CardPile(PileType.Deck));
			AccessTools.Property(typeof(Player), "PlayerCombatState").SetValue(player, state);
		}
		HextechMayhemModifier modifier = CreateMutableTestModel<HextechMayhemModifier>();
		AccessTools.Field(typeof(ModifierModel), "_runState").SetValue(modifier, run);
		return (new HextechEnemyHexContext(modifier), first, second);
	}

	private static Creature CreatePrismaticTestCreature(CombatSide side, CombatState combat)
	{
		Creature creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
		SetAutoProperty(creature, nameof(Creature.Side), side);
		AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 50);
		AccessTools.Property(typeof(Creature), "CombatState").SetValue(creature, combat);
		return creature;
	}

	private sealed class GeneratedTestRelic : RelicModel, IHextechGeneratedRune
	{
		public override RelicRarity Rarity => RelicRarity.Event;
		public string Data { get; set; } = "";
		public string ExportSelectionData() => Data;
		public bool TryImportSelectionData(string data) { Data = data; return data.StartsWith("recipe:", StringComparison.Ordinal); }
	}
}
