using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

internal static class HextechCombatProcTracker
{
	public static bool TryConsumeLimitedProc(Dictionary<uint, int> counts, Creature creature, int maxPerTurn)
	{
		if (creature.CombatId == null)
		{
			return false;
		}

		uint combatId = creature.CombatId.Value;
		int current = counts.GetValueOrDefault(combatId, 0);
		if (current >= maxPerTurn)
		{
			return false;
		}

		counts[combatId] = current + 1;
		return true;
	}

	public static bool TryMarkPersistentHexApplied(HashSet<uint> appliedSet, Creature creature, bool forceReapply = false)
	{
		if (creature.CombatId == null)
		{
			return false;
		}

		bool firstApplication = appliedSet.Add(creature.CombatId.Value);
		return forceReapply || firstApplication;
	}

	public static int GetPlayerRuneProcsThisTurn(HextechMayhemCombatTrackingState tracking, Player player, string procKey)
	{
		return tracking.PlayerRuneProcsThisTurn.GetValueOrDefault(GetPlayerRuneProcKey(player, procKey), 0);
	}

	public static bool TryConsumePlayerRuneProcThisTurn(
		HextechMayhemCombatTrackingState tracking,
		Player player,
		string procKey,
		int maxPerTurn)
	{
		if (maxPerTurn <= 0)
		{
			return false;
		}

		string key = GetPlayerRuneProcKey(player, procKey);
		int current = tracking.PlayerRuneProcsThisTurn.GetValueOrDefault(key, 0);
		if (current >= maxPerTurn)
		{
			return false;
		}

		tracking.PlayerRuneProcsThisTurn[key] = current + 1;
		return true;
	}

	public static int ConsumePlayerRuneProcInCombat(HextechMayhemCombatTrackingState tracking, Player player, string procKey)
	{
		string key = GetPlayerRuneProcKey(player, procKey);
		int current = tracking.PlayerRuneProcsThisCombat.GetValueOrDefault(key, 0);
		tracking.PlayerRuneProcsThisCombat[key] = current + 1;
		return current;
	}

	public static bool TrackPlayerAttackCardPlayedThisTurn(HextechMayhemCombatTrackingState tracking, CardPlay cardPlay)
	{
		if (!cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| cardPlay.Card.Owner?.Creature.Side != CombatSide.Player
			|| !IllusoryWeaponRune.IsAttackForEffects(cardPlay.Card, cardPlay.Card.Owner))
		{
			return false;
		}

		ulong playerId = cardPlay.Card.Owner.NetId;
		tracking.PlayerAttackCardsPlayedThisTurn[playerId] = tracking.PlayerAttackCardsPlayedThisTurn.GetValueOrDefault(playerId, 0) + 1;
		return true;
	}

	public static int GetPlayerAttacksPlayedThisTurn(HextechMayhemCombatTrackingState tracking, CardModel card)
	{
		if (card.Owner == null)
		{
			return 0;
		}

		return tracking.PlayerAttackCardsPlayedThisTurn.GetValueOrDefault(card.Owner.NetId, 0);
	}

	private static string GetPlayerRuneProcKey(Player player, string procKey)
	{
		return $"{player.NetId}:{procKey}";
	}
}
