using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
#if STS2_104_OR_NEWER
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (power is MinionPower && amount > 0m)
		{
			await TryApplyServantMasterIllusion(power.Owner, applier, cardSource);
		}

		bool hasMonsterDebuffTrigger = TryGetMonsterDebuffTrigger(power, amount, applier, out Creature? target, out Creature? source);
		bool suppressMonsterDebuffDuplicate = hasMonsterDebuffTrigger && ShouldSuppressMonsterDebuffDuplicate(power, amount, source, cardSource);
		if (hasMonsterDebuffTrigger && !suppressMonsterDebuffDuplicate)
		{
			if (HasActiveMonsterHex(MonsterHexKind.Slap)
				&& TryConsumeLimitedProc(_combatTracking.SlapProcsThisTurn, source!, 3))
			{
				await PowerCmd.Apply<StrengthPower>(source!, 1m, source, null);
			}

			if (HasActiveMonsterHex(MonsterHexKind.Tormentor)
				&& !_combatTracking.HandlingMonsterTormentorBurn
				&& TryConsumeLimitedProc(_combatTracking.TormentorProcsThisTurn, source!, 5))
			{
				try
				{
					_combatTracking.HandlingMonsterTormentorBurn = true;
					await PowerCmd.Apply<HextechBurnPower>(target!, 2m, source, null);
				}
				finally
				{
					_combatTracking.HandlingMonsterTormentorBurn = false;
				}
			}
		}

		Creature? courageSource = null;
		bool hasCourageTrigger = false;
		if (hasMonsterDebuffTrigger && !suppressMonsterDebuffDuplicate)
		{
			courageSource = source;
			hasCourageTrigger = courageSource != null;
		}
		else if (TryGetMonsterSelfBuffTrigger(power, amount, applier, out Creature? buffSource))
		{
			courageSource = buffSource;
			hasCourageTrigger = true;
		}

		if (HasActiveMonsterHex(MonsterHexKind.CourageOfColossus)
			&& hasCourageTrigger
			&& TryConsumeLimitedProc(_combatTracking.CourageProcsThisTurn, courageSource!, 1))
		{
			int plating = Math.Max(1, (int)Math.Floor(courageSource!.MaxHp * CourageOfColossusPlatingPercent));
			await HextechEnemyPowerScalingHooks.Apply<PlatingPower>(courageSource, plating, courageSource, null);
		}
	}
}
