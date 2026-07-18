// Disable warnings about XML documentation
#pragma warning disable 1591

using System.Globalization;

namespace MoonSharp.Interpreter.CoreLib
{
	/// <summary>
	/// Class implementing the 'decimal' fixed-point decimal package (a MoonSharp addition).
	///
	/// The module table is callable, so <c>decimal(x)</c> is a shortcut for <c>decimal.new(x)</c>.
	/// Values are <see cref="DecimalType"/> userdata backed by <see cref="System.Decimal"/>,
	/// supporting the usual arithmetic and comparison operators for currency-safe calculations.
	/// Arithmetic uses checked (trapping) semantics: overflow, division by zero, and operands
	/// out of decimal's range raise a script error rather than escaping as a raw CLR exception.
	/// </summary>
	[MoonSharpModule(Namespace = "decimal")]
	public class DecimalModule
	{
		public static void MoonSharpInit(Table globalTable, Table decimalTable)
		{
			UserData.RegisterType<DecimalType>();

			// Constants exposed directly as values on the module table.
			decimalTable.Set("zero", UserData.Create(new DecimalType(0m)));
			decimalTable.Set("one", UserData.Create(new DecimalType(1m)));
			decimalTable.Set("min", UserData.Create(new DecimalType(decimal.MinValue)));
			decimalTable.Set("max", UserData.Create(new DecimalType(decimal.MaxValue)));

			// Allow 'decimal(x)' as a shortcut for 'decimal.new(x)'.
			Table meta = new Table(globalTable.OwnerScript);
			meta.Set("__call", DynValue.NewCallback((executionContext, args) =>
			{
				return UserData.Create(ToDecimal(args[1], "decimal"));
			}, "decimal"));
			decimalTable.MetaTable = meta;
		}

		/// <summary>
		/// Coerces a script value (decimal or integer userdata, number, or string) into a DecimalType.
		/// </summary>
		private static DecimalType ToDecimal(DynValue v, string funcName)
		{
			switch (v.Type)
			{
				case DataType.UserData:
					if (v.UserData != null && v.UserData.Object is DecimalType)
						return (DecimalType)v.UserData.Object;
					// An integer always converts exactly: every Int64 value is representable as decimal.
					if (v.UserData != null && v.UserData.Object is IntegerType)
						return new DecimalType(((IntegerType)v.UserData.Object).Value);
					throw ScriptRuntimeException.BadArgument(0, funcName, "decimal, integer, number or string expected, got userdata");
				case DataType.Number:
				{
					double d = v.Number;
					if (double.IsNaN(d) || double.IsInfinity(d))
						throw new ScriptRuntimeException("bad argument to '{0}' (number has no decimal representation)", funcName);
					try { return new DecimalType((decimal)d); }
					catch (System.OverflowException) { throw new ScriptRuntimeException("bad argument to '{0}' (number is out of range for decimal)", funcName); }
				}
				case DataType.String:
				{
					decimal parsed;
					if (!decimal.TryParse(v.String.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
						throw new ScriptRuntimeException("bad argument to '{0}' (cannot parse '{1}' as a decimal)", funcName, v.String);
					return new DecimalType(parsed);
				}
				default:
					throw ScriptRuntimeException.BadArgument(0, funcName, "decimal, integer, number or string expected, got " + v.Type.ToLuaTypeString());
			}
		}

		[MoonSharpModuleMethod]
		public static DynValue @new(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToDecimal(args[0], "decimal.new"));
		}

		[MoonSharpModuleMethod]
		public static DynValue parse(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue s = args.AsType(0, "decimal.parse", DataType.String, false);
			decimal parsed;
			if (!decimal.TryParse(s.String.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("bad argument to 'decimal.parse' (cannot parse '{0}' as a decimal)", s.String);
			return UserData.Create(new DecimalType(parsed));
		}

		[MoonSharpModuleMethod]
		public static DynValue tostring(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewString(ToDecimal(args[0], "decimal.tostring").ToString());
		}

		[MoonSharpModuleMethod]
		public static DynValue tonumber(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewNumber(ToDecimal(args[0], "decimal.tonumber").ToNumber());
		}

		[MoonSharpModuleMethod]
		public static DynValue abs(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToDecimal(args[0], "decimal.abs").Abs());
		}

		[MoonSharpModuleMethod]
		public static DynValue floor(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToDecimal(args[0], "decimal.floor").Floor());
		}

		[MoonSharpModuleMethod]
		public static DynValue ceiling(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToDecimal(args[0], "decimal.ceiling").Ceiling());
		}

		[MoonSharpModuleMethod]
		public static DynValue round(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DecimalType d = ToDecimal(args[0], "decimal.round");
			// Second argument is optional; when present it must be a number (Lua's usual
			// numeric-string coercion applies, so "2" is accepted as 2). A value that is not
			// a number and not coercible to one (boolean, non-numeric string, table, ...) is
			// a caller error, not a silent "round to 0 places".
			DynValue placesArg = args.AsType(1, "decimal.round", DataType.Number, true);
			int places = 0;
			if (placesArg.Type == DataType.Number)
			{
				double p = placesArg.Number;
				if (p < 0 || p != System.Math.Floor(p) || p > 28)
					throw new ScriptRuntimeException("bad argument #2 to 'decimal.round' (integer 0-28 expected)");
				places = (int)p;
			}
			return UserData.Create(d.Round(places));
		}
	}
}
