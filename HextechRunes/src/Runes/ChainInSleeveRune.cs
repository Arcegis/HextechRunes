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

public sealed class ChainInSleeveRune : HextechRelicBase
{
	private const int ShivsNeeded = 3;

	private int _shivsPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedShivsPlayedThisCombat
	{
		get => GetShivsPlayedThisCombat() % ShivsNeeded;
		set
		{
			_shivsPlayedThisCombat = Math.Max(0, value) % ShivsNeeded;
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			int remainder = GetShivsPlayedThisCombat() % ShivsNeeded;
			return remainder == 0 ? ShivsNeeded : ShivsNeeded - remainder;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ShivsNeeded", ShivsNeeded),
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Shiv>(),
		HoverTipFactory.FromCard<SovereignBlade>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsCountedShivPlay(cardPlay))
		{
			return;
		}

		if (ShouldUseNetworkCombatHistory())
		{
			await ResolveShivProgressFromHistory();
			return;
		}

		_shivsPlayedThisCombat++;
		await ResolveShivRewards(previousShivsPlayed: _shivsPlayedThisCombat - 1, currentShivsPlayed: _shivsPlayedThisCombat);
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (ShouldUseNetworkCombatHistory() && IsCountedShivPlay(cardPlay))
		{
			await ResolveShivProgressFromHistory();
		}
	}

	private async Task ResolveShivProgressFromHistory()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int shivsPlayed = CountOwnedShivCardsPlayedFromHistory();
		int previousShivsPlayed = _shivsPlayedThisCombat;
		if (shivsPlayed <= previousShivsPlayed)
		{
			return;
		}

		_shivsPlayedThisCombat = shivsPlayed;
		await ResolveShivRewards(previousShivsPlayed, shivsPlayed);
	}

	private async Task ResolveShivRewards(int previousShivsPlayed, int currentShivsPlayed)
	{
		InvokeDisplayAmountChanged();
		int rewards = currentShivsPlayed / ShivsNeeded - previousShivsPlayed / ShivsNeeded;
		if (rewards <= 0 || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await AddShivRewardCards(rewards * DynamicVars.Cards.IntValue);
	}

	private bool IsCountedShivPlay(CardPlay cardPlay)
	{
		return cardPlay.IsFirstInSeries
			&& !cardPlay.IsAutoPlay
			&& cardPlay.Card.Owner == Owner
			&& HextechKnifeHelper.IsShivLike(cardPlay.Card, Owner);
	}

	private int CountOwnedShivCardsPlayedFromHistory()
	{
		if (Owner == null)
		{
			return 0;
		}

		ulong ownerId = Owner.NetId;
		return CombatManager.Instance.History.Entries
			.OfType<CardPlayFinishedEntry>()
			.Count(entry =>
				entry.CardPlay.IsFirstInSeries
				&& !entry.CardPlay.IsAutoPlay
				&& entry.CardPlay.Card.Owner?.NetId == ownerId
				&& HextechKnifeHelper.IsShivLike(entry.CardPlay.Card, Owner));
	}

	private async Task AddShivRewardCards(int count)
	{
		if (Owner?.GetRelic<BigKnifeRune>() != null)
		{
			await AddCardCopiesToCombatHand<SovereignBlade>(count, HextechKnifeHelper.ConfigureBigKnifeBlade);
			return;
		}

		await AddCardCopiesToCombatHand<Shiv>(count);
	}

	private int GetShivsPlayedThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedShivCardsPlayedFromHistory()
			: _shivsPlayedThisCombat;
	}

	private void ResetCounter()
	{
		_shivsPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}
