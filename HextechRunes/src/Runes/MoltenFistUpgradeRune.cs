using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

// 升级：熔融之拳(仅战士) —— 打出熔融之拳后,在手牌中添加 1 张主宰(Dominate)。
public sealed class MoltenFistUpgradeRune : CardUpgradeRuneBase<MoltenFist>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<MoltenFist>(),
		HoverTipFactory.FromCard<Dominate>()
	];

	protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not MoltenFist
			|| cardPlay.Card.CombatState is not { } combatState)
		{
			return;
		}

		Flash();
		CardModel dominate = combatState.CreateCard<Dominate>(Owner);
		await HextechCardGeneration.AddGeneratedCardToCombat(dominate, PileType.Hand, addedByPlayer: true);
	}
}
