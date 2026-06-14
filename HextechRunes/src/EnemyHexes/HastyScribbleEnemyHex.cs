using MegaCrit.Sts2.Core.Map;

namespace HextechRunes;

internal sealed class HastyScribbleEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.HastyScribble;

	internal override ActMap ModifyGeneratedMapLate(HextechEnemyHexContext context, IRunState runState, ActMap map, int actIndex)
	{
		int rowsToRemove = context.TierValueForAct(Kind, actIndex, 1, 2, 3);
		return HextechMapLengthReducer.ReduceNodeLength(runState, map, runState.CurrentMapCoord, rowsToRemove);
	}
}
