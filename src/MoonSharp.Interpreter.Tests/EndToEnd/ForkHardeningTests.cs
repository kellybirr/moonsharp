using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	// Regression tests for the fork-hardening review findings:
	//  #1 a metamethod/member error thrown on the REFLECTION interop path must stay pcall-catchable
	//     (MethodInfo.Invoke wraps it in TargetInvocationException — the descriptor now unwraps it);
	//  #2 an execution-limit termination must not be swallowed by debug.debug()'s REPL loop;
	//  #7 a limit termination decorates a real source location (no bytecode:-1), even at budget 0.
	[TestFixture]
	public class ForkHardeningTests
	{
		public class Exploder
		{
			// Reflection-invoked (see access mode below); throws a script error that must surface
			// to Lua as a ScriptRuntimeException, not an escaped TargetInvocationException.
			public string Boom()
			{
				throw new ScriptRuntimeException("boom-from-reflection");
			}
		}

		[Test]
		public void ReflectionPath_MemberError_IsPcallCatchable()
		{
			UserData.RegisterType<Exploder>(InteropAccessMode.Reflection);

			Script script = new Script(CoreModules.Preset_SoftSandbox);
			script.Globals["o"] = UserData.Create(new Exploder());

			DynValue res = script.DoString(
				"local ok, err = pcall(function() return o.Boom(o) end) return ok, err");

			Assert.AreEqual(DataType.Tuple, res.Type);
			Assert.IsFalse(res.Tuple[0].Boolean, "pcall should have caught the reflection-path error");
			StringAssert.Contains("boom-from-reflection", res.Tuple[1].String);
		}

		[Test]
		public void Termination_NotSwallowedByDebugDebug()
		{
			// Preset_Complete includes the Debug module. debug.debug() enters a native REPL loop;
			// the fed line loops forever, the instruction budget trips, and the resulting
			// ScriptTerminationException must propagate OUT of the REPL to the host. Without the
			// fix it is caught as an ordinary InterpreterException, printed, and the loop repeats
			// on the same input — i.e. this test would hang rather than fail.
			Script script = new Script(CoreModules.Preset_Complete);
			script.Options.DebugInput = prompt => "while true do end";
			script.Options.DebugPrint = s => { };
			script.ExecutionLimits.InstructionBudget = 100000;

			Assert.Throws<ScriptTerminationException>(() => script.DoString("debug.debug()"));
		}

		[Test]
		public void Termination_DecoratesRealLocation()
		{
			// The four leading lines put the infinite loop on source line 4; the termination must
			// decorate that line, not the instruction before it (the pre-advance check pointed one
			// instruction too early).
			Script script = new Script(CoreModules.Preset_HardSandbox);
			script.ExecutionLimits.InstructionBudget = 5000;

			ScriptTerminationException ex = Assert.Throws<ScriptTerminationException>(
				() => script.DoString("\n\nlocal x = 0\nwhile true do x = x + 1 end"));

			Assert.IsNotNull(ex.DecoratedMessage);
			StringAssert.Contains("(4,", ex.DecoratedMessage);
			StringAssert.DoesNotContain("bytecode:-1", ex.DecoratedMessage);
		}

		[Test]
		public void ZeroBudget_TerminatesOnFirstInstruction_NoNegativeLocation()
		{
			// Budget 0 trips on the very first instruction; before the fix the pre-advance throw
			// decorated the location as bytecode:-1. It now advances first, so it is bytecode:0.
			Script script = new Script(CoreModules.Preset_HardSandbox);
			script.ExecutionLimits.InstructionBudget = 0;

			ScriptTerminationException ex = Assert.Throws<ScriptTerminationException>(
				() => script.DoString("return 1 + 1"));

			Assert.IsNotNull(ex.DecoratedMessage);
			StringAssert.DoesNotContain("bytecode:-1", ex.DecoratedMessage);
			StringAssert.Contains("bytecode:0", ex.DecoratedMessage);
		}
	}
}
