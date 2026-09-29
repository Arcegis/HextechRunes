namespace HextechRunes;

/// <summary>
/// 弹幕类符文（魔法飞弹、点亮他们、双生之焰）共用的结算：伤害留在当前卡牌动作内逐发结算，单人与联机同一路径；
/// 弹道只做视觉，放在独立任务里播放，不能在那里稍后改血量。
/// </summary>
internal static class HextechMissileVolley
{
	/// <summary>伤害等于打出牌的实付能量，不为负。</summary>
	internal static decimal DamageFromEnergyCost(decimal energyCost)
	{
		return Math.Max(0m, energyCost);
	}

	internal static async Task PlayVfxAsync(
		Creature source,
		IReadOnlyList<Creature> targets,
		int missileCount,
		Func<Creature, Creature, int, Task<bool>> playMissile)
	{
		await Task.WhenAll(Enumerable.Range(0, missileCount)
			.SelectMany(missileIndex => targets
				.Select(target => playMissile(source, target, missileIndex))));
	}

	/// <summary>每一发依次打一轮所有目标；来源死亡或离开本场战斗时停止，已死或离场的目标跳过。</summary>
	internal static async Task ResolveVolleyDamageInLockstepAsync(
		PlayerChoiceContext choiceContext,
		Creature source,
		HextechCombatState combatState,
		IReadOnlyList<Creature> targets,
		int missileCount,
		Func<Creature, decimal> damageFor)
	{
		for (int missileIndex = 0; missileIndex < missileCount; missileIndex++)
		{
			if (source.IsDead || !ReferenceEquals(source.CombatState, combatState))
			{
				return;
			}

			foreach (Creature target in targets)
			{
				if (!target.IsAlive || !ReferenceEquals(target.CombatState, combatState))
				{
					continue;
				}

				await HextechGameApiCompat.Damage(
					choiceContext,
					target,
					damageFor(target),
					ValueProp.Unpowered,
					source,
					null);
			}
		}
	}
}
