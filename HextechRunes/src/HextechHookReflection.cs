using System.Reflection;

namespace HextechRunes;

internal static class HextechHookReflection
{
	public static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}

	public static MethodInfo RequireMethodAllowingSingleArityFallback(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		MethodInfo? exact = type.GetMethod(name, flags, binder: null, parameters, modifiers: null);
		if (exact != null)
		{
			return exact;
		}

		MethodInfo[] candidates = type.GetMethods(flags | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
			.Where(method => method.Name == name && method.GetParameters().Length == parameters.Length)
			.ToArray();
		if (candidates.Length == 1)
		{
			return candidates[0];
		}

		throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}

	public static MethodInfo? TryGetMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null);
	}

	public static FieldInfo RequireField(Type type, string name, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
	{
		return type.GetField(name, flags)
			?? throw new InvalidOperationException($"Could not find required field {type.FullName}.{name}.");
	}

	public static FieldInfo? TryGetField(Type type, string name, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
	{
		return type.GetField(name, flags);
	}

	public static MethodInfo RequireGetter(Type type, string propertyName, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
	{
		return type.GetProperty(propertyName, flags)?.GetMethod
			?? throw new InvalidOperationException($"Could not find property getter {type.FullName}.{propertyName}.");
	}
}
