using System.Threading;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// Host-set per-Script execution limits, checked inside the VM instruction loop.
	/// When a limit trips, a <see cref="ScriptTerminationException"/> is thrown; script code
	/// cannot intercept it — pcall/xpcall only handle ScriptRuntimeException, coroutine.resume
	/// rethrows it, and debug.debug() re-raises it rather than swallowing it.
	/// Counting is cumulative for the Script instance until <see cref="Reset"/> is called.
	///
	/// Configure limits BEFORE calling into the script: the loop snapshots whether checking is
	/// enabled when execution begins, so a budget or token installed on an already-running,
	/// initially-unlimited execution takes effect only on the next call (cancelling an
	/// already-installed token during execution works). Assign the properties before execution
	/// and, for concurrency, only ever call <c>Cancel()</c> on an already-installed token source;
	/// mutating the properties from another thread while a script runs is not supported.
	///
	/// Enforcement is by VM instruction, so it preempts only Lua code: a blocking or long-running
	/// CLR callback the script invokes is not interrupted by the instruction counter. A host that
	/// exposes such callbacks should make them observe <see cref="CancellationToken"/> (or their
	/// own timeout) so a stuck callback cannot outlast the run.
	/// </summary>
	public sealed class ExecutionLimits
	{
		/// <summary>Max VM instructions across the Script's executions; null = unlimited.</summary>
		public long? InstructionBudget { get; set; }

		/// <summary>Checked every 1024 instructions; cancellation terminates the script.</summary>
		public CancellationToken CancellationToken { get; set; }

		internal long ExecutedInstructions;

		/// <summary>Instructions executed since construction or the last Reset().</summary>
		public long Executed { get { return ExecutedInstructions; } }

		internal bool Enabled
		{
			get { return InstructionBudget.HasValue || CancellationToken.CanBeCanceled; }
		}

		public void Reset()
		{
			ExecutedInstructions = 0;
		}
	}
}
