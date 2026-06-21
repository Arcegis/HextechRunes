using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		return HextechEnemyHexDispatcher.AnyModified(
			this,
			(effect, context) => effect.TryModifyRewards(context, player, rewards, room));
	}

	public override Task AfterGoldGained(Player player)
	{
		return HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterGoldGained(context, player));
	}

	public override bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward)
	{
		return HextechEnemyHexDispatcher.AnyModified(
			this,
			(effect, context) => effect.ShouldAllowSelectingMoreCardRewards(context, player, cardReward));
	}

	public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
	{
		return HextechEnemyHexDispatcher.Transform(
			this,
			options,
			(effect, context, current) => effect.ModifyCardRewardCreationOptions(context, player, current));
	}

	public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		return HextechEnemyHexDispatcher.AnyModified(
			this,
			(effect, context) => effect.TryModifyCardRewardOptions(context, player, cardRewardOptions, creationOptions));
	}

	public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		return HextechEnemyHexDispatcher.AnyModified(
			this,
			(effect, context) => effect.TryModifyCardRewardOptionsLate(context, player, cardRewardOptions, creationOptions));
	}
}
