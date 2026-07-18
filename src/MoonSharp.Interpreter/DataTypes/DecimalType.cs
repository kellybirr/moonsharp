using System;
using System.Globalization;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// A fixed-point decimal type exposed to scripts as the 'decimal' userdata type.
	/// Backed by <see cref="System.Decimal"/> for currency-safe calculations without
	/// floating-point rounding errors.
	///
	/// Instances are immutable. Arithmetic is CHECKED: overflow, division by zero, and
	/// operands that fall outside <see cref="System.Decimal"/>'s range raise a
	/// <see cref="ScriptRuntimeException"/> instead of throwing a raw CLR exception that
	/// would escape the interpreter (deterministic-trap semantics).
	///
	/// Arithmetic and comparison operators are provided both between two decimals and
	/// between a decimal and a Lua number (double), so expressions such as
	/// <c>decimal(10) + 5</c> work directly from script.
	/// </summary>
	public struct DecimalType : IEquatable<DecimalType>, IComparable<DecimalType>, IComparable
	{
		/// <summary>
		/// Gets the underlying <see cref="System.Decimal"/> value.
		/// </summary>
		public decimal Value { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DecimalType"/> struct wrapping the given value.
		/// </summary>
		public DecimalType(decimal value)
		{
			Value = value;
		}

		/// <summary>
		/// Parses a string into a DecimalType. Raises a script error (never a raw CLR
		/// FormatException/OverflowException, which Lua's pcall cannot catch) on unparseable or
		/// out-of-range input — this method is script-reachable as a static member of the
		/// registered userdata type.
		/// </summary>
		public static DecimalType Parse(string s)
		{
			decimal parsed;
			if (!decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("cannot parse '{0}' as a decimal", s);
			return new DecimalType(parsed);
		}

		#region Arithmetic operators (checked/trapping)

		/// <summary>
		/// Converts a Lua-number (double) operand to decimal, trapping non-finite values and
		/// magnitudes outside decimal's range instead of throwing a raw CLR exception.
		/// </summary>
		private static decimal FromDouble(double d, string op)
		{
			if (double.IsNaN(d) || double.IsInfinity(d))
				throw new ScriptRuntimeException("decimal arithmetic error in '{0}': operand has no decimal representation ({1})", op, d.ToString(CultureInfo.InvariantCulture));
			try { return (decimal)d; }
			catch (OverflowException) { throw new ScriptRuntimeException("decimal overflow in '{0}': number {1} is out of range for decimal", op, d.ToString(CultureInfo.InvariantCulture)); }
		}

		private static string Fmt(decimal d)
		{
			return d.ToString(CultureInfo.InvariantCulture);
		}

		private static DecimalType Add(decimal a, decimal b)
		{
			try { return new DecimalType(a + b); }
			catch (OverflowException) { throw new ScriptRuntimeException("decimal overflow in '+' ({0} + {1})", Fmt(a), Fmt(b)); }
		}

		private static DecimalType Sub(decimal a, decimal b)
		{
			try { return new DecimalType(a - b); }
			catch (OverflowException) { throw new ScriptRuntimeException("decimal overflow in '-' ({0} - {1})", Fmt(a), Fmt(b)); }
		}

		private static DecimalType Mul(decimal a, decimal b)
		{
			try { return new DecimalType(a * b); }
			catch (OverflowException) { throw new ScriptRuntimeException("decimal overflow in '*' ({0} * {1})", Fmt(a), Fmt(b)); }
		}

		private static DecimalType Div(decimal a, decimal b)
		{
			if (b == 0m)
				throw new ScriptRuntimeException("decimal division by zero");
			try { return new DecimalType(a / b); }
			catch (OverflowException) { throw new ScriptRuntimeException("decimal overflow in '/' ({0} / {1})", Fmt(a), Fmt(b)); }
		}

		private static DecimalType Mod(decimal a, decimal b)
		{
			if (b == 0m)
				throw new ScriptRuntimeException("decimal modulo by zero");
			try { return new DecimalType(a % b); }
			catch (OverflowException) { throw new ScriptRuntimeException("decimal overflow in '%' ({0} % {1})", Fmt(a), Fmt(b)); }
		}

		public static DecimalType operator +(DecimalType a, DecimalType b) { return Add(a.Value, b.Value); }
		public static DecimalType operator +(DecimalType a, double b) { return Add(a.Value, FromDouble(b, "+")); }
		public static DecimalType operator +(double a, DecimalType b) { return Add(FromDouble(a, "+"), b.Value); }

		public static DecimalType operator -(DecimalType a, DecimalType b) { return Sub(a.Value, b.Value); }
		public static DecimalType operator -(DecimalType a, double b) { return Sub(a.Value, FromDouble(b, "-")); }
		public static DecimalType operator -(double a, DecimalType b) { return Sub(FromDouble(a, "-"), b.Value); }

		public static DecimalType operator *(DecimalType a, DecimalType b) { return Mul(a.Value, b.Value); }
		public static DecimalType operator *(DecimalType a, double b) { return Mul(a.Value, FromDouble(b, "*")); }
		public static DecimalType operator *(double a, DecimalType b) { return Mul(FromDouble(a, "*"), b.Value); }

		public static DecimalType operator /(DecimalType a, DecimalType b) { return Div(a.Value, b.Value); }
		public static DecimalType operator /(DecimalType a, double b) { return Div(a.Value, FromDouble(b, "/")); }
		public static DecimalType operator /(double a, DecimalType b) { return Div(FromDouble(a, "/"), b.Value); }

		public static DecimalType operator %(DecimalType a, DecimalType b) { return Mod(a.Value, b.Value); }
		public static DecimalType operator %(DecimalType a, double b) { return Mod(a.Value, FromDouble(b, "%")); }
		public static DecimalType operator %(double a, DecimalType b) { return Mod(FromDouble(a, "%"), b.Value); }

		// Negation never overflows: decimal is sign-magnitude with a symmetric range.
		public static DecimalType operator -(DecimalType a) { return new DecimalType(-a.Value); }

		#endregion

		#region Comparison operators

		public static bool operator ==(DecimalType a, DecimalType b) { return a.Value == b.Value; }
		public static bool operator !=(DecimalType a, DecimalType b) { return a.Value != b.Value; }
		public static bool operator <(DecimalType a, DecimalType b) { return a.Value < b.Value; }
		public static bool operator <=(DecimalType a, DecimalType b) { return a.Value <= b.Value; }
		public static bool operator >(DecimalType a, DecimalType b) { return a.Value > b.Value; }
		public static bool operator >=(DecimalType a, DecimalType b) { return a.Value >= b.Value; }

		public static bool operator ==(DecimalType a, double b) { return (double)a.Value == b; }
		public static bool operator ==(double a, DecimalType b) { return a == (double)b.Value; }
		public static bool operator !=(DecimalType a, double b) { return (double)a.Value != b; }
		public static bool operator !=(double a, DecimalType b) { return a != (double)b.Value; }
		public static bool operator <(DecimalType a, double b) { return (double)a.Value < b; }
		public static bool operator <(double a, DecimalType b) { return a < (double)b.Value; }
		public static bool operator <=(DecimalType a, double b) { return (double)a.Value <= b; }
		public static bool operator <=(double a, DecimalType b) { return a <= (double)b.Value; }
		public static bool operator >(DecimalType a, double b) { return (double)a.Value > b; }
		public static bool operator >(double a, DecimalType b) { return a > (double)b.Value; }
		public static bool operator >=(DecimalType a, double b) { return (double)a.Value >= b; }
		public static bool operator >=(double a, DecimalType b) { return a >= (double)b.Value; }

		#endregion

		/// <summary>
		/// Returns the absolute value. Never overflows: decimal's range is symmetric, so
		/// <c>abs(decimal.min)</c> is exactly <c>decimal.max</c>.
		/// </summary>
		public DecimalType Abs()
		{
			return new DecimalType(Math.Abs(Value));
		}

		/// <summary>
		/// Returns the largest integer less than or equal to this value.
		/// </summary>
		public DecimalType Floor()
		{
			return new DecimalType(Math.Floor(Value));
		}

		/// <summary>
		/// Returns the smallest integer greater than or equal to this value.
		/// </summary>
		public DecimalType Ceiling()
		{
			return new DecimalType(Math.Ceiling(Value));
		}

		/// <summary>
		/// Rounds to the specified number of decimal places (0-28).
		/// </summary>
		public DecimalType Round(int decimals = 0)
		{
			return new DecimalType(Math.Round(Value, decimals, MidpointRounding.AwayFromZero));
		}

		/// <summary>
		/// Converts to a double-precision Lua number. May lose precision.
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
		/// Returns the string representation of the value.
		/// </summary>
		public override string ToString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override bool Equals(object obj)
		{
			if (obj is DecimalType) return Value == ((DecimalType)obj).Value;
			// Value-equal to any other numeric operand (decimal(5) == 5).
			return NumericInterop.AreEqual(Value, obj);
		}

		public bool Equals(DecimalType other)
		{
			return Value == other.Value;
		}

		public int CompareTo(DecimalType other)
		{
			return Value.CompareTo(other.Value);
		}

		/// <summary>
		/// Non-generic comparison, used by the runtime to dispatch comparison metamethods.
		/// Supports comparison against another DecimalType or a Lua number. Floating-point
		/// operands are compared in double space so out-of-range magnitudes order correctly
		/// instead of overflowing the decimal cast.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is DecimalType)
				return Value.CompareTo(((DecimalType)obj).Value);
			return NumericInterop.Compare(Value, obj);
		}

		public override int GetHashCode()
		{
			// Value-based so it stays consistent with cross-type equality (decimal(5) == 5).
			return NumericInterop.ValueHashCode(Value);
		}
	}
}
