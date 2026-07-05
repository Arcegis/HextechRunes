namespace HextechRunes;

internal sealed class HextechActSelectionGate
{
	private bool _isHandling;
	private object? _runState;

	public bool IsHandling => _isHandling;

	public bool ResetIfStaleRun(object runState)
	{
		ArgumentNullException.ThrowIfNull(runState);
		if (!_isHandling || _runState == null || ReferenceEquals(_runState, runState))
		{
			return false;
		}

		Reset();
		return true;
	}

	public void Enter(object runState)
	{
		ArgumentNullException.ThrowIfNull(runState);
		if (_isHandling)
		{
			throw new InvalidOperationException("Act selection is already being handled.");
		}

		_isHandling = true;
		_runState = runState;
	}

	public bool ExitIfCurrent(object runState)
	{
		ArgumentNullException.ThrowIfNull(runState);
		if (!ReferenceEquals(_runState, runState))
		{
			return false;
		}

		Reset();
		return true;
	}

	public void Reset()
	{
		_isHandling = false;
		_runState = null;
	}
}
