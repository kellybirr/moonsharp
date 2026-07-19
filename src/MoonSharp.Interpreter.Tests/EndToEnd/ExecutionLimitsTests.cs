using System.Threading;
using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class ExecutionLimitsTests
	{
		[Test]
		public void InstructionBudget_TripsOnInfiniteLoop()
		{
			Script script = new Script(CoreModules.Preset_HardSandbox);
			script.ExecutionLimits.InstructionBudget = 100000;
			ScriptTerminationException ex = Assert.Throws<ScriptTerminationException>(
				() => script.DoString("while true do end"));
			StringAssert.Contains("instruction budget exceeded", ex.Message);
		}

		[Test]
		public void Termination_CannotBeSwallowedByPcall()
		{
			Script script = new Script(CoreModules.Preset_SoftSandbox); // pcall available
			script.ExecutionLimits.InstructionBudget = 100000;
			Assert.Throws<ScriptTerminationException>(() => script.DoString(
				"local ok, err = pcall(function() while true do end end) return 'survived'"));
		}

		[Test]
		public void Termination_CannotBeSwallowedByXpcall()
		{
			Script script = new Script(CoreModules.Preset_SoftSandbox);
			script.ExecutionLimits.InstructionBudget = 100000;
			Assert.Throws<ScriptTerminationException>(() => script.DoString(
				"local ok, err = xpcall(function() while true do end end, function(e) return e end) return 'survived'"));
		}

		[Test]
		public void CancellationToken_TripsRunningScript()
		{
			Script script = new Script(CoreModules.Preset_HardSandbox);
			CancellationTokenSource cts = new CancellationTokenSource();
			cts.Cancel();
			script.ExecutionLimits.CancellationToken = cts.Token;
			Assert.Throws<ScriptTerminationException>(() => script.DoString(
				"local x = 0 for i = 1, 100000 do x = x + i end return x"));
		}

		[Test]
		public void NoLimits_BehaviorUnchanged()
		{
			Script script = new Script(CoreModules.Preset_HardSandbox);
			DynValue res = script.DoString("local x = 0 for i = 1, 1000 do x = x + i end return x");
			Assert.AreEqual(500500.0, res.Number);
		}

		[Test]
		public void Termination_CarriesDecoratedSourceLocation()
		{
			Script script = new Script(CoreModules.Preset_HardSandbox);
			script.ExecutionLimits.InstructionBudget = 50000;
			ScriptTerminationException ex = Assert.Throws<ScriptTerminationException>(
				() => script.DoString("\nwhile true do end"));
			Assert.IsNotNull(ex.DecoratedMessage);
			StringAssert.Contains("instruction budget exceeded", ex.DecoratedMessage);
		}

		[Test]
		public void Reset_AllowsReuseAfterConsumption()
		{
			Script script = new Script(CoreModules.Preset_HardSandbox);
			script.ExecutionLimits.InstructionBudget = 200000;
			script.DoString("local x = 0 for i = 1, 100 do x = x + i end");
			Assert.Greater(script.ExecutionLimits.Executed, 0);
			script.ExecutionLimits.Reset();
			DynValue res = script.DoString("return 1 + 1");
			Assert.AreEqual(2.0, res.Number);
		}
	}
}
