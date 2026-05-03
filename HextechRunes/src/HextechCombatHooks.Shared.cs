using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static HextechMayhemModifier? GetMayhemModifier(RunState runState)
	{
		return runState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault();
	}
}
