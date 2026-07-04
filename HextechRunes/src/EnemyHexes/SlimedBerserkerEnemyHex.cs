using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

internal sealed class SlimedBerserkerEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SlimedBerserker;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		// "战斗开始时":首个玩家回合开始只发生一次,牌堆此时已就绪。
		if (combatState.RoundNumber != 1)
		{
			return;
		}

		int count = context.TierValue(Kind, 5, 10, 15);
		foreach (Player player in players
			.Where(static creature => !creature.IsDead)
			.Select(static creature => creature.Player)
			.OfType<Player>()
			.OrderBy(static player => player.NetId))
		{
			for (int i = 0; i < count; i++)
			{
				CardModel slimed = combatState.CreateCard<Slimed>(player);
				await HextechCardGeneration.AddGeneratedCardToCombat(
					slimed,
					PileType.Discard,
					addedByPlayer: false,
					CardPilePosition.Top);
			}
		}
	}
}
