namespace HextechRunes;

internal static class HextechStableModelIdListCodec
{
	public const int Version = -3;
	public const int MaxCount = 64;
	public const int MaxSerializedLength = 128;

	public static void Append(List<int> payload, IEnumerable<ModelId> modelIds)
	{
		ModelId[] ids = modelIds.ToArray();
		if (ids.Length > MaxCount)
		{
			throw new ArgumentOutOfRangeException(
				nameof(modelIds),
				ids.Length,
				$"ModelId payload count must not exceed {MaxCount}.");
		}

		string[] serializedIds = new string[ids.Length];
		for (int i = 0; i < ids.Length; i++)
		{
			string serialized = ids[i].ToString();
			if (serialized.Length > MaxSerializedLength)
			{
				throw new ArgumentException(
					$"Serialized ModelId length must not exceed {MaxSerializedLength}: {serialized.Length}.",
					nameof(modelIds));
			}

			serializedIds[i] = serialized;
		}

		payload.Add(Version);
		payload.Add(ids.Length);
		foreach (string serialized in serializedIds)
		{
			HextechChoiceCodec.AppendLengthPrefixedString(payload, serialized);
		}
	}

	public static bool TryDecode(IReadOnlyList<int> payload, int cursor, out List<ModelId> modelIds, out int nextCursor)
	{
		modelIds = [];
		nextCursor = cursor;
		if (!HextechChoiceCodec.HasRemaining(payload, cursor, 1) || payload[cursor] != Version)
		{
			return false;
		}

		cursor++;
		if (!HextechChoiceCodec.HasRemaining(payload, cursor, 1))
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
			if (!HextechChoiceCodec.TryReadLengthPrefixedString(payload, ref cursor, MaxSerializedLength, out string? serialized))
			{
				modelIds.Clear();
				return false;
			}

			try
			{
				modelIds.Add(ModelId.Deserialize(serialized));
			}
			catch
			{
				modelIds.Clear();
				return false;
			}
		}

		nextCursor = cursor;
		return true;
	}
}
