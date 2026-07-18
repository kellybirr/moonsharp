MoonSharp       [![CI](../../actions/workflows/ci.yml/badge.svg)](../../actions/workflows/ci.yml) [![Build Status](https://img.shields.io/nuget/v/MoonSharp.svg)](https://www.nuget.org/packages/MoonSharp/)
=========
http://www.moonsharp.org   



A complete Lua solution written entirely in C# for the .NET, Mono, Xamarin and Unity3D platforms.

Features:
* 99% compatible with Lua 5.2 (with the only unsupported feature being weak tables support)
* Support for metalua style anonymous functions (lambda-style)
* Easy to use API
* **Debugger** support via Debug Adapter Protocol e.g. Visual Studio Code
* Runs on .NET 4.5, .NET Platform (formerly Core), Mono, Xamarin and Unity
* Runs on Ahead-of-time platforms like iOS
* Runs on IL2CPP converted code
* No external dependencies, implemented in as few targets as possible
* Easy and performant interop with CLR objects, with runtime code generation where supported
* Interop with methods, extension methods, overloads, fields, properties and indexers supported
* Support for the complete Lua standard library with very few exceptions (mostly located on the 'debug' module) and a few extensions (in the string library, mostly)
* Async method support
* Supports dumping/loading bytecode for obfuscation and quicker parsing at runtime
* An embedded JSON parser (with no dependencies) to convert between JSON and Lua tables
* Extended numeric types beyond the standard Lua number: `integer` (exact 64-bit integers with overflow trapping) and `decimal` (exact fixed-point decimals for currency-safe math) — see below
* Easy opt-out of Lua standard library modules to sandbox what scripts can access
* Easy to use error handling (script errors are exceptions)
* Support for coroutines, including invocation of coroutines as C# iterators 
* REPL interpreter, plus facilities to easily implement your own REPL in few lines of code
* Complete XML help, and walkthroughs on http://www.moonsharp.org

For highlights on differences between MoonSharp and standard Lua, see http://www.moonsharp.org/moonluadifferences.html

Please see http://www.moonsharp.org for downloads, infos, tutorials, etc.

## Extended numeric types: `integer` and `decimal`

This fork adds two exact numeric types on top of the standard Lua `number` (which is,
as in stock Lua 5.2, a binary double — so `0.1 + 0.2 ~= 0.3` and integers above 2^53
lose precision). Both are implemented as userdata types, are available in every
`CoreModules` preset including the hard sandbox, and never let a raw CLR exception
escape into the host: every error they raise can be caught from script with `pcall`.

### `decimal` — exact fixed-point (backed by `System.Decimal`)

For currency-safe math with no floating-point drift:

```lua
local price    = decimal('19.99')
local total    = price * decimal('5')          -- 99.95, exactly
local discount = total * decimal('0.05')
print(decimal.round(total - discount, 2))      -- 94.95
print(decimal('0.1') + decimal('0.2'))         -- 0.3 (exactly)
```

* `decimal(x)` / `decimal.new(x)` — construct from a decimal, an integer, a number or a string
* `decimal.parse(s)`, `decimal.tostring(d)`, `decimal.tonumber(d)`
* `decimal.abs(d)`, `decimal.floor(d)`, `decimal.ceiling(d)`, `decimal.round(d [, places])` (places 0–28)
* Constants: `decimal.zero`, `decimal.one`, `decimal.min`, `decimal.max`

Arithmetic is *checked*: overflow, division/modulo by zero, and operands with no
decimal representation (`inf`, `nan`, out-of-range doubles) raise a script error
instead of silently producing a wrong value.

### `integer` — exact 64-bit integers (backed by `System.Int64`)

For IDs, counters and money-in-cents style math that must not lose precision or
silently wrap:

```lua
local big = integer('9223372036854775807')     -- integer.max; exact, unlike a number
print(integer(7) / integer(2))                 -- 3   (truncating integer division)
print(integer.max + integer(1))                -- error: integer overflow in '+'
```

* `integer(x)` / `integer.new(x)` — construct from an integer, an integer-valued decimal,
  an integer-valued number or a string; a fractional or out-of-range value raises an error
  (`integer(2.5)` is an error, not a truncation)
* `integer.parse(s)`, `integer.tostring(i)`, `integer.tonumber(i)`, `integer.abs(i)`
* Constants: `integer.min`, `integer.max`

Arithmetic between two integers is *checked*: overflow, underflow, division/modulo by
zero, and `-integer.min` / `abs(integer.min)` raise a script error instead of wrapping.

### Mixing types: results always widen

Mixed-type expressions promote toward the representation that preserves the value, so
no fractional part is ever silently lost:

| Expression            | Result type | Notes |
|-----------------------|-------------|-------|
| `integer op integer`  | `integer`   | checked; traps on overflow and division by zero |
| `integer op number`   | `number`    | the integer becomes a double; plain Lua semantics apply (`integer(10) / 4 == 2.5`, `integer(1) / 0 == inf`, no overflow trapping) |
| `integer op decimal`  | `decimal`   | the integer converts exactly; decimal's checked semantics apply |
| `decimal op number`   | `decimal`   | the double converts to decimal; traps on `inf`/`nan`/out-of-range |
| `decimal op decimal`  | `decimal`   | checked |

Because the `integer op number` form uses plain number semantics, trapped 64-bit
arithmetic requires both operands to be integers: `integer.max + 1` is a (rounded)
number, while `integer.max + integer(1)` raises.

Equality and ordering compare by mathematical value across all three types, in any
operand order: `integer(5) == 5`, `decimal(5) == integer(5)` and `integer(5) <
decimal('5.5')` all behave as expected. (Stock Lua would never consult `__eq` across
types; this is a deliberate usability extension.) Table keys are unaffected — a
`number` key, an `integer` key and a `decimal` key remain distinct slots.

Explicit conversions are checked both ways: `integer(decimal_value)` requires an
integral value within Int64 range, while `decimal(integer_value)` always succeeds
since every Int64 is exactly representable as a decimal.

From the host side the types are `MoonSharp.Interpreter.IntegerType` and
`MoonSharp.Interpreter.DecimalType` (immutable structs wrapping `long` /
`decimal`), registered via `CoreModules.Integer` and `CoreModules.Decimal`.

## Unity Package (UPM)

### Build package locally

```bash
tools/upm/stage-local-package.sh 3.0.0-local
cd .upm-staging/org.moonsharp.moonsharp
npm pack
```

This produces a tarball like:

`org.moonsharp.moonsharp-3.0.0-local.tgz`

### Install in Unity

Install from version branch:

1. In your Unity project's `Packages/manifest.json`, add:
   `"org.moonsharp.moonsharp": "https://github.com/moonsharp-devs/moonsharp.git?path=/interpreter#upm/v3.0"`
2. If you just want to pin to a major version (3 instead 3.0), use branches like:
   `upm/v3`
3. The VSCode debugger is a separate package and can be added with:
   `"org.moonsharp.debugger.vscode": "https://github.com/moonsharp-devs/moonsharp.git?path=/debugger/vscode#upm/v3.0"`

<blockquote>
<p>[!NOTE]
Beta branches are available with names like `upm/beta/v3.0`
</p></blockquote>

**License**

The program and libraries are released under a 3-clause BSD license - see the license section.

Parts of the string library are based on the KopiLua project (https://github.com/NLua/KopiLua).
Debugger icons are from the Eclipse project (https://www.eclipse.org/).


**Usage**

Use of the library is easy as:

```C#
double MoonSharpFactorial()
{
	string script = @"    
		-- defines a factorial function
		function fact (n)
			if (n == 0) then
				return 1
			else
				return n*fact(n - 1)
			end
		end

	return fact(5)";

	DynValue res = Script.RunString(script);
	return res.Number;
}
```

For more in-depth tutorials, samples, etc. please refer to http://www.moonsharp.org/getting_started.html
