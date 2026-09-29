using System.Collections;
using System.Collections.Concurrent;

namespace HextechRunes;

internal static partial class HextechMayhemCombatTrackingSerializer
{
	// Tracking 字段只有 Dictionary（走非泛型 IDictionary）、HashSet<T> 与几种标量。HashSet<T> 没有非泛型集合接口，
	// 按集合类型缓存一次 Clear/Add/Count 反射句柄；ClearPhase 每个回合边界都会跑，不能每次 GetMethod。
	private sealed record HashSetAccessor(MethodInfo Clear, MethodInfo Add, PropertyInfo Count);

	private static readonly ConcurrentDictionary<Type, HashSetAccessor> HashSetAccessors = new();

	private static HashSetAccessor GetHashSetAccessor(Type setType)
	{
		return HashSetAccessors.GetOrAdd(setType, static type => new HashSetAccessor(
			type.GetMethod(nameof(HashSet<object>.Clear), Type.EmptyTypes)
				?? throw new InvalidOperationException($"Combat tracking collection {type} does not expose Clear()."),
			type.GetMethod(nameof(HashSet<object>.Add), [type.GetGenericArguments()[0]])
				?? throw new InvalidOperationException($"Combat tracking collection {type} does not expose Add()."),
			type.GetProperty(nameof(HashSet<object>.Count), BindingFlags.Instance | BindingFlags.Public)
				?? throw new InvalidOperationException($"Combat tracking collection {type} does not expose Count.")));
	}

	private static object? CopyStateValue(object? source, Type snapshotType)
	{
		if (source == null)
		{
			return null;
		}

		if (source is IDictionary dictionary)
		{
			return CopyDictionary(dictionary, snapshotType);
		}

		if (IsHashSet(source.GetType()))
		{
			return CopySet(source, snapshotType);
		}

		return source is int counter ? Math.Max(0, counter) : source;
	}

	private static object CopyDictionary(IDictionary source, Type snapshotType)
	{
		IDictionary target = (IDictionary)(Activator.CreateInstance(snapshotType)
			?? throw new InvalidOperationException($"Failed to create combat tracking dictionary {snapshotType}."));
		foreach (object key in OrderedValues(source.Keys))
		{
			target.Add(key, source[key]);
		}

		return target;
	}

	private static object CopySet(object source, Type snapshotType)
	{
		IList target = (IList)(Activator.CreateInstance(snapshotType)
			?? throw new InvalidOperationException($"Failed to create combat tracking list {snapshotType}."));
		foreach (object value in OrderedValues((IEnumerable)source))
		{
			target.Add(value);
		}

		return target;
	}

	private static void RestoreStateField(FieldInfo field, HextechMayhemCombatTrackingState state, object? snapshotValue)
	{
		object? target = field.GetValue(state);
		if (target is IDictionary targetDictionary)
		{
			targetDictionary.Clear();
			if (snapshotValue is IDictionary sourceDictionary)
			{
				foreach (DictionaryEntry entry in sourceDictionary)
				{
					targetDictionary[entry.Key] = entry.Value;
				}
			}

			return;
		}

		if (target != null && IsHashSet(target.GetType()))
		{
			HashSetAccessor accessor = GetHashSetAccessor(target.GetType());
			accessor.Clear.Invoke(target, null);
			if (snapshotValue is IEnumerable sourceValues)
			{
				foreach (object value in sourceValues)
				{
					accessor.Add.Invoke(target, [value]);
				}
			}

			return;
		}

		if (field.FieldType == typeof(int))
		{
			field.SetValue(state, Math.Max(0, snapshotValue is int value ? value : 0));
		}
	}

	private static void ClearStateField(FieldInfo field, HextechMayhemCombatTrackingState state)
	{
		object? target = field.GetValue(state);
		if (target is IDictionary dictionary)
		{
			dictionary.Clear();
			return;
		}

		if (target != null && IsHashSet(target.GetType()))
		{
			GetHashSetAccessor(target.GetType()).Clear.Invoke(target, null);
			return;
		}

		if (field.FieldType == typeof(string))
		{
			field.SetValue(state, null);
		}
		else if (field.FieldType == typeof(bool))
		{
			field.SetValue(state, false);
		}
		else if (field.FieldType == typeof(int))
		{
			field.SetValue(state, 0);
		}
	}

	private static bool HasNonDefaultValue(object? value)
	{
		return value switch
		{
			null => false,
			IDictionary dictionary => dictionary.Count > 0,
			_ when TryGetCount(value, out int count) => count > 0,
			int counter => counter > 0,
			bool flag => flag,
			string text => !string.IsNullOrEmpty(text),
			_ => false
		};
	}

	private static bool TryGetCount(object value, out int count)
	{
		if (value is ICollection collection)
		{
			count = collection.Count;
			return true;
		}

		if (IsHashSet(value.GetType())
			&& GetHashSetAccessor(value.GetType()).Count.GetValue(value) is int setCount)
		{
			count = setCount;
			return true;
		}

		count = 0;
		return false;
	}

	private static bool IsHashSet(Type type)
	{
		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>);
	}

	private static IEnumerable<object> OrderedValues(IEnumerable values)
	{
		return values.Cast<object>().OrderBy(static value => value, Comparer<object>.Create(CompareValues));
	}

	private static int CompareValues(object? left, object? right)
	{
		if (ReferenceEquals(left, right))
		{
			return 0;
		}

		if (left == null)
		{
			return -1;
		}

		if (right == null)
		{
			return 1;
		}

		// string 的 IComparable 实现是 culture-sensitive 的，两端 locale 不同会把同一组键排出不同序，
		// 序列化结果参与联机 checksum，必须走 ordinal。
		if (left is string leftText && right is string rightText)
		{
			return string.CompareOrdinal(leftText, rightText);
		}

		return left is IComparable comparable
			? comparable.CompareTo(right)
			: string.CompareOrdinal(left.ToString(), right.ToString());
	}
}
