using System;
using System.Globalization;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// A fixed-width signed 64-bit integer exposed to scripts as the 'integer' userdata type.
	/// Backed by <see cref="System.Int64"/>. Arithmetic between two integers is CHECKED:
	/// overflow, underflow, and division by zero raise a <see cref="ScriptRuntimeException"/>
	/// instead of silently wrapping (deterministic-trap semantics, matching
	/// <see cref="DecimalType"/>).
	///
	/// Mixed-type arithmetic promotes toward the "wider" representation so no fractional part
	/// is silently lost: integer op number yields a plain Lua number (double), and
	/// integer op decimal yields a decimal. Comparison and equality metamethods are dispatched
	/// through <see cref="IComparable"/> and <see cref="object.Equals(object)"/>.
	/// </summary>
	public struct IntegerType : IEquatable<IntegerType>, IComparable<IntegerType>, IComparable
	{
		/// <summary>
		/// Gets the underlying <see cref="System.Int64"/> value.
		/// </summary>
		public long Value { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="IntegerType"/> struct wrapping the given value.
		/// </summary>
		public IntegerType(long value)
		{
			Value = value;
		}

		/// <summary>
		/// Parses a decimal string into an integer. Raises a script error (never a raw CLR
		/// FormatException/OverflowException, which Lua's pcall cannot catch) on unparseable
		/// input — this method is script-reachable as a static member of the registered userdata type.
		/// </summary>
		public static IntegerType Parse(string s)
		{
			long parsed;
			if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("cannot parse '{0}' as an integer", s);
			return new IntegerType(parsed);
		}

		#region Arithmetic operators (checked/trapping)

		private static IntegerType Add(long a, long b)
		{
			try { return new IntegerType(checked(a + b)); }
			catch (OverflowException) { throw new ScriptRuntimeException("integer overflow in '+' ({0} + {1})", a, b); }
		}

		private static IntegerType Sub(long a, long b)
		{
			try { return new IntegerType(checked(a - b)); }
			catch (OverflowException) { throw new ScriptRuntimeException("integer overflow in '-' ({0} - {1})", a, b); }
		}

		private static IntegerType Mul(long a, long b)
		{
			try { return new IntegerType(checked(a * b)); }
			catch (OverflowException) { throw new ScriptRuntimeException("integer overflow in '*' ({0} * {1})", a, b); }
		}

		private static IntegerType Div(long a, long b)
		{
			if (b == 0)
				throw new ScriptRuntimeException("integer division by zero");
			if (a == long.MinValue && b == -1)
				throw new ScriptRuntimeException("integer overflow in '/' ({0} / {1})", a, b);
			return new IntegerType(a / b);
		}

		private static IntegerType Mod(long a, long b)
		{
			if (b == 0)
				throw new ScriptRuntimeException("integer modulo by zero");
			// min % -1 is mathematically 0, but the CLR raises OverflowException for it.
			if (a == long.MinValue && b == -1)
				return new IntegerType(0);
			return new IntegerType(a % b);
		}

		public static IntegerType operator +(IntegerType a, IntegerType b) { return Add(a.Value, b.Value); }
		public static IntegerType operator -(IntegerType a, IntegerType b) { return Sub(a.Value, b.Value); }
		public static IntegerType operator *(IntegerType a, IntegerType b) { return Mul(a.Value, b.Value); }
		public static IntegerType operator /(IntegerType a, IntegerType b) { return Div(a.Value, b.Value); }
		public static IntegerType operator %(IntegerType a, IntegerType b) { return Mod(a.Value, b.Value); }

		// Mixed integer/number arithmetic yields a plain Lua number: the integer operand is
		// promoted to double and standard IEEE number semantics apply, so no fractional part
		// is ever silently lost (integer(10) / 4 == 2.5). Note the trade-offs: magnitudes
		// above 2^53 lose precision in the promotion, and the checked/trapping semantics of
		// integer/integer arithmetic do NOT apply — integer.max + 1 is a (slightly rounded)
		// double, and integer(1) / 0 is inf, exactly as with plain numbers.
		public static double operator +(IntegerType a, double b) { return (double)a.Value + b; }
		public static double operator +(double a, IntegerType b) { return a + (double)b.Value; }
		public static double operator -(IntegerType a, double b) { return (double)a.Value - b; }
		public static double operator -(double a, IntegerType b) { return a - (double)b.Value; }
		public static double operator *(IntegerType a, double b) { return (double)a.Value * b; }
		public static double operator *(double a, IntegerType b) { return a * (double)b.Value; }
		public static double operator /(IntegerType a, double b) { return (double)a.Value / b; }
		public static double operator /(double a, IntegerType b) { return a / (double)b.Value; }
		public static double operator %(IntegerType a, double b) { return (double)a.Value % b; }
		public static double operator %(double a, IntegerType b) { return a % (double)b.Value; }

		// Mixed integer/decimal arithmetic yields a decimal: every Int64 converts to decimal
		// exactly, and the checked (trapping) decimal semantics apply. (Only this operand
		// order lives here — the runtime dispatches the metamethod from the first operand's
		// descriptor, so decimal-first overloads live on DecimalType.)
		public static DecimalType operator +(IntegerType a, DecimalType b) { return DecimalType.Add(a.Value, b.Value); }
		public static DecimalType operator -(IntegerType a, DecimalType b) { return DecimalType.Sub(a.Value, b.Value); }
		public static DecimalType operator *(IntegerType a, DecimalType b) { return DecimalType.Mul(a.Value, b.Value); }
		public static DecimalType operator /(IntegerType a, DecimalType b) { return DecimalType.Div(a.Value, b.Value); }
		public static DecimalType operator %(IntegerType a, DecimalType b) { return DecimalType.Mod(a.Value, b.Value); }

		public static IntegerType operator -(IntegerType a)
		{
			if (a.Value == long.MinValue)
				throw new ScriptRuntimeException("integer overflow in unary '-' ({0})", a.Value);
			return new IntegerType(-a.Value);
		}

		#endregion

		/// <summary>
		/// Returns the absolute value; traps for <c>integer.min</c> (whose absolute value is
		/// not representable in two's complement).
		/// </summary>
		public IntegerType Abs()
		{
			if (Value == long.MinValue)
				throw new ScriptRuntimeException("integer overflow in 'abs' ({0})", Value);
			return new IntegerType(Value < 0 ? -Value : Value);
		}

		/// <summary>
		/// Converts to a double-precision Lua number. Magnitudes above 2^53 lose precision.
		/// </summary>
		public double ToNumber()
		{
			return (double)Value;
		}

		/// <summary>
		/// Metamethod handler for 'tostring' / string coercion.
		/// </summary>
		[MoonSharpUserDataMetamethod("__tostring")]
		public string ToLuaString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Metamethod handler for '..' (concatenation). Lets an integer join a string the way a
		/// plain Lua number does — <c>"status " .. res.status</c> — instead of raising "attempt to
		/// concatenate a userdata value". Handles either operand order and keeps the exact integer
		/// text; see <see cref="NumericInterop.Concat"/>.
		/// </summary>
		[MoonSharpUserDataMetamethod("__concat")]
		public static DynValue LuaConcat(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return NumericInterop.Concat(args[0], args[1]);
		}

		/// <summary>
		/// Returns the string representation of the value.
		/// </summary>
		public override string ToString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override bool Equals(object obj)
		{
			if (obj is IntegerType) return Value == ((IntegerType)obj).Value;
			// Value-equal to any other numeric operand (integer(5) == 5 == decimal(5)).
			return NumericInterop.AreEqual(Value, obj);
		}

		public bool Equals(IntegerType other)
		{
			return Value == other.Value;
		}

		public int CompareTo(IntegerType other)
		{
			return Value.CompareTo(other.Value);
		}

		/// <summary>
		/// Non-generic comparison used by the runtime to dispatch the ordering metamethods.
		/// Supports comparison against another integer, a decimal, or a Lua number.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is IntegerType)
				return Value.CompareTo(((IntegerType)obj).Value);
			return NumericInterop.Compare(Value, obj);
		}

		public override int GetHashCode()
		{
			// Value-based so it stays consistent with cross-type equality (integer(5) == 5 == decimal(5)).
			return NumericInterop.ValueHashCode(Value);
		}
	}
}
