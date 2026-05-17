using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

public sealed class OldIdolRune : HextechRelicBase
{
	private bool _secondTurnStrengthGranted;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedSecondTurnStrengthGranted
	{
		get => _secondTurnStrengthGranted;
		set => _secondTurnStrengthGranted = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(10m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override Task BeforeCombatStart()
	{
		_secondTurnStrengthGranted = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_secondTurnStrengthGranted = false;
		return Task.CompletedTask;
	}

	public override decimal ModifyHandDrawLate(Player player, decimal count)
	{
		return player == Owner && player.Creature.CombatState?.RoundNumber == 1 ? 0m : count;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || _secondTurnStrengthGranted || combatState.RoundNumber != 2)
		{
			return;
		}

		_secondTurnStrengthGranted = true;
		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
	}
}
