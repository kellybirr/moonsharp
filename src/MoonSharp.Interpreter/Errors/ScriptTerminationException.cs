namespace MoonSharp.Interpreter
{
	/// <summary>
	/// Thrown when a script exceeds its host-set <see cref="ExecutionLimits"/> (instruction
	/// budget or cancellation). Deliberately NOT a ScriptRuntimeException: the VM only routes
	/// ScriptRuntimeException through Lua-side pcall/xpcall handlers, so termination always
	/// unwinds to the host. FillDebugData still decorates it, so DecoratedMessage carries the
	/// source location. Do not override Rethrow(): the VM rethrows the original instance.
	/// </summary>
	public class ScriptTerminationException : InterpreterException
	{
		internal ScriptTerminationException(string message)
			: base(message) { }

		internal ScriptTerminationException(string format, params object[] args)
			: base(format, args) { }
	}
}
