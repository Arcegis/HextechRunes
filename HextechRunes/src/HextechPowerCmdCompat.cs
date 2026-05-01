using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class HextechPowerCmdCompat
{
	public static Task<IReadOnlyList<T>> Apply<T>(
		IEnumerable<Creature> targets,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
		where T : PowerModel
	{
#if STS2_104_OR_NEWER
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			new BlockingPlayerChoiceContext(),
			targets,
			amount,
			applier,
			cardSource,
			silent);
#else
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			targets,
			amount,
			applier,
			cardSource,
			silent);
#endif
	}

	public static Task<T?> Apply<T>(
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
		where T : PowerModel
	{
#if STS2_104_OR_NEWER
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			new BlockingPlayerChoiceContext(),
			target,
			amount,
			applier,
			cardSource,
			silent);
#else
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			target,
			amount,
			applier,
			cardSource,
			silent);
#endif
	}

	public static Task Apply(
		PowerModel power,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
	{
#if STS2_104_OR_NEWER
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply(
			new BlockingPlayerChoiceContext(),
			power,
			target,
			amount,
			applier,
			cardSource,
			silent);
#else
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply(
			power,
			target,
			amount,
			applier,
			cardSource,
			silent);
#endif
	}

	public static Task Remove<T>(Creature creature)
		where T : PowerModel
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Remove<T>(creature);
	}

	public static Task Remove(PowerModel power)
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(power);
	}
}
