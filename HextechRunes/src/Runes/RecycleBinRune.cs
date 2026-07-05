namespace HextechRunes;

public sealed class RecycleBinRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
	{
		if (isInitialShuffle || player != Owner || Owner == null)
		{
			return;
		}

		int removed = cards.RemoveAll(static card => card.Type == CardType.Status && card.Pile?.Type == PileType.Discard);
		if (removed > 0)
		{
			Flash();
		}
	}
}
