using MegaCrit.Sts2.Core.Models.Enchantments;

namespace HextechRunes;

public sealed class InkshadowRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Shiv>(),
		.. HoverTipFactory.FromEnchantment<Inky>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	internal static bool TryApplyForOwner(CardModel? card, Player? owner, bool flash = true)
	{
		return owner?.GetRelic<InkshadowRune>() is InkshadowRune rune
			&& rune.TryApplyInkshadow(card, flash);
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		TryApplyInkshadow(card);
		return Task.CompletedTask;
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		TryApplyInkshadow(card, flash: false);
		return Task.CompletedTask;
	}

	public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
	{
		newCard = null;
		if (!TryApplyInkshadow(card))
		{
			return false;
		}

		newCard = card;
		return true;
	}

	private bool TryApplyInkshadow(CardModel? card, bool flash = true)
	{
		if (card == null
			|| card.Owner != Owner
			|| !HextechKnifeHelper.IsShivLike(card, Owner)
			|| card.Enchantment != null)
		{
			return false;
		}

		Inky enchantment = (Inky)ModelDb.Enchantment<Inky>().ToMutable();
		if (!enchantment.CanEnchant(card))
		{
			return false;
		}

		CardCmd.Enchant(enchantment, card, 1m);
		if (flash)
		{
			Flash();
		}

		return true;
	}

	// 墨影在小刀生成时已上 Inky，而 Inky 不可叠加(IsStackable=false)：原版 BladeOfInk.OnPlay 对生成的
	// 小刀二次 CardCmd.Enchant 会因 CanEnchant=false 抛 InvalidOperationException 打断打出管线
	// (玩家实测:墨影+瓦库之肩自动打出墨之刃即卡死;手动打出同样中招)。
	// 仅当持有墨影时替换 OnPlay，补附魔前守卫；未持有时走原版路径。
	[HarmonyPatch(typeof(BladeOfInk), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.inkshadow", "墨影", Rune = typeof(InkshadowRune))]
	private static class BladeOfInkPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(BladeOfInk __instance, PlayerChoiceContext choiceContext, ref Task __result)
		{
			if (__instance.Owner is not { } owner
				|| __instance.CombatState is not { } combatState
				|| owner.GetRelic<InkshadowRune>() == null)
			{
				return true;
			}

			__result = PlayWithGuardedEnchant(__instance, owner, combatState);
			return false;
		}

		private static async Task PlayWithGuardedEnchant(BladeOfInk card, Player owner, HextechCombatState combatState)
		{
			foreach (CardModel item in await Shiv.CreateInHand(owner, card.DynamicVars.Cards.IntValue, combatState))
			{
				if (item.Enchantment is Inky)
				{
					// 墨影已在生成时上过 Inky,原版的二次附魔按语义就是"确保有 Inky",直接跳过。
					continue;
				}

				Inky enchantment = (Inky)ModelDb.Enchantment<Inky>().ToMutable();
				if (enchantment.CanEnchant(item))
				{
					CardCmd.Enchant(enchantment, item, 1m);
				}
			}
		}
	}
}
