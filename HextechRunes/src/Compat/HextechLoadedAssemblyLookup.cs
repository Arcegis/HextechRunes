namespace HextechRunes;

/// <summary>
/// 按程序集名查找已加载的外部模组程序集（软依赖探测）。结果（包括"没找到"）会缓存，
/// 之后每加载一个新程序集就让"没找到"失效重查：模组按顺序加载，首次查询可能早于对方加载。
/// </summary>
internal sealed class HextechLoadedAssemblyLookup
{
	private readonly string _assemblyName;
	private readonly StringComparison _comparison;
	private readonly object _lock = new();
	private bool _resolved;
	private Assembly? _assembly;

	internal HextechLoadedAssemblyLookup(string assemblyName, StringComparison comparison)
	{
		_assemblyName = assemblyName;
		_comparison = comparison;
		AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
	}

	internal Assembly? Find()
	{
		lock (_lock)
		{
			if (!_resolved)
			{
				_assembly = AppDomain.CurrentDomain.GetAssemblies()
					.FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, _assemblyName, _comparison));
				_resolved = true;
			}

			return _assembly;
		}
	}

	private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
	{
		lock (_lock)
		{
			if (_assembly == null)
			{
				_resolved = false;
			}
		}
	}
}
