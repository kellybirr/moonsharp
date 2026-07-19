using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	// Covers the '..' (__concat) metamethod added to the integer and decimal userdata types,
	// so they concatenate with strings/numbers like plain Lua numbers instead of raising
	// "attempt to concatenate a userdata value".
	[TestFixture]
	public class ConcatMetamethodTests
	{
		private static string Str(string code) { return Script.RunString(code).String; }
		private static bool Bool(string code) { return Script.RunString(code).Boolean; }

		[Test]
		public void Integer_Concat_WithStringEitherSide()
		{
			Assert.AreEqual("status 200", Str("return 'status ' .. integer(200)"));
			Assert.AreEqual("200 ok", Str("return integer(200) .. ' ok'"));
		}

		[Test]
		public void Integer_Concat_WithNumberEitherSide()
		{
			Assert.AreEqual("105", Str("return integer(10) .. 5"));
			Assert.AreEqual("510", Str("return 5 .. integer(10)"));
		}

		[Test]
		public void Integer_Concat_WithInteger()
		{
			Assert.AreEqual("23", Str("return integer(2) .. integer(3)"));
		}

		[Test]
		public void Integer_Concat_NegativeKeepsSign()
		{
			Assert.AreEqual("x-7", Str("return 'x' .. integer(-7)"));
		}

		[Test]
		public void Decimal_Concat_PreservesScale()
		{
			// The whole point for money: the decimal keeps its full scale, never a lossy double.
			Assert.AreEqual("amount: 104.70", Str("return 'amount: ' .. decimal('104.70')"));
			Assert.AreEqual("104.70!", Str("return decimal('104.70') .. '!'"));
		}

		[Test]
		public void Mixed_Integer_Decimal_Concat()
		{
			Assert.AreEqual("12.5", Str("return integer(1) .. decimal('2.5')"));
			Assert.AreEqual("2.51", Str("return decimal('2.5') .. integer(1)"));
		}

		[Test]
		public void Chained_Concat()
		{
			// The res.status-style message shape from the product's default script template.
			Assert.AreEqual("endpoint returned 404: body",
				Str("local status = integer(404) return 'endpoint returned ' .. status .. ': body'"));
			Assert.AreEqual("a1b2", Str("return 'a' .. integer(1) .. 'b' .. integer(2)"));
		}

		[Test]
		public void Concat_WithNonConcatenable_RaisesLuaTypedError()
		{
			// Still errors exactly like stock Lua for a genuinely non-concatenable operand,
			// naming it in Lua type terms — and on whichever side is the offender.
			var boolEx = Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(1) .. true"));
			StringAssert.Contains("concatenate", boolEx.Message);
			StringAssert.Contains("boolean", boolEx.Message);

			var tblEx = Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(1) .. {}"));
			StringAssert.Contains("table", tblEx.Message);

			var nilEx = Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal('1') .. nil"));
			StringAssert.Contains("nil", nilEx.Message);

			var leftEx = Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return true .. integer(1)"));
			StringAssert.Contains("boolean", leftEx.Message);
		}

		[Test]
		public void Concat_Error_IsPcallCatchable()
		{
			// A concat type error is an ordinary ScriptRuntimeException — pcall catches it,
			// unlike an execution-limit ScriptTerminationException.
			Assert.IsFalse(Bool("local ok, err = pcall(function() return integer(1) .. {} end) return ok"));
			Assert.AreEqual("caught", Str(
				"local ok, err = pcall(function() return integer(1) .. nil end) " +
				"if ok then return 'no-error' else return 'caught' end"));
		}
	}
}
