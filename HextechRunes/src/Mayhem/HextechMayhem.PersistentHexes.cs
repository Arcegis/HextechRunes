using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	private async Task ApplyPersistentMonsterHexes(Creature creature, bool replayOneShotPowers = false)
	{
		int? maxHpBaseOverride = replayOneShotPowers ? creature.MaxHp : null;
		await HextechEnemyHexDispatcher.ForEachActiveOrdered(
			this,
			static effect => effect.PersistentOrder,
			(effect, context) => effect.ApplyPersistentToEnemy(context, creature, maxHpBaseOverride, replayOneShotPowers));
	}

	internal static async Task EnsureMonsterMaxHpBonus(Creature creature, decimal bonusPercent, int? baseMaxHpOverride = null)
	{
		int baseMaxHp = baseMaxHpOverride ?? creature.MonsterMaxHpBeforeModification ?? creature.MaxHp;
		int expectedMaxHp = baseMaxHp + (int)Math.Floor(baseMaxHp * bonusPercent);
		int missingMaxHp = expectedMaxHp - creature.MaxHp;
		if (missingMaxHp > 0)
		{
			await GainMonsterMaxHpWithoutHeal(creature, missingMaxHp);
		}
		else
		{
			await KeepFurCoatMarkedEnemyAtOneHp(creature);
		}
	}

	internal static async Task GainMonsterMaxHpWithoutHeal(Creature creature, int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		int oldMaxHp = creature.MaxHp;
		int oldCurrentHp = creature.CurrentHp;
		await CreatureCmdCompat.SetMaxHp(creature, oldMaxHp + amount);

		int actualMaxHpGain = Math.Max(0, creature.MaxHp - oldMaxHp);
		if (actualMaxHpGain <= 0)
		{
			return;
		}

		int newCurrentHp = IsFurCoatMarkedEnemy(creature)
			? 1
			: Math.Min(creature.MaxHp, oldCurrentHp + actualMaxHpGain);
		if (newCurrentHp != creature.CurrentHp)
		{
			await CreatureCmd.SetCurrentHp(creature, newCurrentHp);
		}
	}

	private static Task KeepFurCoatMarkedEnemyAtOneHp(Creature creature)
	{
		if (IsFurCoatMarkedEnemy(creature) && creature.CurrentHp != 1)
		{
			return CreatureCmd.SetCurrentHp(creature, 1m);
		}

		return Task.CompletedTask;
	}

	private static bool IsFurCoatMarkedEnemy(Creature creature)
	{
		if (creature.Side != CombatSide.Enemy || !creature.IsAlive || creature.CombatState == null)
		{
			return false;
		}

		foreach (RelicModel relic in creature.CombatState.Players.SelectMany(static player => player.Relics))
		{
			if (relic is not FurCoat furCoat || furCoat.Owner?.RunState.CurrentMapPoint == null)
			{
				continue;
			}

			if (furCoat.GetMarkedCoords()?.Contains(furCoat.Owner.RunState.CurrentMapPoint.coord) == true)
			{
				return true;
			}
		}

		return false;
	}

	internal void UpdateEnemyScale(Creature creature)
	{
		float baseScale = HasActiveMonsterHex(MonsterHexKind.Goliath) ? 1.35f : 1f;
		// 巨人杀手敌方版让敌人体型缩小(纯视觉,呼应「体型变小」的设定,无机制意义)。
		float giantSlayerShrink = HasActiveMonsterHex(MonsterHexKind.GiantSlayer) ? 0.25f : 0f;
		int tankStacks = creature.CombatId == null ? 0 : _combatTracking.TankEngineStacks.GetValueOrDefault(creature.CombatId.Value, 0);
		int shrinkStacks = creature.CombatId == null ? 0 : _combatTracking.ShrinkEngineStacks.GetValueOrDefault(creature.CombatId.Value, 0);
		float finalScale = Math.Max(0.2f, baseScale + tankStacks * 0.05f - shrinkStacks * 0.02f - giantSlayerShrink);
		NCombatRoom.Instance?.GetCreatureNode(creature)?.SetDefaultScaleTo(finalScale, 0f);
	}
}
