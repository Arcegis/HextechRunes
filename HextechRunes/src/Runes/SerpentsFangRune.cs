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

public sealed class SerpentsFangRune : HextechRelicBase
{
	private int _poisonApplicationsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPoisonApplicationsThisCombat
	{
		get => _poisonApplicationsThisCombat;
		set
		{
			_poisonApplicationsThisCombat = Math.Max(0, value);
			UpdateDisplay();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? Math.Max(0, DynamicVars["PoisonApplications"].IntValue - _poisonApplicationsThisCombat) : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<EnvenomPower>(1m),
		new DynamicVar("PoisonApplications", 2m),
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<EnvenomPower>(),
		HoverTipFactory.FromPower<PoisonPower>(),
		HoverTipFactory.FromCard<Shiv>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		ResetCounter();
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<EnvenomPower>(Owner.Creature, DynamicVars["EnvenomPower"].BaseValue, Owner.Creature, null);
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetCounter();
		return Task.CompletedTask;
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (power is not PoisonPower
			|| Owner == null
			|| Owner.Creature.IsDead
			|| !TryGetOwnedEnemyDebuffTarget(power, amount, applier, out Creature? target))
		{
			return;
		}

		_poisonApplicationsThisCombat++;
		int poisonApplicationsNeeded = DynamicVars["PoisonApplications"].IntValue;
		int shivsToCreate = 0;
		while (_poisonApplicationsThisCombat >= poisonApplicationsNeeded)
		{
			_poisonApplicationsThisCombat -= poisonApplicationsNeeded;
			shivsToCreate += DynamicVars.Cards.IntValue;
		}

		UpdateDisplay();
		if (shivsToCreate <= 0)
		{
			return;
		}

		Flash(target == null ? Array.Empty<Creature>() : [target]);
		await AddCardCopiesToCombatHand<Shiv>(shivsToCreate);
	}

	private void ResetCounter()
	{
		_poisonApplicationsThisCombat = 0;
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		Status = _poisonApplicationsThisCombat == DynamicVars["PoisonApplications"].IntValue - 1
			? RelicStatus.Active
			: RelicStatus.Normal;
		InvokeDisplayAmountChanged();
	}
}
