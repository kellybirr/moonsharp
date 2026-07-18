using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class IntegerTests
	{
		private static string Str(string code) { return Script.RunString(code).String; }
		private static bool Bool(string code) { return Script.RunString(code).Boolean; }
		private static double Num(string code) { return Script.RunString(code).Number; }

		[Test]
		public void Integer_Construct()
		{
			Assert.AreEqual("42", Str("return tostring(integer(42))"));
			Assert.AreEqual("-7", Str("return tostring(integer(-7))"));
			Assert.AreEqual("9223372036854775807", Str("return tostring(integer('9223372036854775807'))"));
			Assert.AreEqual("100", Str("return tostring(integer.new(100))"));
			Assert.AreEqual("100", Str("return tostring(integer.parse('100'))"));
		}

		[Test]
		public void Integer_Arithmetic()
		{
			Assert.AreEqual("5", Str("return tostring(integer(2) + integer(3))"));
			Assert.AreEqual("-1", Str("return tostring(integer(9) - integer(10))"));
			Assert.AreEqual("42", Str("return tostring(integer(6) * integer(7))"));
			Assert.AreEqual("3", Str("return tostring(integer(7) / integer(2))"));
			Assert.AreEqual("1", Str("return tostring(integer(7) % integer(2))"));
			Assert.AreEqual("-5", Str("return tostring(-integer(5))"));
		}

		[Test]
		public void Integer_TrapsOnOverflow()
		{
			// Checked arithmetic between two integers: overflow raises instead of wrapping.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer('9223372036854775807') + integer(1)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer('-9223372036854775808') - integer(1)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer('9223372036854775807') * integer(2)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(1) / integer(0)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(1) % integer(0)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return -integer('-9223372036854775808')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer.abs(integer('-9223372036854775808'))"));
		}

		[Test]
		public void Integer_MixedWithLuaNumbers()
		{
			// integer op number promotes the integer to a plain Lua number (double), so the
			// result is a number and fractional values are preserved.
			Assert.AreEqual("number", Str("return type(integer(10) + 5)"));
			Assert.AreEqual(15.0, Num("return integer(10) + 5"));
			Assert.AreEqual(15.0, Num("return 5 + integer(10)"));
			Assert.AreEqual(15.5, Num("return integer(10) + 5.5"));
			Assert.AreEqual(30.0, Num("return integer(10) * 3"));
			Assert.AreEqual(25.0, Num("return integer(10) * 2.5"));
			Assert.AreEqual(2.5, Num("return integer(10) / 4"));
			Assert.AreEqual(7.5, Num("return 15 - integer(10) + 2.5"));
			Assert.AreEqual(1.5, Num("return integer(7) % 2.75"), 1e-12);
		}

		[Test]
		public void Integer_MixedWithLuaNumbersUsesNumberSemantics()
		{
			// Because the integer converts to a number, number (IEEE) semantics apply to the
			// mixed expression: no 64-bit overflow trapping (the result is a double), and
			// division by a plain 0 yields inf rather than trapping.
			Assert.AreEqual("number", Str("return type(integer.max + 1)"));
			Assert.AreEqual(9.2233720368547758e18, Num("return integer.max + 1"));
			Assert.AreEqual(double.PositiveInfinity, Num("return integer(1) / 0"));
		}

		[Test]
		public void Integer_Comparisons()
		{
			Assert.IsTrue(Bool("return integer(5) < integer(10)"));
			Assert.IsTrue(Bool("return integer(10) > integer(5)"));
			Assert.IsTrue(Bool("return integer(5) <= integer(5)"));
			Assert.IsTrue(Bool("return integer(5) == integer(5)"));
			Assert.IsFalse(Bool("return integer(5) == integer(6)"));
			Assert.IsTrue(Bool("return integer(5) < 10"));
			Assert.IsTrue(Bool("return integer(-3) < 0"));
			// Mixed equality with a plain Lua number (both operand orders).
			Assert.IsTrue(Bool("return integer(5) == 5"));
			Assert.IsTrue(Bool("return 5 == integer(5)"));
			Assert.IsFalse(Bool("return integer(5) == 6"));
			Assert.IsTrue(Bool("return integer(5) ~= 6"));
		}

		[Test]
		public void Integer_HelpersAndConstants()
		{
			Assert.AreEqual("17", Str("return tostring(integer.abs(integer(-17)))"));
			Assert.AreEqual(123.0, Script.RunString("return integer.tonumber(integer(123))").Number);
			Assert.AreEqual("9223372036854775807", Str("return tostring(integer.max)"));
			Assert.AreEqual("-9223372036854775808", Str("return tostring(integer.min)"));
		}

		[Test]
		public void Integer_OutOfRangeOrBadInputRaises()
		{
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(2.5)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer('nope')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(1e300)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(0/0)"));
		}

		[Test]
		public void Integer_DecimalInterop()
		{
			// Cross-type equality and ordering by mathematical value (both operand orders).
			Assert.IsTrue(Bool("return integer(5) == decimal(5)"));
			Assert.IsTrue(Bool("return decimal(5) == integer(5)"));
			Assert.IsFalse(Bool("return integer(5) == decimal('5.5')"));
			Assert.IsTrue(Bool("return integer(5) < decimal('5.5')"));
			Assert.IsTrue(Bool("return decimal('4.5') < integer(5)"));

			// Conversion in both directions.
			Assert.AreEqual("5", Str("return tostring(integer(decimal('5')))"));
			Assert.AreEqual("5", Str("return tostring(decimal(integer(5)))"));
			Assert.AreEqual("-9223372036854775808", Str("return tostring(decimal(integer.min))"));

			// A fractional or out-of-range decimal has no integer representation.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(decimal('5.5'))"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(decimal.max)"));
		}

		[Test]
		public void Integer_DecimalArithmetic()
		{
			// integer op decimal promotes the integer to decimal (always exact) and yields a
			// decimal, in both operand orders, with decimal's checked (trapping) semantics.
			Assert.AreEqual("4.5", Str("return tostring(integer(3) * decimal('1.5'))"));
			Assert.AreEqual("4.5", Str("return tostring(decimal('1.5') * integer(3))"));
			Assert.AreEqual("2.5", Str("return tostring(decimal('10') / integer(4))"));
			Assert.AreEqual("2", Str("return tostring(integer(1) + decimal(1))"));
			Assert.AreEqual("8.5", Str("return tostring(integer(10) - decimal('1.5'))"));
			// System.Decimal preserves scale through remainder: 7 % 2.75 renders as 1.50.
			Assert.AreEqual("1.50", Str("return tostring(integer(7) % decimal('2.75'))"));
			Assert.AreEqual("0.1", Str("return tostring(integer(1) * decimal('0.1'))"));

			// The exact-promotion guarantee: integer.max survives the round trip unrounded.
			Assert.AreEqual("9223372036854775808", Str("return tostring(integer.max + decimal(1))"));

			// Decimal trap semantics apply to the mixed expression.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(1) / decimal(0)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal.max + integer(1)"));
		}

		[Test]
		public void Integer_NoRawClrExceptionsEscapeToScript()
		{
			// Parse is script-reachable as a static member of the registered userdata type;
			// unparseable or oversized input must raise a pcall-catchable script error, not a
			// raw FormatException/OverflowException/ArgumentException.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer.max.Parse('zz')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer.max.Parse('99999999999999999999')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(1) < 'abc'"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer.max.CompareTo({})"));
			Assert.IsFalse(Bool("return (pcall(function () return integer(1) < 'abc' end))"));

			// Error message reports the operand in Lua type terms, not CLR type names.
			var ex = Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return integer(1) < 'abc'"));
			StringAssert.Contains("string", ex.Message);
			StringAssert.DoesNotContain("String", ex.Message);
		}
	}
}
