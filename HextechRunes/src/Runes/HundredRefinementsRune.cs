using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class HundredRefinementsRune : HextechRelicBase
{
	private const string BodyForgeOptionId = "HEXTECH_BODY_FORGE";
	private int _bodyForgeCount;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedBodyForgeCount
	{
		get => _bodyForgeCount;
		set
		{
			_bodyForgeCount = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("BodyForges", 3m),
		new DynamicVar("ForgeCount", 1m)
	];

	public override bool ShowCounter => true;

	public override int DisplayAmount => !IsCanonical ? _bodyForgeCount % BodyForgeThreshold : 0;

	public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
	{
		if (player != Owner || Owner == null)
		{
			return false;
		}

		alternatives.Add(new CardRewardAlternative(
			BodyForgeOptionId,
			ForgingBodyFromCardReward,
			PostAlternateCardRewardAction.EndSelectionAndCompleteReward));
		return true;
	}

	private async Task ForgingBodyFromCardReward()
	{
		if (Owner == null)
		{
			return;
		}

		_bodyForgeCount++;
		InvokeDisplayAmountChanged();
		Flash();
		if (_bodyForgeCount % BodyForgeThreshold != 0)
		{
			return;
		}

		await HextechForgeGrantHelper.ObtainRandomForges(Owner, Math.Max(0, DynamicVars["ForgeCount"].IntValue));
	}

	private int BodyForgeThreshold => Math.Max(1, DynamicVars["BodyForges"].IntValue);
}
