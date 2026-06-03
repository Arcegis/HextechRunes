using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;

namespace HextechRunes;

internal static class HextechAutoPlayHelper
{
	internal static async Task AutoPlayOrMoveToResultPile(
		PlayerChoiceContext choiceContext,
		CardModel card,
		Creature? target,
		AutoPlayType type = AutoPlayType.Default,
		bool skipXCapture = false,
		bool skipCardPileVisuals = false)
	{
		try
		{
			await CardCmd.AutoPlay(choiceContext, card, target, type, skipXCapture, skipCardPileVisuals);
		}
		catch (Exception ex) when (IsKnownExternalAutoPlayCompatibilityFailure(ex))
		{
			Log.Warn($"[{ModInfo.Id}][AutoPlay] Skipped autoplay for {card.Id} after external compatibility failure: {ex.GetType().Name}: {ex.Message}");
			await MoveToResultPile(choiceContext, card);
		}
	}

	private static bool IsKnownExternalAutoPlayCompatibilityFailure(Exception ex)
	{
		return ex is TypeLoadException or MissingMethodException
			|| ex is AggregateException aggregate && aggregate.InnerExceptions.Any(IsKnownExternalAutoPlayCompatibilityFailure);
	}

	private static async Task MoveToResultPile(PlayerChoiceContext choiceContext, CardModel card)
	{
		await card.MoveToResultPileWithoutPlaying(choiceContext);
	}
}
