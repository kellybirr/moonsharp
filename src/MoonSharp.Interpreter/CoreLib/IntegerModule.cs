// Disable warnings about XML documentation
#pragma warning disable 1591

using System.Globalization;

namespace MoonSharp.Interpreter.CoreLib
{
	/// <summary>
	/// Class implementing the 'integer' fixed-width signed 64-bit integer package (a MoonSharp addition).
	///
	/// The module table is callable, so <c>integer(x)</c> is a shortcut for <c>integer.new(x)</c>.
	/// Values are <see cref="IntegerType"/> userdata supporting the usual arithmetic and comparison
	/// operators with checked (trapping) semantics: overflow and division by zero raise a script error.
	/// </summary>
	[MoonSharpModule(Namespace = "integer")]
	public class IntegerModule
	{
		public static void MoonSharpInit(Table globalTable, Table moduleTable)
		{
			UserData.RegisterType<IntegerType>();

			moduleTable.Set("min", UserData.Create(new IntegerType(long.MinValue)));
			moduleTable.Set("max", UserData.Create(new IntegerType(long.MaxValue)));

			// Allow 'integer(x)' as a shortcut for 'integer.new(x)'.
			Table meta = new Table(globalTable.OwnerScript);
			meta.Set("__call", DynValue.NewCallback((executionContext, args) =>
				UserData.Create(ToInteger(args[1], "integer")), "integer"));
			moduleTable.MetaTable = meta;
		}

		/// <summary>
		/// Coerces a script value (integer, integer-valued decimal, integer-valued number, or
		/// decimal string) into an IntegerType.
		/// </summary>
		private static IntegerType ToInteger(DynValue v, string funcName)
		{
			switch (v.Type)
			{
				case DataType.UserData:
					if (v.UserData != null && v.UserData.Object is IntegerType)
						return (IntegerType)v.UserData.Object;
					if (v.UserData != null && v.UserData.Object is DecimalType)
					{
						decimal d = ((DecimalType)v.UserData.Object).Value;
						if (d != decimal.Truncate(d))
							throw new ScriptRuntimeException("bad argument to '{0}' (decimal value {1} has no integer representation)", funcName, d.ToString(CultureInfo.InvariantCulture));
						if (d < long.MinValue || d > long.MaxValue)
							throw new ScriptRuntimeException("bad argument to '{0}' (decimal value {1} is out of range for integer)", funcName, d.ToString(CultureInfo.InvariantCulture));
						return new IntegerType((long)d);
					}
					throw ScriptRuntimeException.BadArgument(0, funcName, "integer, decimal, number or string expected, got userdata");
				case DataType.Number:
				{
					double d = v.Number;
					if (double.IsNaN(d) || double.IsInfinity(d) || d != System.Math.Floor(d))
						throw new ScriptRuntimeException("bad argument to '{0}' (number has no integer representation)", funcName);
					if (d < -9223372036854775808.0 || d >= 9223372036854775808.0)
						throw new ScriptRuntimeException("bad argument to '{0}' (number is out of range for integer)", funcName);
					return new IntegerType((long)d);
				}
				case DataType.String:
				{
					long parsed;
					if (!long.TryParse(v.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
						throw new ScriptRuntimeException("bad argument to '{0}' (cannot parse '{1}' as an integer)", funcName, v.String);
					return new IntegerType(parsed);
				}
				default:
					throw ScriptRuntimeException.BadArgument(0, funcName, "integer, decimal, number or string expected, got " + v.Type.ToLuaTypeString());
			}
		}

		[MoonSharpModuleMethod]
		public static DynValue @new(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToInteger(args[0], "integer.new"));
		}

		[MoonSharpModuleMethod]
		public static DynValue parse(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue s = args.AsType(0, "integer.parse", DataType.String, false);
			long parsed;
			if (!long.TryParse(s.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("bad argument to 'integer.parse' (cannot parse '{0}' as an integer)", s.String);
			return UserData.Create(new IntegerType(parsed));
		}

		[MoonSharpModuleMethod]
		public static DynValue tostring(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewString(ToInteger(args[0], "integer.tostring").ToString());
		}

		[MoonSharpModuleMethod]
		public static DynValue tonumber(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewNumber(ToInteger(args[0], "integer.tonumber").ToNumber());
		}

		[MoonSharpModuleMethod]
		public static DynValue abs(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToInteger(args[0], "integer.abs").Abs());
		}
	}
}
