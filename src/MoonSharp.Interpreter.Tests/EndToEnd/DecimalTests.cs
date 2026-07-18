using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class DecimalTests
	{
		private static string Str(string code)
		{
			return Script.RunString(code).String;
		}

		private static bool Bool(string code)
		{
			return Script.RunString(code).Boolean;
		}

		[Test]
		public void Decimal_ConstructFromNumberAndString()
		{
			Assert.AreEqual("42", Str("return tostring(decimal(42))"));
			Assert.AreEqual("-7", Str("return tostring(decimal(-7))"));
			Assert.AreEqual("3.14", Str("return tostring(decimal('3.14'))"));
		}

		[Test]
		public void Decimal_NewAndParse()
		{
			Assert.AreEqual("100", Str("return tostring(decimal.new(100))"));
			Assert.AreEqual("99.99", Str("return tostring(decimal.parse('99.99'))"));
		}

		[Test]
		public void Decimal_Addition()
		{
			// Classic floating-point failure: 0.1 + 0.2 should be exactly 0.3
			Assert.AreEqual("0.3", Str("return tostring(decimal('0.1') + decimal('0.2'))"));
		}

		[Test]
		public void Decimal_Subtraction()
		{
			Assert.AreEqual("0.01", Str("return tostring(decimal('1.03') - decimal('1.02'))"));
		}

		[Test]
		public void Decimal_Multiplication()
		{
			Assert.AreEqual("19.990", Str("return tostring(decimal('9.995') * decimal('2'))"));
		}

		[Test]
		public void Decimal_Division()
		{
			Assert.AreEqual("3.3333333333333333333333333333", Str("return tostring(decimal('10') / decimal('3'))"));
		}

		[Test]
		public void Decimal_Modulo()
		{
			Assert.AreEqual("1.5", Str("return tostring(decimal('7.5') % decimal('3'))"));
		}

		[Test]
		public void Decimal_UnaryMinus()
		{
			Assert.AreEqual("-5.5", Str("return tostring(-decimal('5.5'))"));
		}

		[Test]
		public void Decimal_MixedWithLuaNumbers()
		{
			Assert.AreEqual("15.5", Str("return tostring(decimal('10.5') + 5)"));
			Assert.AreEqual("15.5", Str("return tostring(5 + decimal('10.5'))"));
			Assert.AreEqual("31.5", Str("return tostring(decimal('10.5') * 3)"));
		}

		[Test]
		public void Decimal_Comparisons()
		{
			Assert.IsTrue(Bool("return decimal('5.5') < decimal('10.5')"));
			Assert.IsTrue(Bool("return decimal('10.5') > decimal('5.5')"));
			Assert.IsTrue(Bool("return decimal('5.5') <= decimal('5.5')"));
			Assert.IsTrue(Bool("return decimal('5.5') == decimal('5.5')"));
			Assert.IsFalse(Bool("return decimal('5.5') == decimal('6.5')"));
			Assert.IsTrue(Bool("return decimal('5') < 10"));
			// Mixed equality with a plain Lua number (both operand orders).
			Assert.IsTrue(Bool("return decimal('5') == 5"));
			Assert.IsTrue(Bool("return 5 == decimal('5')"));
			Assert.IsFalse(Bool("return decimal('5.5') == 5"));
			Assert.IsTrue(Bool("return decimal('5.5') ~= 5"));
		}

		[Test]
		public void Decimal_Abs()
		{
			Assert.AreEqual("17.5", Str("return tostring(decimal.abs(decimal('-17.5')))"));
		}

		[Test]
		public void Decimal_Floor()
		{
			Assert.AreEqual("3", Str("return tostring(decimal.floor(decimal('3.7')))"));
			Assert.AreEqual("-4", Str("return tostring(decimal.floor(decimal('-3.2')))"));
		}

		[Test]
		public void Decimal_Ceiling()
		{
			Assert.AreEqual("4", Str("return tostring(decimal.ceiling(decimal('3.2')))"));
			Assert.AreEqual("-3", Str("return tostring(decimal.ceiling(decimal('-3.7')))"));
		}

		[Test]
		public void Decimal_Round()
		{
			Assert.AreEqual("3.14", Str("return tostring(decimal.round(decimal('3.14159'), 2))"));
			Assert.AreEqual("3", Str("return tostring(decimal.round(decimal('3.14159')))"));
			Assert.AreEqual("3.5", Str("return tostring(decimal.round(decimal('3.45'), 1))"));
		}

		[Test]
		public void Decimal_ToNumber()
		{
			Assert.AreEqual(123.45, Script.RunString("return decimal.tonumber(decimal('123.45'))").Number, 0.0001);
		}

		[Test]
		public void Decimal_Constants()
		{
			Assert.AreEqual("0", Str("return tostring(decimal.zero)"));
			Assert.AreEqual("1", Str("return tostring(decimal.one)"));
			Assert.AreEqual("79228162514264337593543950335", Str("return tostring(decimal.max)"));
			Assert.AreEqual("-79228162514264337593543950335", Str("return tostring(decimal.min)"));
		}

		[Test]
		public void Decimal_CurrencySafe()
		{
			// A real percentage calculation: 19.99 x 5 = 99.95, less a 5% discount.
			// 99.95 * 0.05 = 4.9975 -> 99.95 - 4.9975 = 94.9525 -> 94.95 (rounded to cents).
			// The point is that the intermediate products stay exact, with no 0.1+0.2 drift.
			Assert.AreEqual("94.95", Str(@"
				local price = decimal('19.99')
				local qty = decimal('5')
				local total = price * qty          -- 99.95
				local discount = total * decimal('0.05')
				return tostring(decimal.round(total - discount, 2))
			"));
		}

		[Test]
		public void Decimal_OverflowTraps()
		{
			// Deterministic-trap semantics: overflow and divide-by-zero raise a script
			// error instead of escaping as a raw CLR exception.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal.max + decimal.one"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal.max * decimal('2')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal.max / decimal('0.5')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal('1') / decimal('0')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal('1') % decimal('0')"));
			// Out-of-range Lua number, both in the constructor and mixed arithmetic.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal(1e100)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal('1') + 1e100"));
		}

		[Test]
		public void Decimal_AbsMinIsSymmetric()
		{
			// Unlike a two's-complement integer (where abs(min) overflows), decimal is
			// sign-magnitude with a symmetric range, so abs(min) == max exactly.
			Assert.AreEqual("79228162514264337593543950335", Str("return tostring(decimal.abs(decimal.min))"));
		}

		[Test]
		public void Decimal_CompareWithOutOfRangeNumber()
		{
			// Comparison against a double beyond decimal's range must order correctly,
			// not overflow the decimal cast.
			Assert.IsTrue(Bool("return decimal('1') < 1e100"));
			Assert.IsFalse(Bool("return decimal('1') > 1e100"));
		}

		[Test]
		public void Decimal_RoundRejectsBadArgument()
		{
			// A non-numeric places argument is a caller error, not a silent round-to-0.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal.round(decimal('3.14159'), 'xx')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal.round(decimal('3.14159'), false)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal.round(decimal('3.14159'), -1)"));
			// A numeric string coerces to a number, as Lua does elsewhere.
			Assert.AreEqual("3.14", Str("return tostring(decimal.round(decimal('3.14159'), '2'))"));
			// ...but a fractional or out-of-range coerced value is still rejected.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal.round(decimal('3.14159'), '2.5')"));
		}

		[Test]
		public void Decimal_StaticParseDoesNotEscapeRawClrException()
		{
			// decimal(x).Parse(...) is script-reachable as a static member of the registered
			// userdata type; unparseable or out-of-range input previously escaped as a raw
			// FormatException/OverflowException rather than a pcall-catchable script error.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal(0).Parse('zz')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal(0).Parse('1e400')"));
			Assert.IsFalse(Bool("return (pcall(function () return decimal(0).Parse('zz') end))"));
		}

		[Test]
		public void Decimal_ComparisonWithNonNumberIsPcallCatchable()
		{
			// Previously escaped as a raw ArgumentException from NumericInterop.Compare.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal(1) < 'abc'"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal(1) < {}"));
			Assert.IsFalse(Bool("return (pcall(function () return decimal(1) < 'abc' end))"));

			// Error message reports the operand in Lua type terms, not CLR type names.
			var ex = Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return decimal(1) < 'abc'"));
			StringAssert.Contains("string", ex.Message);
			StringAssert.DoesNotContain("String", ex.Message);
		}
	}
}
