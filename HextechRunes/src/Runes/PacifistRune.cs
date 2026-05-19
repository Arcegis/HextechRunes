using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class PacifistRune : HextechRelicBase
{
	private readonly Dictionary<uint, decimal> _pendingDoomByTarget = new();
	private int _replacementDoomApplicationDepth;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("SustainMultiplier", 1.3m),
		new PowerVar<DoomPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<DoomPower>()
	];

	public decimal SustainMultiplier => DynamicVars["SustainMultiplier"].BaseValue;

	public override Task BeforeCombatStart()
	{
		_pendingDoomByTarget.Clear();
		_replacementDoomApplicationDepth = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingDoomByTarget.Clear();
		_replacementDoomApplicationDepth = 0;
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		_pendingDoomByTarget.Clear();
		return Task.CompletedTask;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (_replacementDoomApplicationDepth > 0)
		{
			return 0m;
		}

		if (Owner == null || target?.Side != CombatSide.Enemy || amount <= 0m || !IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		if (target.CombatId is uint combatId)
		{
			_pendingDoomByTarget[combatId] = Math.Max(_pendingDoomByTarget.GetValueOrDefault(combatId), amount);
		}

		return 0m;
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null || target.Side != CombatSide.Enemy || target.CombatId is not uint combatId)
		{
			return;
		}

		if (!_pendingDoomByTarget.Remove(combatId, out decimal doom) || doom <= 0m)
		{
			return;
		}

		Flash([target]);
		await ApplyReplacementDoom(target, Math.Max(1, Math.Floor(doom)), cardSource);
	}

	private async Task ApplyReplacementDoom(Creature target, decimal amount, CardModel? cardSource)
	{
		// Prevent damage-on-debuff effects, such as Sleight of Flesh, from escaping Pacifist or recursively becoming more Doom.
		_replacementDoomApplicationDepth++;
		try
		{
			await PowerCmd.Apply<DoomPower>(target, amount, Owner!.Creature, cardSource);
		}
		finally
		{
			_replacementDoomApplicationDepth--;
		}
	}
}
