using System;
using System.Globalization;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// Shared value-equality and ordering logic for the numeric userdata types
	/// (currently <see cref="DecimalType"/>) and plain Lua numbers.
	///
	/// This makes the numeric types behave consistently under the '==', '&lt;', '&lt;=',
	/// '&gt;' and '&gt;=' metamethods: two numeric values compare by mathematical value
	/// regardless of which concrete type carries them, so <c>decimal(5) == 5</c> and
	/// <c>decimal(5) &lt; 6</c> behave as expected. (Stock Lua only consults '__eq' when
	/// both operands share a type, so cross-type numeric equality is a deliberate
	/// departure in favour of consistency.)
	///
	/// Integer-valued operands are compared exactly (via <see cref="Decimal"/>);
	/// comparisons involving a floating-point (double) operand are performed in double
	/// space, matching the precision trade-off already used for number operands.
	///
	/// Cross-type value equality is matched by a value-based <see cref="ValueHashCode"/>
	/// that the numeric types delegate their GetHashCode to, so the CLR equality contract
	/// (equal objects hash equally) holds across these types and against plain doubles.
	/// This is independent of Lua table keys, which key on <see cref="DynValue"/>: those
	/// short-circuit on data-type and userdata-descriptor mismatch before ever consulting
	/// these values, so distinct numeric types remain distinct table slots (as in stock
	/// Lua, where '__eq' never governs rawget).
	/// </summary>
	internal static class NumericInterop
	{
		/// <summary>
		/// Unwraps a numeric userdata struct to its underlying CLR numeric, or returns the
		/// value unchanged. Non-numeric values pass through and are rejected downstream.
		/// </summary>
		private static object Unwrap(object o)
		{
			if (o is DecimalType) return ((DecimalType)o).Value;
			return o;
		}

		private static bool IsFloat(object o)
		{
			return o is double || o is float;
		}

		private static bool IsNumeric(object o)
		{
			return o is double || o is float || o is decimal ||
				o is sbyte || o is byte || o is short || o is ushort ||
				o is int || o is uint || o is long || o is ulong;
		}

		private static double AsDouble(object o)
		{
			return Convert.ToDouble(o, CultureInfo.InvariantCulture);
		}

		// Exact for every remaining integer type and decimal itself. This relies on callers
		// taking the float branch first: Convert.ToDecimal would OverflowException on an
		// out-of-range double/float, so a double/float must never reach here.
		private static decimal AsDecimal(object o)
		{
			return Convert.ToDecimal(o, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Value-equality across the numeric types. Returns false (never throws) when the
		/// other operand is not numeric, so it is safe to call from object.Equals overrides.
		/// </summary>
		internal static bool AreEqual(object a, object b)
		{
			a = Unwrap(a);
			b = Unwrap(b);

			if (!IsNumeric(a) || !IsNumeric(b))
				return false;

			if (IsFloat(a) || IsFloat(b))
				return AsDouble(a) == AsDouble(b);

			return AsDecimal(a) == AsDecimal(b);
		}

		/// <summary>
		/// Value-based hash consistent with <see cref="AreEqual"/>: whenever two operands
		/// compare equal they hash equally, because equal real values map to the same double
		/// (equality is either performed in double space, or is exact between values that then
		/// convert to the same double). Unequal values may of course collide, as always. The
		/// numeric types delegate GetHashCode here so the CLR equality contract holds.
		/// </summary>
		internal static int ValueHashCode(object value)
		{
			return AsDouble(Unwrap(value)).GetHashCode();
		}

		/// <summary>
		/// Ordering across the numeric types, used to dispatch the comparison metamethods.
		/// Raises a pcall-catchable <see cref="ScriptRuntimeException"/> for a non-numeric operand
		/// (which cannot be ordered against a number) — never a raw CLR exception, which would
		/// escape the interpreter into the embedding host, and reporting the operand in Lua type
		/// terms ("string", "table", ...) rather than a CLR type name.
		/// </summary>
		internal static int Compare(object a, object b)
		{
			object ua = Unwrap(a);
			object ub = Unwrap(b);

			if (!IsNumeric(ua) || !IsNumeric(ub))
			{
				// Describe a numeric operand as Lua's "number"; the non-numeric one gets its
				// actual Lua type ("string", "table", ...). Matches Lua's own wording.
				string ta = IsNumeric(ua) ? "number" : ua.ToLuaTypeString();
				string tb = IsNumeric(ub) ? "number" : ub.ToLuaTypeString();
				throw new ScriptRuntimeException("attempt to compare " + ta + " with " + tb);
			}

			if (IsFloat(ua) || IsFloat(ub))
				return AsDouble(ua).CompareTo(AsDouble(ub));

			return AsDecimal(ua).CompareTo(AsDecimal(ub));
		}
	}
}
