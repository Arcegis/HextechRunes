using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class HextechSelectionHelpers
{
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

		ModelId selectedId = selected.CanonicalInstance?.Id ?? selected.Id;
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

	public static async Task<T?> WaitForSingletonAsync<T>(Func<T?> getInstance, int attempts = 60)
		where T : class
	{
		for (int i = 0; i < attempts; i++)
		{
			T? instance = getInstance();
			if (instance != null)
			{
				return instance;
			}

			await Task.Yield();
		}

		return getInstance();
	}
}
