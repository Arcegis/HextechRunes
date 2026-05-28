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

public sealed class SoulCallingRune : HextechRelicBase
{
	private bool _addedThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Soul>()
	];

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedAddedThisCombat
	{
		get => _addedThisCombat;
		set => _addedThisCombat = value;
	}

	public override Task BeforeCombatStart()
	{
		_addedThisCombat = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_addedThisCombat = false;
		return Task.CompletedTask;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (_addedThisCombat || player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		_addedThisCombat = true;
		Flash();
		await AddSoulToPile(combatState, PileType.Draw);
		await AddSoulToPile(combatState, PileType.Hand);
		await AddSoulToPile(combatState, PileType.Discard);
	}

	private Task AddSoulToPile(HextechCombatState combatState, PileType pileType)
	{
		IEnumerable<Soul> souls = Soul.Create(Owner!, DynamicVars.Cards.IntValue, combatState);
		return HextechCardGeneration.AddGeneratedCardsToCombat(
			souls,
			pileType,
			addedByPlayer: true,
			position: CardPilePosition.Top);
	}
}
