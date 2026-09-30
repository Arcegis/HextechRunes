using MegaCrit.Sts2.Core.Helpers;

namespace HextechRunes;

public sealed class LightEmUpRune : HextechRelicBase
{
	internal const int AttacksPerVolley = 4;
	internal const int MissileCount = 6;

	private int _attacksPlayedThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Attacks", AttacksPerVolley),
		new DynamicVar("Missiles", MissileCount)
	];

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisCombat
	{
		get => _attacksPlayedThisCombat;
		set
		{
			_attacksPlayedThisCombat = Math.Clamp(value, 0, AttacksPerVolley);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => IsInLiveCombat;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			return _attacksPlayedThisCombat;
		}
	}

	public override Task BeforeCombatStart()
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		// 计数口径同原版苦无（Kunai）：持有者攻击牌的每一次打出都计 1，含重放（PlayIndex > 0）与自动打出。
		if (!IsOwnedAttack(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		decimal damage = HextechMissileVolley.DamageFromEnergyCost(HextechCombatHooks.GetEnergyCostForCurrentCardPlay(cardPlay.Card));
		_attacksPlayedThisCombat = AdvanceAttackProgress(
			_attacksPlayedThisCombat,
			damage,
			out bool shouldLaunchVolley);
		InvokeDisplayAmountChanged();
		if (!shouldLaunchVolley
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return Task.CompletedTask;
		}

		List<Creature> targets = HextechRuneTargeting.ResolveCardPlayEnemyTargets(cardPlay, combatState);
		if (targets.Count == 0)
		{
			return Task.CompletedTask;
		}

		Flash(targets);
		Creature source = Owner.Creature;
		_ = TaskHelper.RunSafely(HextechMissileVolley.PlayVfxAsync(source, targets, MissileCount, HextechCombatVfx.PlayTwinFlamesMissile));
		return HextechMissileVolley.ResolveVolleyDamageInLockstepAsync(choiceContext, source, combatState, targets, MissileCount, _ => damage);
	}

	private void ResetAttacksPlayedThisCombat()
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
	}

	internal static int AdvanceAttackProgress(int currentProgress, decimal energyCost, out bool shouldLaunchVolley)
	{
		int progress = Math.Clamp(currentProgress, 0, AttacksPerVolley);
		if (progress < AttacksPerVolley)
		{
			progress++;
		}

		shouldLaunchVolley = progress == AttacksPerVolley && energyCost > 0m;
		return shouldLaunchVolley ? 0 : progress;
	}
}
