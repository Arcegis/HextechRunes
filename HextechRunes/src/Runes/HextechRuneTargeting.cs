namespace HextechRunes;

internal static class HextechRuneTargeting
{
	/// <summary>按 CombatId 取第一个可命中敌人：两端候选顺序一致，结果确定。</summary>
	internal static Creature? FirstHittableEnemy(HextechCombatState combatState)
	{
		return combatState.HittableEnemies
			.OrderBy(static enemy => enemy.CombatId ?? uint.MaxValue)
			.FirstOrDefault();
	}

	/// <summary>
	/// 跟随打出的牌选弹幕目标：全体牌打所有可命中敌人，单体牌打它的敌方目标；只保留存活敌人，按 CombatId 排序。
	/// </summary>
	internal static List<Creature> ResolveCardPlayEnemyTargets(CardPlay cardPlay, HextechCombatState combatState)
	{
		IEnumerable<Creature> targets = cardPlay.Card.TargetType == TargetType.AllEnemies
			? combatState.HittableEnemies
			: cardPlay.Target is { Side: CombatSide.Enemy } target
				? [target]
				: [];
		return targets
			.Where(static target => target.IsAlive && target.Side == CombatSide.Enemy)
			.OrderBy(static target => target.CombatId ?? uint.MaxValue)
			.ToList();
	}

	internal static Creature? PickRandomHittableEnemy(
		Player? owner,
		HextechCombatState? combatState,
		string scope,
		params string?[] saltParts)
	{
		if (owner == null || combatState == null)
		{
			return null;
		}

		List<Creature> enemies = combatState.HittableEnemies
			.OrderBy(static enemy => enemy.CombatId ?? uint.MaxValue)
			.ToList();
		if (enemies.Count == 0)
		{
			return null;
		}

		string?[] fullSalt = new string?[saltParts.Length + 2];
		fullSalt[0] = scope;
		fullSalt[1] = HextechStableRandom.PlayerKey(owner);
		Array.Copy(saltParts, 0, fullSalt, 2, saltParts.Length);

		return enemies[HextechStableRandom.Index((RunState)owner.RunState, enemies.Count, fullSalt)];
	}
}
