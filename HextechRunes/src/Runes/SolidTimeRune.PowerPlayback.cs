namespace HextechRunes;

public sealed partial class SolidTimeRune
{
	// 原版 protected virtual CardModel.OnPlay(PlayerChoiceContext, CardPlay)(0.107.1~0.111.0)。经基类 MethodInfo 反射调用
	// 按虚分派进入各卡的覆写，无需沿继承链逐级查找；缺失时进启动摘要，本次回放跳过 OnPlay。
	private static readonly MethodInfo? CardOnPlayMethod = HextechHookReflection.TryGetMethod(
		typeof(CardModel),
		"OnPlay",
		BindingFlags.Instance | BindingFlags.NonPublic,
		typeof(PlayerChoiceContext),
		typeof(CardPlay));

	private Creature? PickTarget(CardModel card, HextechCombatState combatState, int index)
	{
		return card.TargetType switch
		{
			TargetType.AnyEnemy => HextechRuneTargeting.PickRandomHittableEnemy(
				Owner,
				combatState,
				"solid-time",
				combatState.RoundNumber.ToString(),
				index.ToString(),
				card.Id.Entry),
			TargetType.AnyAlly => Owner?.Creature,
			TargetType.AnyPlayer => Owner?.Creature,
			_ => null
		};
	}

	private static async Task ApplyStoredPowerDirectly(PlayerChoiceContext choiceContext, CardModel card, Creature? target)
	{
		bool addedToTemporaryPlayPile = false;
		if (card.Pile == null)
		{
			await CardPileCmd.Add(card, PileType.Play, skipVisuals: true);
			addedToTemporaryPlayPile = card.Pile?.Type == PileType.Play;
			if (!addedToTemporaryPlayPile)
			{
				HextechLog.Warn("SolidTime", $"Skipped stored power without combat pile: card={card.Id}");
				return;
			}
		}

		CardPlay cardPlay = HextechRuneApiCompat.CreateFreeAutoPlay(card, target);

		choiceContext.PushModel(card);
		try
		{
			if (!await TryApplySolidTimeSpecialCase(card))
			{
				if (CardOnPlayMethod == null)
				{
					HextechLog.Warn("SolidTime", $"Skipped stored power OnPlay because CardModel.OnPlay is unavailable: card={card.Id}");
				}
				else if (CardOnPlayMethod.Invoke(card, [choiceContext, cardPlay]) is Task onPlay)
				{
					await onPlay;
				}
			}

			if (!card.Owner.Creature.IsDead)
			{
				card.InvokeExecutionFinished();
			}
		}
		finally
		{
			choiceContext.PopModel(card);
			if (addedToTemporaryPlayPile && card.Pile?.IsCombatPile == true)
			{
				await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
			}
		}
	}

	private static async Task<bool> TryApplySolidTimeSpecialCase(CardModel card)
	{
		if (card is VoidForm)
		{
			await PowerCmd.Apply<VoidFormPower>(
				card.Owner.Creature,
				card.DynamicVars["VoidFormPower"].BaseValue,
				card.Owner.Creature,
				card);
			return true;
		}

		return false;
	}
}
