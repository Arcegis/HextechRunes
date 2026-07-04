using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

/// <summary>
/// 我们的治疗(仅联机):队友获得生命回复时,持有者获得等量回复。
/// 由 HextechCombatHooks.Healing 的 CreatureCmd.Heal postfix 统一驱动,
/// 战斗内外通吃;镜像期间以深度标志阻断再触发,防止双持互相回血无限递归。
/// </summary>
public sealed class OurHealingRune : HextechRelicBase
{
	private static int _mirrorDepth;

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNetworkMultiplayer();
	}

	internal static async Task MirrorTeammateHeal(Creature healed, decimal amount)
	{
		if (_mirrorDepth > 0 || amount <= 0m)
		{
			return;
		}

		Player? healedPlayer = healed.Player;
		if (healedPlayer == null || healed != healedPlayer.Creature || healedPlayer.RunState is not RunState runState)
		{
			return;
		}

		List<OurHealingRune> receivers = [];
		foreach (Player player in runState.Players)
		{
			if (player == healedPlayer || player.Creature.IsDead)
			{
				continue;
			}

			if (player.GetRelic<OurHealingRune>() is OurHealingRune rune)
			{
				receivers.Add(rune);
			}
		}

		if (receivers.Count == 0)
		{
			return;
		}

		_mirrorDepth++;
		try
		{
			foreach (OurHealingRune rune in receivers)
			{
				if (rune.Owner == null || rune.Owner.Creature.IsDead)
				{
					continue;
				}

				rune.Flash();
				await CreatureCmd.Heal(rune.Owner.Creature, amount);
			}
		}
		finally
		{
			_mirrorDepth--;
		}
	}
}
