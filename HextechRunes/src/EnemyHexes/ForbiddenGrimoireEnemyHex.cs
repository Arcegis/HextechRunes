namespace HextechRunes;

internal sealed class ForbiddenGrimoireEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ForbiddenGrimoire;

	internal override bool TryModifyCardRewardOptionsLate(HextechEnemyHexContext context, Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		if (player.RunState != context.RunState
			|| creationOptions.Source != CardCreationSource.Encounter
			|| cardRewardOptions.Count <= 1)
		{
			return false;
		}

		int removals = context.TierValue(Kind, 1, 1, 2);
		bool modified = false;
		while (removals > 0 && cardRewardOptions.Count > 1)
		{
			cardRewardOptions.RemoveAt(cardRewardOptions.Count - 1);
			removals--;
			modified = true;
		}

		return modified;
	}
}
