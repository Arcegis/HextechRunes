using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunes;

public sealed class NatureIsHealingRune : HextechRelicBase
{
	private const string TimerNodeName = "HextechNatureIsHealingTimer";

	private Godot.Timer? _timer;
	private bool _healing;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("IntervalSeconds", 10m),
		new HealVar(1m)
	];

	// 联机下禁用:实时(墙钟)回血在 lockstep 多人下无法确定性同步(墙钟各机不同),
	// 故联机时不把本符文放进玩家可选/发放池——BuildSelectableRunePool 与 BuildObtainableRunePool
	// 都以 HextechCatalog.IsAvailableForPlayer 为闸门。IsNetworkMultiplayer() 两端一致,池子确定性排除、不引入分叉。
	// 单机不受影响。敌方侧的对应排除见 HextechMonsterHexRoller.FilterMultiplayerDisabled。
	public override bool IsAvailableForPlayer(Player player)
	{
		return !IsNetworkMultiplayer();
	}

	public override Task BeforeCombatStart()
	{
		StopTimer();
		if (!IsNetworkMultiplayer())
		{
			StartTimer();
		}

		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		StopTimer();
		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (!IsNetworkMultiplayer() || player != Owner || !ShouldHeal())
		{
			return;
		}

		Flash();
		await HealOwner();
	}

	private void StartTimer()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Node? root = NGame.Instance?.GetTree()?.Root;
		if (root == null)
		{
			Log.Warn($"[{ModInfo.Id}][NatureIsHealing] Timer skipped: scene tree root unavailable.", 2);
			return;
		}

		Godot.Timer timer = new()
		{
			Name = TimerNodeName,
			WaitTime = (double)DynamicVars["IntervalSeconds"].BaseValue,
			OneShot = false,
			Autostart = true
		};
		timer.Timeout += OnTimerTimeout;
		root.AddChild(timer);
		_timer = timer;
	}

	private void StopTimer()
	{
		if (_timer is not { } timer)
		{
			return;
		}

		if (GodotObject.IsInstanceValid(timer))
		{
			timer.Timeout -= OnTimerTimeout;
			timer.QueueFree();
		}

		_timer = null;
		_healing = false;
	}

	private async void OnTimerTimeout()
	{
		if (_healing)
		{
			return;
		}

		_healing = true;
		try
		{
			if (!ShouldHeal())
			{
				return;
			}

			Flash();
			await HealOwner();
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][NatureIsHealing] Timer heal failed: {ex.Message}", 2);
		}
		finally
		{
			_healing = false;
		}
	}

	private bool ShouldHeal()
	{
		return Owner != null
			&& CombatManager.Instance?.IsInProgress == true
			&& Owner.RunState.CurrentRoom is CombatRoom
			&& Owner.Creature.CombatState != null
			&& !Owner.Creature.IsDead;
	}

	private Task HealOwner()
	{
		return CreatureCmd.Heal(Owner!.Creature, DynamicVars.Heal.BaseValue);
	}
}
