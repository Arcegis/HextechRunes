using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace HextechRunes;

public sealed class HappyAccidentRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbCount", 1m),
		new PowerVar<FocusPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
#else
	public override async Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
#endif
	{
#if STS2_104_OR_NEWER
		bool addedByPlayer = creator == Owner;
#endif
		if (!addedByPlayer
			|| card.Owner != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState == null
			|| card.Type != CardType.Status)
		{
			return;
		}

		Flash();
		PlayerChoiceContext choiceContext = new BlockingPlayerChoiceContext();
		for (int i = 0; i < DynamicVars["OrbCount"].IntValue; i++)
		{
			OrbModel orb = HextechStableRandom.CreateOrb(
				(RunState)Owner.RunState,
				Owner,
				"happy-accident-status-orb",
				CombatManager.Instance.History.Entries.Count() + i,
				Owner.Creature.CombatState.RoundNumber);
			await OrbCmd.Channel(choiceContext, orb, Owner);
		}

		await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, card);
	}
}
