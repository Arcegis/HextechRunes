using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

internal sealed class GenesisUpgradedCardReward : CardReward
{
	private bool _upgradedGeneratedCards;

	public GenesisUpgradedCardReward(CardCreationOptions options, int cardCount, Player player)
		: base(options, cardCount, player)
	{
	}

	private void UpgradeGeneratedCards()
	{
		if (_upgradedGeneratedCards)
		{
			return;
		}

		foreach (CardModel card in Cards)
		{
			if (card.IsUpgradable && !card.IsUpgraded)
			{
				CardCmd.Upgrade(card, CardPreviewStyle.None);
			}
		}

		_upgradedGeneratedCards = true;
	}

#if STS2_105_OR_NEWER
	public override void Populate()
	{
		base.Populate();
		UpgradeGeneratedCards();
	}
#else
	public override async Task Populate()
	{
		await base.Populate();
		UpgradeGeneratedCards();
	}
#endif

	public override SerializableReward ToSerializable()
	{
		SerializableReward save = base.ToSerializable();
		save.CustomDescriptionEncounterSourceId = ModelDb.GetId<GenesisRune>();
		return save;
	}
}
