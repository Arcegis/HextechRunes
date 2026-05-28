using MegaCrit.Sts2.Core.Map;

namespace HextechRunes;

internal sealed class HastyScribbleEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.HastyScribble;

	internal override ActMap ModifyGeneratedMapLate(HextechEnemyHexContext context, IRunState runState, ActMap map, int actIndex)
	{
		return HextechMapLengthReducer.ReduceNodeLengthByOne(map, runState.CurrentMapCoord);
	}
}
