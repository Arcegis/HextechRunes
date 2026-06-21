using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class StokeRune : HextechRelicBase
{
	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (Owner == null || player != Owner || options.Any(static option => option.OptionId == StokeRestSiteOption.OptionIdValue))
		{
			return false;
		}

		options.Add(new StokeRestSiteOption(player));
		return true;
	}
}

internal sealed class StokeRestSiteOption : RestSiteOption
{
	public const string OptionIdValue = "STOKE";

	private static readonly string IconPath = ImageHelper.GetImagePath("ui/rest_site/option_stoke.png");

	public override string OptionId => OptionIdValue;

	public override LocString Description => IsEnabled
		? new LocString("rest_site_ui", "OPTION_" + OptionId + ".description")
		: new LocString("rest_site_ui", "OPTION_" + OptionId + ".descriptionDisabled");

	public override IEnumerable<string> AssetPaths => [IconPath];

#if STS2_104_OR_NEWER
	public override bool IsEnabled => CanRemoveCard;
#endif

	public StokeRestSiteOption(Player owner)
		: base(owner)
	{
#if !STS2_104_OR_NEWER
		IsEnabled = CanRemoveCard;
#endif
		EnsureIconAlias();
	}

	public override async Task<bool> OnSelect()
	{
		CardSelectorPrefs prefs = new(CardSelectorPrefs.RemoveSelectionPrompt, 1)
		{
			Cancelable = true,
			RequireManualConfirmation = true
		};

		CardModel? card = (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).FirstOrDefault();
		if (card == null)
		{
			return false;
		}

		await CardPileCmd.RemoveFromDeck(card);
		return true;
	}

	private static int GetRemovableCardCount(Player player)
	{
		return PileType.Deck.GetPile(player).Cards.Count(static card => card.IsRemovable);
	}

	private bool CanRemoveCard => GetRemovableCardCount(Owner) >= 1;

	private static void EnsureIconAlias()
	{
		Texture2D stokePortrait = ModelDb.Card<Stoke>().Portrait;
		Texture2D icon = stokePortrait.Duplicate() as Texture2D ?? stokePortrait;
		PreloadManager.Cache.SetAsset(IconPath, icon);
	}
}
