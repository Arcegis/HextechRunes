using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class MiseryUpgradeRune : CardUpgradeRuneBase<Misery>
{
	private bool _isDoublingDebuffs;

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (_isDoublingDebuffs
			|| Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Misery
			|| cardPlay.Target == null
			|| cardPlay.Target.Side == Owner.Creature.Side
			|| Owner.Creature.CombatState == null)
		{
			return;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count != 1 || enemies[0] != cardPlay.Target)
		{
			return;
		}

		List<PowerModel> debuffs = cardPlay.Target.Powers
			.Where(static power => power.Amount > 0m && power.TypeForCurrentAmount == PowerType.Debuff)
			.Select(static power => (PowerModel)power.ClonePreservingMutability())
			.ToList();
		if (debuffs.Count == 0)
		{
			return;
		}

		_isDoublingDebuffs = true;
		try
		{
			Flash([cardPlay.Target]);
			foreach (PowerModel debuff in debuffs)
			{
#if !STS2_108_OR_NEWER
				// 0.108.0 起 ITemporaryPower.IgnoreNextInstance 被移除(临时 power 的实例追踪
				// 疑似改为引擎侧自动处理);beta 期观察复制的临时型 debuff 衰减是否正常。
				if (debuff is ITemporaryPower temporaryPower)
				{
					temporaryPower.IgnoreNextInstance();
				}
#endif

				await HextechPowerCmdCompat.Apply(debuff, cardPlay.Target, debuff.Amount, Owner.Creature, cardPlay.Card);
			}
		}
		finally
		{
			_isDoublingDebuffs = false;
		}
	}
}
