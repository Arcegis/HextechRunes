using MegaCrit.Sts2.Core.Helpers;

namespace HextechRunes;

public sealed class MagicMissileRune : TurnScopedRelicBase
{
	internal const int MissileCount = 3;
	internal const decimal MaxHpDamagePercent = 3m;

	private bool _triggeredThisTurn;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Missiles", MissileCount),
		new DynamicVar("MaxHpDamagePercent", MaxHpDamagePercent)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		EnsureTurnScopedStateCurrent();
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !cardPlay.IsFirstInSeries
			|| !IsOwnedAttack(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		HextechCombatState? combatState = Owner.Creature.CombatState;
		List<Creature> targets = ResolveTargets(cardPlay, combatState);
		if (combatState == null || targets.Count == 0
			|| !TryConsumeTurnProc(nameof(MagicMissileRune), ref _triggeredThisTurn))
		{
			return Task.CompletedTask;
		}

		Flash(targets);
		Creature source = Owner.Creature;
		decimal damagePercent = DynamicVars["MaxHpDamagePercent"].BaseValue;
		_ = TaskHelper.RunSafely(PlayVolleyVfxAsync(source, targets));
		return ResolveVolleyDamageInLockstepAsync(choiceContext, source, combatState, targets, damagePercent);
	}

	private static async Task PlayVolleyVfxAsync(Creature source, IReadOnlyList<Creature> targets)
	{
		await Task.WhenAll(Enumerable.Range(0, MissileCount)
			.SelectMany(missileIndex => targets
				.Select(target => HextechCombatVfx.PlayMagicMissile(source, target, missileIndex))));
	}

	private static async Task ResolveVolleyDamageInLockstepAsync(
		PlayerChoiceContext choiceContext,
		Creature source,
		HextechCombatState combatState,
		IReadOnlyList<Creature> targets,
		decimal damagePercent)
	{
		for (int missileIndex = 0; missileIndex < MissileCount; missileIndex++)
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
					CalculateMissileDamage(target.MaxHp, damagePercent),
					ValueProp.Unpowered,
					source,
					null);
			}
		}
	}

	internal static int CalculateMissileDamage(decimal targetMaxHp, decimal damagePercent = MaxHpDamagePercent)
	{
		return Math.Max(1, FloorToInt(Math.Max(0m, targetMaxHp) * Math.Max(0m, damagePercent) / 100m));
	}

	private List<Creature> ResolveTargets(CardPlay cardPlay, HextechCombatState? combatState)
	{
		if (combatState == null)
		{
			return [];
		}

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

	protected override void ResetTurnScopedState()
	{
		_triggeredThisTurn = false;
	}
}
