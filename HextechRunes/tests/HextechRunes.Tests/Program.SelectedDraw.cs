using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes.Tests;

internal static partial class Program
{
	[HextechTest]
	private static void SelectedDrawRefillsQuotaTakenByNestedDraws()
	{
		CardModel[] cards = Enumerable.Range(0, 8).Select(static _ => (CardModel)CreateMutableTestModel<StrikeIronclad>()).ToArray();
		List<CardModel> drawPile = [.. cards];
		List<CardModel> hand = [];
		List<CardModel> triggered = [];
		int rounds = 0;

		Task<IReadOnlyList<CardModel>> SelectTop(int count)
		{
			rounds++;
			return Task.FromResult<IReadOnlyList<CardModel>>(drawPile.Take(count).ToList());
		}

		void MoveToHand(CardModel card)
		{
			drawPile.Remove(card);
			hand.Add(card);
		}

		// 抽到第一张时模拟升级自动化：嵌套抽走抽牌堆顶的两张（恰好是本轮已选的 B、C）。
		int drawnCount = HextechSelectedDrawHelper.DrawSelectedRounds(
			4,
			SelectTop,
			card => drawPile.Contains(card),
			() => hand.Count < 10,
			card =>
			{
				triggered.Add(card);
				MoveToHand(card);
				if (card == cards[0])
				{
					foreach (CardModel nested in drawPile.Take(2).ToList())
					{
						MoveToHand(nested);
					}
				}

				return Task.CompletedTask;
			}).GetAwaiter().GetResult();

		Equal(4, drawnCount, "selection quota is filled despite nested draws");
		Equal(6, hand.Count, "4 selected draws plus 2 nested draws");
		SequenceEqual(new[] { cards[0], cards[3], cards[4], cards[5] }, triggered, "cards taken by the nested draw are not drawn again");
		Equal(2, rounds, "shortfall is re-selected from the remaining draw pile");

		hand.Clear();
		drawPile.Clear();
		drawPile.AddRange(cards);
		int capped = HextechSelectedDrawHelper.DrawSelectedRounds(
			4,
			SelectTop,
			card => drawPile.Contains(card),
			() => hand.Count < 2,
			card => { MoveToHand(card); return Task.CompletedTask; }).GetAwaiter().GetResult();
		Equal(2, capped, "hand limit stops drawing");

		int empty = HextechSelectedDrawHelper.DrawSelectedRounds(
			3,
			_ => Task.FromResult<IReadOnlyList<CardModel>>(Array.Empty<CardModel>()),
			_ => true,
			() => true,
			_ => Task.CompletedTask).GetAwaiter().GetResult();
		Equal(0, empty, "an empty selection ends instead of looping");
	}
}
