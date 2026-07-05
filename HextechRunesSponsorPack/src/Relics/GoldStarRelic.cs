using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

public sealed class GoldStarRelic : RelicModel, IHextechSharedCombatVictoryRune
{
	private const int NormalMonsterMinGold = 10;
	private const int NormalMonsterMaxGold = 20;
	private const int CardRewardOptionCount = 3;
	private const string RelicIconPath = "res://HextechRunesSponsorPack/images/relics/goldStarRelic.png";

	public sealed override RelicRarity Rarity => RelicRarity.Event;

	public override string PackedIconPath => RelicIconPath;

	protected override string PackedIconOutlinePath => RelicIconPath;

	protected override string BigIconPath => RelicIconPath;

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (HextechRelicBase.IsNetworkMultiplayerRun())
		{
			return Task.CompletedTask;
		}

		return ApplySharedCombatVictory(room);
	}

	public Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead || room.RoomType != RoomType.Elite)
		{
			return Task.CompletedTask;
		}

		Flash(Array.Empty<Creature>());
		AddNormalMonsterGoldReward(room);
		AddNormalMonsterPotionRewardIfRolled(room);
		room.AddExtraReward(Owner, new CardReward(CardCreationOptions.ForRoom(Owner, RoomType.Monster), CardRewardOptionCount, Owner));
		return Task.CompletedTask;
	}

	private void AddNormalMonsterGoldReward(CombatRoom room)
	{
		if (Owner == null || room.GoldProportion <= 0f)
		{
			return;
		}

		int minGold = (int)Math.Round(NormalMonsterMinGold * room.GoldProportion, MidpointRounding.AwayFromZero);
		int maxGold = (int)Math.Round(NormalMonsterMaxGold * room.GoldProportion, MidpointRounding.AwayFromZero);
		if (maxGold <= 0)
		{
			return;
		}

		room.AddExtraReward(Owner, new GoldReward(minGold, maxGold, Owner));
	}

	private void AddNormalMonsterPotionRewardIfRolled(CombatRoom room)
	{
		if (Owner == null || RunManager.Instance?.AscensionManager == null)
		{
			return;
		}

		// 0.108.0 起 Roll 去掉 AscensionManager 参数。
#if STS2_108_OR_NEWER
		if (Owner.PlayerOdds.PotionReward.Roll(Owner, RoomType.Monster))
#else
		if (Owner.PlayerOdds.PotionReward.Roll(Owner, RunManager.Instance.AscensionManager, RoomType.Monster))
#endif
		{
			room.AddExtraReward(Owner, new PotionReward(Owner));
		}
	}
}
