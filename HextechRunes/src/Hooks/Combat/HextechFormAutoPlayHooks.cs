using HarmonyLib;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

/// <summary>
/// 开局自动打出"形态"牌时压掉卡牌自带的回合结束(虚空形态 OnPlay 末尾会 EndTurn),
/// 否则玩家首回合直接被吃掉。作用域仅覆盖自动打出窗口,手动打出不受影响;
/// 两端都在确定性战斗逻辑内进入/退出作用域,联机安全。
/// </summary>
internal static class HextechFormAutoPlayHooks
{
	private static readonly AsyncLocal<bool> SuppressEndTurn = new();

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(PlayerCmd), nameof(PlayerCmd.EndTurn), BindingFlags.Public | BindingFlags.Static, typeof(Player), typeof(bool), typeof(Func<Task>)),
			prefix: new HarmonyMethod(typeof(HextechFormAutoPlayHooks), nameof(EndTurnPrefix)) { priority = Priority.First });
	}

	internal static IDisposable BeginEndTurnSuppression()
	{
		return new SuppressionScope();
	}

	private static bool EndTurnPrefix()
	{
		return !SuppressEndTurn.Value;
	}

	private sealed class SuppressionScope : IDisposable
	{
		private readonly bool _previous;

		public SuppressionScope()
		{
			_previous = SuppressEndTurn.Value;
			SuppressEndTurn.Value = true;
		}

		public void Dispose()
		{
			SuppressEndTurn.Value = _previous;
		}
	}
}
