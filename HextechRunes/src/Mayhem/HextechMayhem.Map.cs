using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
	{
		if (!ReferenceEquals(runState, RunState))
		{
			return map;
		}

		return ApplyMapModifiers(map, runState, actIndex);
	}

	internal void ApplyMapModifiersToCurrentAct(string reason)
	{
		RunState runState = ActiveRunState;
		ActMap currentMap = runState.Map;
		ActMap modifiedMap = ApplyMapModifiers(currentMap, runState, runState.CurrentActIndex);
		if (ReferenceEquals(modifiedMap, currentMap))
		{
			return;
		}

		runState.Map = modifiedMap;
		runState.RemoveStaleVisitedMapCoords(modifiedMap);
		Log.Info($"[{ModInfo.Id}][Mayhem] Applied map modifiers to current act: reason={reason} act={runState.CurrentActIndex} activeHexes={string.Join(",", GetActiveMonsterHexes())}");
	}

	private ActMap ApplyMapModifiers(ActMap map, IRunState runState, int actIndex)
	{
		var context = new HextechEnemyHexContext(this);
		ActMap modifiedMap = map;
		foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
		{
			modifiedMap = effect.ModifyGeneratedMapLate(context, runState, modifiedMap, actIndex);
		}

		return modifiedMap;
	}
}
