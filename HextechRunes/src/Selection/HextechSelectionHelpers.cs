using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class HextechSelectionHelpers
{
	// 没有场景树可等(或节点尚未入树)时,用约一帧(60 FPS)的延迟代替等待 ProcessFrame。
	private static readonly TimeSpan FrameFallbackDelay = TimeSpan.FromMilliseconds(16);

	/// <summary>当前是已连接的联机(房主或客户端)。断线后的联机局与单机/回放都返回 false。</summary>
	internal static bool IsMultiplayerConnected()
	{
		INetGameService netService = RunManager.Instance.NetService;
		return netService.Type is NetGameType.Host or NetGameType.Client && netService.IsConnected;
	}

	internal static bool SameRuneCandidate(RelicModel left, RelicModel right)
	{
		if (left.CanonicalId() != right.CanonicalId())
		{
			return false;
		}

		if (left is IHextechGeneratedRune a && right is IHextechGeneratedRune b)
		{
			return string.Equals(a.ExportSelectionData(), b.ExportSelectionData(), StringComparison.Ordinal);
		}

		return true;
	}

	public static int IndexOfRelicInstance(IReadOnlyList<RelicModel> relics, RelicModel? selected)
	{
		if (selected == null)
		{
			return -1;
		}

		for (int i = 0; i < relics.Count; i++)
		{
			if (ReferenceEquals(relics[i], selected))
			{
				return i;
			}
		}

		return -1;
	}

	public static int IndexOfRelicById(IReadOnlyList<RelicModel> relics, RelicModel? selected)
	{
		if (selected == null)
		{
			return -1;
		}

		ModelId selectedId = selected.CanonicalId();
		for (int i = 0; i < relics.Count; i++)
		{
			ModelId optionId = relics[i].CanonicalInstance?.Id ?? relics[i].Id;
			if (optionId == selectedId)
			{
				return i;
			}
		}

		return -1;
	}

	public static RelicModel? CreateMonsterHexRelic(MonsterHexKind? monsterHex)
	{
		return monsterHex.HasValue
			? MonsterHexCatalog.GetIconRelicForMonsterHex(monsterHex.Value).ToMutable()
			: null;
	}

	public static MonsterHexKind? GetMonsterHexSlot(IReadOnlyList<MonsterHexKind?> monsterHexes, int slotIndex)
	{
		return slotIndex >= 0 && slotIndex < monsterHexes.Count
			? monsterHexes[slotIndex]
			: null;
	}

	public static void MarkRelicsSeen(IEnumerable<RelicModel> relics)
	{
		foreach (RelicModel relic in relics)
		{
			SaveManager.Instance.MarkRelicAsSeen(relic);
		}
	}

	public static async Task<T?> WaitForSingletonAsync<T>(
		Func<T?> getInstance,
		int attempts = 60,
		CancellationToken cancellationToken = default)
		where T : class
	{
		for (int i = 0; i < attempts; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			T? instance = getInstance();
			if (instance != null)
			{
				return instance;
			}

			await WaitForProcessFrameOrDelayAsync(cancellationToken);
		}

		cancellationToken.ThrowIfCancellationRequested();
		return getInstance();
	}

	public static async Task WaitForProcessFrameOrDelayAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		NGame? game = NGame.Instance;
		if (game?.IsInsideTree() != true)
		{
			await Task.Delay(FrameFallbackDelay, cancellationToken);
			return;
		}

		SceneTree tree = game.GetTree();
		TaskCompletionSource<bool> processFrame = new(TaskCreationOptions.RunContinuationsAsynchronously);
		void OnProcessFrame()
		{
			processFrame.TrySetResult(true);
		}

		tree.ProcessFrame += OnProcessFrame;
		try
		{
			Task delay = Task.Delay(FrameFallbackDelay, cancellationToken);
			Task completed = await Task.WhenAny(processFrame.Task, delay);
			await completed;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(tree))
			{
				tree.ProcessFrame -= OnProcessFrame;
			}
		}
	}
}
