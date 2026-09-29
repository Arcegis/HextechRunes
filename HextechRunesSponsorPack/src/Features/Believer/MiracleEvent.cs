using HextechRunes;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunesSponsorPack;

// 「神迹」事件:信徒海克斯触发。三级分支(大类→献祭/索取→强度1/2/3)。游戏原生 EventModel + SetEventState 多屏导航,
// 自动被 ModelDb 发现、不进随机事件池;立绘由 MiracleEventPortraitPatch 处理,锻造器售价修正由 BelieverRune 登记给本体。
//
// 金币消耗用 PlayerCmd.LoseGold(GainGold 负数不扣);金币不足/无对应稀有度卡时把选项锁定(onChosen=null)。
public sealed class MiracleEvent : EventModel
{
	internal const string PortraitPath = "res://images/events/doors_of_light_and_dark.png";

	private const string InitialPage = "INITIAL";
	private const string EventLocTable = "events";

	// 大类与献祭/索取的键同时是本地化页面/选项键的一部分(events.json:MIRACLE_EVENT.pages.FORGE_ROOT 等),只能保持字符串。
	private const string CategoryForge = "FORGE";
	private const string CategoryGold = "GOLD";
	private const string CategoryCard = "CARD";
	private const string CategoryGift = "GIFT";
	private const string ActionSacrifice = "SACRIFICE";
	private const string ActionTake = "TAKE";

	private const int GiftTakeGold = 50;
	private const int CardPackSize = 3;
	private const int PercentRollRange = 100;
	private const int GiftMaxGold = 100;

	// 每档(T1/T2/T3)数值只写这一处:可选性判断与实际结算都读它。
	private static readonly MiracleTierValues[] TierValues =
	[
		new(ForgeSacrificeCost: 100, ForgeTakePriceDelta: 50, GoldAmount: 100, GoldSacrificePriceDelta: -25, GoldTakePriceDelta: 25,
			CardSacrificeGold: 25, CardPackCost: 25, GiftLotteryCost: 25, CardRarity: CardRarity.Common),
		new(ForgeSacrificeCost: 200, ForgeTakePriceDelta: 100, GoldAmount: 200, GoldSacrificePriceDelta: -50, GoldTakePriceDelta: 25,
			CardSacrificeGold: 50, CardPackCost: 50, GiftLotteryCost: 50, CardRarity: CardRarity.Uncommon),
		new(ForgeSacrificeCost: 300, ForgeTakePriceDelta: 150, GoldAmount: 400, GoldSacrificePriceDelta: -100, GoldTakePriceDelta: 100,
			CardSacrificeGold: 75, CardPackCost: 50, GiftLotteryCost: 75, CardRarity: CardRarity.Rare)
	];

	public override IEnumerable<string> GetAssetPaths(IRunState runState)
	{
		yield return PortraitPath;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return
		[
			Choice(InitialPage, CategoryForge, () => ShowCategoryRoot(CategoryForge)),
			Choice(InitialPage, CategoryGold, () => ShowCategoryRoot(CategoryGold)),
			Choice(InitialPage, CategoryCard, () => ShowCategoryRoot(CategoryCard)),
			Choice(InitialPage, CategoryGift, () => ShowCategoryRoot(CategoryGift)),
		];
	}

	private IReadOnlyList<EventOption> ActionOptions(string category)
	{
		string root = RootPage(category);
		return
		[
			Choice(root, ActionSacrifice, () => Show(ActionPage(category, sacrifice: true), StrengthOptions(category, sacrifice: true))),
			Choice(root, ActionTake, () => Show(ActionPage(category, sacrifice: false), StrengthOptions(category, sacrifice: false))),
			Back(ShowInitial),
		];
	}

	private IReadOnlyList<EventOption> GiftRootOptions()
	{
		string root = RootPage(CategoryGift);
		return
		[
			Choice(root, ActionSacrifice, () => Show(ActionPage(CategoryGift, sacrifice: true), StrengthOptions(CategoryGift, sacrifice: true))),
			Choice(root, ActionTake, () => ApplyAndFinish(ActionPage(CategoryGift, sacrifice: false), _ => GainGold(GiftTakeGold))),
			Back(ShowInitial),
		];
	}

	// 三档强度选项;不满足条件(金币不足 / 无对应稀有度卡)的档位锁定(onChosen=null → IsLocked)。
	private IReadOnlyList<EventOption> StrengthOptions(string category, bool sacrifice)
	{
		string page = ActionPage(category, sacrifice);
		List<EventOption> options = [];
		for (int tier = 1; tier <= TierValues.Length; tier++)
		{
			int t = tier;
			Func<Task>? action = IsLeafAvailable(category, sacrifice, t)
				? () => ApplyAndFinish(page, owner => ApplyLeaf(category, sacrifice, t, owner))
				: null;
			options.Add(new EventOption(this, action, $"{Id.Entry}.pages.{page}.options.T{t}"));
		}

		options.Add(Back(() => ShowCategoryRoot(category)));
		return options;
	}

	private bool IsLeafAvailable(string category, bool sacrifice, int tier)
	{
		int gold = Owner?.Gold ?? 0;
		MiracleTierValues values = ValuesFor(tier);
		return category switch
		{
			CategoryForge => !sacrifice || gold >= values.ForgeSacrificeCost,
			CategoryGold => !sacrifice || gold >= values.GoldAmount,
			CategoryCard => sacrifice ? DeckHasSacrificeCard(tier) : gold >= values.CardPackCost,
			CategoryGift => !sacrifice || gold >= values.GiftLotteryCost,
			_ => true,
		};
	}

	private async Task ApplyLeaf(string category, bool sacrifice, int tier, Player owner)
	{
		switch (category)
		{
			case CategoryForge:
				await ApplyForge(owner, sacrifice, tier);
				break;
			case CategoryGold:
				await ApplyGold(sacrifice, tier);
				break;
			case CategoryCard:
				await ApplyCard(owner, sacrifice, tier);
				break;
			case CategoryGift:
				await ApplyGiftLottery(owner, tier);
				break;
		}
	}

	// 锻造器:献祭 花 100/200/300 金币换 tier 个锻造器;索取 直取 tier 个 + 本局售价 +50/+100/+150。
	private async Task ApplyForge(Player owner, bool sacrifice, int tier)
	{
		MiracleTierValues values = ValuesFor(tier);
		if (sacrifice)
		{
			await SpendGold(values.ForgeSacrificeCost);
		}
		else
		{
			AddForgePriceDelta(values.ForgeTakePriceDelta);
		}

		// 逐个发放,每个锻造器稀有度按 65/25/10(银/金/棱彩)加权随机,而非清一色白银。
		for (int i = 0; i < tier; i++)
		{
			await HextechRunesApi.ObtainRandomForges(
				owner, RandomForgeRarity(owner, $"forge:{tier}:{i}"), 1, static _ => true, $"{ModInfo.Id}.miracle.forge");
		}
	}

	// 金币:献祭 花 100/200/400 金币、本局售价 -25/-50/-100;索取 得 100/200/400 金币、本局售价 +25/+25/+100。
	private async Task ApplyGold(bool sacrifice, int tier)
	{
		MiracleTierValues values = ValuesFor(tier);
		if (sacrifice)
		{
			AddForgePriceDelta(values.GoldSacrificePriceDelta);
			await SpendGold(values.GoldAmount);
		}
		else
		{
			AddForgePriceDelta(values.GoldTakePriceDelta);
			await GainGold(values.GoldAmount);
		}
	}

	// 卡牌:献祭 移除 1 张对应稀有度的牌(普通/罕见/稀有)+ 25/50/75 金币;索取 花 25/50/50 金币开 1 只卡包。
	private async Task ApplyCard(Player owner, bool sacrifice, int tier)
	{
		MiracleTierValues values = ValuesFor(tier);
		if (sacrifice)
		{
			List<CardModel> ofTier = owner.Deck.Cards.Where(card => MatchesSacrificeTier(card, tier)).ToList();
			if (ofTier.Count == 0)
			{
				return;
			}

			CardModel target = ofTier[StableIndex(owner, ofTier.Count, "miracle.sacrifice", tier.ToString())];
			await CardPileCmd.RemoveFromDeck(target);
			await GainGold(values.CardSacrificeGold);
		}
		else
		{
			await SpendGold(values.CardPackCost);
			await GrantCardPack(owner, tier);
		}
	}

	// 卡包(ScrollBoxes 式):把 3 张对应稀有度的牌装进 1 只卡包,玩家点开即获得这 3 张。
	private async Task GrantCardPack(Player owner, int tier)
	{
		CardRarity rarity = ValuesFor(tier).CardRarity;
		List<CardModel> candidates = owner.Character.CardPool.AllCards
			.Where(card => card.Rarity == rarity)
			.ToList();

		List<CardModel> pack = [];
		for (int i = 0; i < CardPackSize && candidates.Count > 0; i++)
		{
			int idx = StableIndex(owner, candidates.Count, "miracle.cardpack", tier.ToString(), i.ToString());
			// 必须 CreateCard 从卡池模板实例化(canonical 不能直接用,否则 CanonicalModelException)。
			pack.Add(owner.RunState.CreateCard(candidates[idx], owner));
			candidates.RemoveAt(idx);
		}

		if (pack.Count == 0)
		{
			return;
		}

		List<IReadOnlyList<CardModel>> bundles = [pack];
		foreach (CardModel chosen in await CardSelectCmd.FromChooseABundleScreen(owner, bundles))
		{
			await CardPileCmd.Add(chosen, PileType.Deck);
		}
	}

	// 礼盒抽奖:花 25/50/75 金币,抽 tier 次。奖池 10%锻造器 / 10%锻造器售价-25 / 10%删 1 张牌 / 70%金币。
	private async Task ApplyGiftLottery(Player owner, int tier)
	{
		await SpendGold(ValuesFor(tier).GiftLotteryCost);
		for (int i = 0; i < tier; i++)
		{
			int roll = StableIndex(owner, PercentRollRange, "miracle.gift", tier.ToString(), i.ToString());
			if (roll < 10)
			{
				await HextechRunesApi.ObtainRandomForges(owner, RandomForgeRarity(owner, $"gift:{tier}:{i}"), 1, static _ => true, $"{ModInfo.Id}.miracle.gift.forge");
			}
			else if (roll < 20)
			{
				AddForgePriceDelta(-25);
			}
			else if (roll < 30)
			{
				List<CardModel> removed = (await CardSelectCmd.FromDeckForRemoval(owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
				if (removed.Count > 0)
				{
					await CardPileCmd.RemoveFromDeck(removed);
				}
			}
			else
			{
				await GainGold(1 + StableIndex(owner, GiftMaxGold, "miracle.gift.gold", tier.ToString(), i.ToString()));
			}
		}
	}

	// ---- 小工具 ----

	// 锻造器稀有度加权随机:65% 白银 / 25% 黄金 / 10% 棱彩。数值等于本体默认锻造器权重
	// (HextechRuneConfiguration.DefaultForgeRarityWeights),但这里是固定值,不跟随本局配置里改过的权重;
	// 是否改为跟随配置属于平衡决策,整理时不改变现有行为。
	private static HextechRarityTier RandomForgeRarity(Player owner, string salt)
	{
		int r = StableIndex(owner, PercentRollRange, "miracle.forge.rarity", salt);
		return r < 65 ? HextechRarityTier.Silver : r < 90 ? HextechRarityTier.Gold : HextechRarityTier.Prismatic;
	}

	// 事件目前仅单机开放,但写游戏状态的随机一律走运行种子哈希而非 GD.Randi():避免日后放开联机时留下双端分叉。
	// 盐格式(miracle.*)自 0.9.x 起不变,历史结果逐位一致。
	private static int StableIndex(Player owner, int count, params string?[] saltParts)
	{
		return HextechRunesApi.StableIndex((RunState)owner.RunState, count, saltParts);
	}

	private static MiracleTierValues ValuesFor(int tier)
	{
		return TierValues[tier - 1];
	}

	// 卡牌献祭按档匹配:普通档(T1)把初始/基础卡(打击、防御)也算进去;罕见(T2)、稀有(T3)。
	private static bool MatchesSacrificeTier(CardModel card, int tier) => tier switch
	{
		1 => card.Rarity is CardRarity.Common or CardRarity.Basic,
		2 => card.Rarity == CardRarity.Uncommon,
		_ => card.Rarity == CardRarity.Rare,
	};

	private bool DeckHasSacrificeCard(int tier) => Owner?.Deck.Cards.Any(card => MatchesSacrificeTier(card, tier)) ?? false;

	private Task GainGold(int amount) => Owner != null ? PlayerCmd.GainGold(amount, Owner) : Task.CompletedTask;

	private Task SpendGold(int amount) => Owner != null ? PlayerCmd.LoseGold(amount, Owner) : Task.CompletedTask;

	private void AddForgePriceDelta(int delta) => Owner?.Relics.OfType<BelieverRune>().FirstOrDefault()?.AddForgePriceDelta(delta);

	// ---- 多屏导航薄封装 ----

	private static string RootPage(string category) => $"{category}_ROOT";

	private static string ActionPage(string category, bool sacrifice) => $"{category}_{(sacrifice ? ActionSacrifice : ActionTake)}";

	private EventOption Choice(string pageKey, string optionKey, Func<Task> onChosen)
	{
		return new EventOption(this, onChosen, $"{Id.Entry}.pages.{pageKey}.options.{optionKey}");
	}

	// 「返回上一级」选项(每个子页都有,始终可选)。防止进入某分支后所有档位都因条件不满足被锁 → 无可选项 → 死档。
	// 用共享本地化键 common.BACK(EventOption 自动补 .title/.description)。
	private EventOption Back(Func<Task> onBack)
	{
		return new EventOption(this, onBack, $"{Id.Entry}.common.BACK");
	}

	private Task ShowInitial() => Show(InitialPage, GenerateInitialOptions());

	private Task ShowCategoryRoot(string category)
	{
		return category == CategoryGift
			? Show(RootPage(CategoryGift), GiftRootOptions())
			: Show(RootPage(category), ActionOptions(category));
	}

	private Task Show(string pageKey, IReadOnlyList<EventOption> options)
	{
		SetEventState(new LocString(EventLocTable, $"{Id.Entry}.pages.{pageKey}.description"), options);
		return Task.CompletedTask;
	}

	private async Task ApplyAndFinish(string pageKey, Func<Player, Task> effect)
	{
		if (Owner != null)
		{
			await effect(Owner);
		}

		SetEventFinished(new LocString(EventLocTable, $"{Id.Entry}.pages.{pageKey}.resolution"));
	}

	private readonly record struct MiracleTierValues(
		int ForgeSacrificeCost,
		int ForgeTakePriceDelta,
		int GoldAmount,
		int GoldSacrificePriceDelta,
		int GoldTakePriceDelta,
		int CardSacrificeGold,
		int CardPackCost,
		int GiftLotteryCost,
		CardRarity CardRarity);
}
