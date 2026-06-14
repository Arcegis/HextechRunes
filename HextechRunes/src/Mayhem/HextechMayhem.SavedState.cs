using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	private readonly HextechMayhemActState _actState = new();
	private readonly HextechMayhemCombatTrackingState _combatTracking = new();
	private readonly HextechMayhemChoiceHistoryState _choiceHistory = new();
	private readonly HextechActiveMonsterHexCache _activeMonsterHexCache = new();
	private int[] _enemyHexCountsByAct = HextechRuneConfiguration.GetDefaultEnemyHexCountsByAct();
	private int _hexCountRecoveryBaseline;
	private int _monsterHexStrengthTierFloor;
	private int _enemyTezcatarasMercyCombatCounter;
	private bool _hostUsesBetterMultiplayerScaling;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedRarityByAct
	{
		get => _actState.SavedRarityByAct;
		set
		{
			_actState.SavedRarityByAct = value;
			InvalidateActiveMonsterHexCache();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedMonsterHexByAct
	{
		get => _actState.SavedMonsterHexByAct;
		set
		{
			_actState.SavedMonsterHexByAct = value;
			InvalidateActiveMonsterHexCache();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string SavedMonsterHexesByActJson
	{
		get => _actState.SavedMonsterHexesByActJson;
		set
		{
			_actState.SavedMonsterHexesByActJson = value;
			InvalidateActiveMonsterHexCache();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedCarriedMonsterHexes
	{
		get => _actState.SavedCarriedMonsterHexes;
		set
		{
			_actState.SavedCarriedMonsterHexes = value;
			InvalidateActiveMonsterHexCache();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedResolvedActs
	{
		get => _actState.SavedResolvedActs;
		set
		{
			_actState.SavedResolvedActs = value;
			InvalidateActiveMonsterHexCache();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedMapLengthReducedActs
	{
		get => _actState.SavedMapLengthReducedActs;
		set => _actState.SavedMapLengthReducedActs = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedEnemyHexCountsByAct
	{
		get => _enemyHexCountsByAct.ToArray();
		set => _enemyHexCountsByAct = NormalizeEnemyHexCountsByAct(value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string SavedTelemetryChoicesJson
	{
		get => _choiceHistory.SavedTelemetryChoicesJson;
		set => _choiceHistory.SavedTelemetryChoicesJson = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string SavedSeenPlayerRuneIdsJson
	{
		get => _choiceHistory.SavedSeenPlayerRuneIdsJson;
		set => _choiceHistory.SavedSeenPlayerRuneIdsJson = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedHexCountRecoveryBaseline
	{
		get => _hexCountRecoveryBaseline;
		set => _hexCountRecoveryBaseline = Math.Max(0, value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedMonsterHexStrengthTierFloor
	{
		get => _monsterHexStrengthTierFloor;
		set => _monsterHexStrengthTierFloor = Math.Clamp(value, 0, 3);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string SavedCombatTrackingJson
	{
		get => _combatTracking.Serialize();
		set => _combatTracking.Restore(value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedEnemyTezcatarasMercyCombatCounter
	{
		get => _enemyTezcatarasMercyCombatCounter;
		set => _enemyTezcatarasMercyCombatCounter = Math.Max(0, value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedHextechHostUsesBetterMultiplayerScaling
	{
		get => _hostUsesBetterMultiplayerScaling;
		set => _hostUsesBetterMultiplayerScaling = value;
	}

	public override LocString Title => new("modifiers", "HEXTECH_MAYHEM.title");

	public override LocString Description => new("modifiers", "HEXTECH_MAYHEM.description");

	protected override string IconPath => $"res://{ModInfo.Id}/images/relics/prismaticForge.png";

	public override IEnumerable<IHoverTip> HoverTips => [];

	public RunState ActiveRunState => RunState;

	internal HextechMayhemCombatTrackingState CombatTracking => _combatTracking;

	internal bool IsEndlessLoopActive => _monsterHexStrengthTierFloor >= 3;

	internal bool HostUsesBetterMultiplayerScaling
	{
		get => _hostUsesBetterMultiplayerScaling;
		set => _hostUsesBetterMultiplayerScaling = value;
	}
}
