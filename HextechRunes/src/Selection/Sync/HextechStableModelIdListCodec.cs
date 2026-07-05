namespace HextechRunes;

internal static class HextechStableModelIdListCodec
{
	public const int Version = -3;
	private const int MaxCount = 64;
	private const int MaxLength = 128;

	public static void Append(List<int> payload, IEnumerable<ModelId> modelIds)
	{
		ModelId[] ids = modelIds.ToArray();
		payload.Add(Version);
		payload.Add(ids.Length);
		foreach (ModelId id in ids)
		{
			string serialized = id.ToString();
			payload.Add(serialized.Length);
			foreach (char ch in serialized)
			{
				payload.Add(ch);
			}
		}
	}

	public static bool TryDecode(IReadOnlyList<int> payload, int cursor, out List<ModelId> modelIds, out int nextCursor)
	{
		modelIds = [];
		nextCursor = cursor;
		if (payload.Count <= cursor || payload[cursor] != Version)
		{
			return false;
		}

		cursor++;
		if (payload.Count <= cursor)
		{
			return false;
		}

		int count = payload[cursor++];
		if (count < 0 || count > MaxCount)
		{
			return false;
		}

		for (int i = 0; i < count; i++)
		{
			if (payload.Count <= cursor)
			{
				return false;
			}

			int length = payload[cursor++];
			if (length < 0 || length > MaxLength || payload.Count < cursor + length)
			{
				return false;
			}

			char[] chars = new char[length];
			for (int j = 0; j < length; j++)
			{
				int value = payload[cursor + j];
				if (value < char.MinValue || value > char.MaxValue)
				{
					return false;
				}

				chars[j] = (char)value;
			}

			try
			{
				modelIds.Add(ModelId.Deserialize(new string(chars)));
			}
			catch
			{
				modelIds.Clear();
				return false;
			}

			cursor += length;
		}

		nextCursor = cursor;
		return true;
	}
}
