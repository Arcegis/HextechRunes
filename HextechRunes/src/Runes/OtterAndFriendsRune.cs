using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class OtterAndFriendsRune : HextechRelicBase
{
	private int _cycleIndex;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCycleIndex
	{
		get => _cycleIndex;
		set => _cycleIndex = Math.Max(0, value);
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new EnergyVar(1),
		new PowerVar<StrengthPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<BelieveInYou>(),
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNetworkMultiplayer();
	}

	public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		List<Player> players = combatState.Players
			.Where(static combatPlayer => combatPlayer.Creature.IsAlive)
			.OrderBy(static combatPlayer => combatPlayer.NetId)
			.ToList();
		if (players.Count == 0)
		{
			return;
		}

		await AddCardCopiesToCombatHand<BelieveInYou>(1);
		int cycle = _cycleIndex % 3;
		SavedCycleIndex = _cycleIndex + 1;
		FlashDeferred(players.Select(static combatPlayer => combatPlayer.Creature));
		switch (cycle)
		{
			case 0:
				foreach (Player combatPlayer in players)
				{
					await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, combatPlayer, fromHandDraw: false);
				}
				break;
			case 1:
				foreach (Player combatPlayer in players)
				{
					await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, combatPlayer);
				}
				break;
			default:
				foreach (Player combatPlayer in players)
				{
					await PowerCmd.Apply<StrengthPower>(combatPlayer.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
				}
				break;
		}
	}
}
