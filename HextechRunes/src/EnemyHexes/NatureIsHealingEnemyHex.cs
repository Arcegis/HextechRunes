using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;

namespace HextechRunes;

internal sealed class NatureIsHealingEnemyHex : HextechEnemyHexEffect
{
	private const string TimerNodeName = "HextechEnemyNatureIsHealingTimer";

	private Godot.Timer? _timer;
	private HextechMayhemModifier? _modifier;
	private bool _healing;

	internal override MonsterHexKind Kind => MonsterHexKind.NatureIsHealing;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		_modifier = context.Modifier;
		if (!HextechPlayerContextHelper.IsNetworkMultiplayerRun())
		{
			StartTimer(context);
		}

		return Task.CompletedTask;
	}

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		if (!HextechPlayerContextHelper.IsNetworkMultiplayerRun() || enemies.Count == 0 || combatState.RunState != context.RunState)
		{
			return;
		}

		foreach (Creature enemy in enemies)
		{
			await CreatureCmd.Heal(enemy, 1m);
		}
	}

	internal override Task AfterCombatEnd(HextechEnemyHexContext context, CombatRoom room)
	{
		StopTimer();
		return Task.CompletedTask;
	}

	private void StartTimer(HextechEnemyHexContext context)
	{
		if (_timer != null)
		{
			return;
		}

		Node? root = NGame.Instance?.GetTree()?.Root;
		if (root == null)
		{
			Log.Warn($"[{ModInfo.Id}][EnemyNatureIsHealing] Timer skipped: scene tree root unavailable.", 2);
			return;
		}

		Godot.Timer timer = new()
		{
			Name = TimerNodeName,
			WaitTime = (double)context.TierValue(Kind, 15.0m, 10.0m, 5.0m),
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
		_modifier = null;
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
			if (!TryGetAliveEnemies(out IReadOnlyList<Creature> enemies))
			{
				StopTimer();
				return;
			}

			foreach (Creature enemy in enemies)
			{
				await CreatureCmd.Heal(enemy, 1m);
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][EnemyNatureIsHealing] Timer heal failed: {ex.Message}", 2);
		}
		finally
		{
			_healing = false;
		}
	}

	private bool TryGetAliveEnemies(out IReadOnlyList<Creature> enemies)
	{
		enemies = [];
		if (_modifier == null
			|| CombatManager.Instance?.IsInProgress != true
			|| _modifier.ActiveRunState.CurrentRoom is not CombatRoom room
			|| room.CombatState.RunState != _modifier.ActiveRunState)
		{
			return false;
		}

		enemies = HextechCombatCreatureHelper.GetAliveEnemies(room.CombatState);
		return enemies.Count > 0;
	}
}
