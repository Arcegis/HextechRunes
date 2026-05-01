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

namespace HextechRunes;

public abstract class HextechForgeBase : HextechRelicBase
{
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedStackCount
	{
		get => StackCount;
		set
		{
			int target = Math.Max(1, value);
			while (StackCount < target)
			{
				IncrementStackCount();
			}

			InvokeDisplayAmountChanged();
		}
	}

	public override bool IsStackable => true;

	public override bool ShowCounter => true;

	public override int DisplayAmount => !IsCanonical ? StackCount : 0;

	protected int StackAmount => Math.Max(1, StackCount);

	protected decimal StackMultiplier => StackAmount;

	protected decimal Stacked(decimal value)
	{
		return value * StackMultiplier;
	}

	protected decimal StackedMultiplier(decimal value)
	{
		if (StackAmount <= 1)
		{
			return value;
		}

		return (decimal)Math.Pow((double)value, StackAmount);
	}

	public void AddForgeStack(bool flash = true)
	{
		IncrementStackCount();
		InvokeDisplayAmountChanged();
		if (flash)
		{
			Flash();
		}
	}
}
