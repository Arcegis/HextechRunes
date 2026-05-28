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
		HextechEnemyHexContext context = new(this);
		foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
		{
			await effect.AfterPowerAmountChanged(context, power, amount, applier, cardSource);
		}

		bool hasMonsterDebuffTrigger = TryGetMonsterDebuffTrigger(power, amount, applier, out Creature? target, out Creature? source);
		bool suppressMonsterDebuffDuplicate = hasMonsterDebuffTrigger && ShouldSuppressMonsterDebuffDuplicate(power, amount, source, cardSource);
		if (hasMonsterDebuffTrigger && !suppressMonsterDebuffDuplicate)
		{
			foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
			{
				await effect.AfterMonsterDebuffApplied(context, power, amount, target!, source!, cardSource);
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

		if (hasCourageTrigger)
		{
			foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
			{
				await effect.AfterCourageTrigger(context, courageSource!);
			}
		}
	}
}
