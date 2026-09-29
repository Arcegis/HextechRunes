using MegaCrit.Sts2.Core.Helpers;

namespace HextechRunes;

public sealed class TwinFlamesRune : HextechRelicBase
{
	internal const int MissileCount = 3;

	private int _targetRollsThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Missiles", MissileCount)
	];

	public override Task BeforeCombatStart()
	{
		_targetRollsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_targetRollsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !IsOwnedSkill(cardPlay.Card)
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return Task.CompletedTask;
		}

		Creature source = Owner.Creature;
		decimal damage = ResolveMissileDamage(HextechCombatHooks.GetEnergyCostForCurrentCardPlay(cardPlay.Card));
		if (!ShouldLaunchMissiles(damage))
		{
			return Task.CompletedTask;
		}

		int targetOrdinal = ConsumeCombatProcOrdinal(nameof(TwinFlamesRune), ref _targetRollsThisCombat);
		string cardKey = HextechStableRandom.CardKey(cardPlay.Card);
		Creature? target = HextechRuneTargeting.PickRandomHittableEnemy(
			Owner,
			combatState,
			"twin-flames-target",
			combatState.RoundNumber.ToString(),
			targetOrdinal.ToString(),
			cardKey);
		if (target == null)
		{
			return Task.CompletedTask;
		}

		Flash([target]);
		_ = TaskHelper.RunSafely(PlayVolleyVfxAsync(source, target));
		return ResolveVolleyDamageInLockstepAsync(choiceContext, source, combatState, target, damage);
	}

	private static async Task PlayVolleyVfxAsync(Creature source, Creature target)
	{
		await Task.WhenAll(Enumerable.Range(0, MissileCount)
			.Select(missileIndex => HextechCombatVfx.PlayTwinFlamesMissile(source, target, missileIndex)));
	}

	private static async Task ResolveVolleyDamageInLockstepAsync(
		PlayerChoiceContext choiceContext,
		Creature source,
		HextechCombatState combatState,
		Creature target,
		decimal damage)
	{
		// 伤害留在当前卡牌动作内结算（单人与联机同一路径）；弹道只做视觉，不能在独立任务中稍后改血量。
		for (int missileIndex = 0; missileIndex < MissileCount; missileIndex++)
		{
			if (source.IsDead
				|| !target.IsAlive
				|| !ReferenceEquals(source.CombatState, combatState)
				|| !ReferenceEquals(target.CombatState, combatState))
			{
				return;
			}

			await HextechGameApiCompat.Damage(
				choiceContext,
				target,
				damage,
				ValueProp.Unpowered,
				source,
				null);
		}
	}

	internal static decimal ResolveMissileDamage(decimal energyCost)
	{
		return Math.Max(0m, energyCost);
	}

	internal static bool ShouldLaunchMissiles(decimal damage)
	{
		return damage > 0m;
	}
}
