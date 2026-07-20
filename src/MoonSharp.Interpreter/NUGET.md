# Coderz.MoonSharp.Plus

**A fork of [MoonSharp](http://www.moonsharp.org), created by Marco Mastropaolo.**
Fork maintained by Kelly Birr.

MoonSharp is a complete Lua interpreter written entirely in C#, and all the credit
for it belongs to Marco Mastropaolo and its contributors. This fork adds two exact
numeric types alongside the standard Lua `number`, for hosts that need money-safe
and precision-safe scripting. Everything the original library does, it still does;
the additions are strictly on top.

Ships a single `netstandard2.0` assembly with **no dependencies**, so it runs on
.NET 8/10, .NET Framework 4.6.1+, Mono and Unity without framework pinning.

---

## What this fork adds

Standard Lua `number` is a binary double, so `0.1 + 0.2 ~= 0.3` and integers above
2^53 lose precision. This fork adds two exact types that fix that. Both are
userdata types, available in every `CoreModules` preset including the hard
sandbox, and neither ever lets a raw CLR exception escape into the host — every
error they raise is catchable from script with `pcall`.

### `decimal` — exact fixed-point (`System.Decimal`)

```lua
local price    = decimal('19.99')
local total    = price * decimal('5')          -- 99.95, exactly
local discount = total * decimal('0.05')
print(decimal.round(total - discount, 2))      -- 94.95
print(decimal('0.1') + decimal('0.2'))         -- 0.3 (exactly)
```

### `integer` — exact 64-bit integers (`System.Int64`)

```lua
local big = integer('9223372036854775807')     -- integer.max; exact, unlike a number
print(integer(7) / integer(2))                 -- 3   (truncating integer division)
print(integer.max + integer(1))                -- error: integer overflow in '+'
```

Arithmetic on both types is *checked*: overflow, division or modulo by zero, and
values with no exact representation raise a script error instead of silently
producing a wrong answer.

Mixed-type expressions widen toward the representation that preserves the value,
so no fractional part is ever silently lost:

| Expression           | Result    |
|----------------------|-----------|
| `integer op integer` | `integer` (checked) |
| `integer op number`  | `number` (plain Lua semantics) |
| `integer op decimal` | `decimal` (checked) |
| `decimal op number`  | `decimal` (checked) |
| `decimal op decimal` | `decimal` (checked) |

From the host side these are `MoonSharp.Interpreter.IntegerType` and
`MoonSharp.Interpreter.DecimalType`, registered via `CoreModules.Integer` and
`CoreModules.Decimal`.

Full documentation of the numeric types:
https://github.com/kellybirr/moonsharp#extended-numeric-types-integer-and-decimal

---

## Usage

```csharp
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

## About MoonSharp

MoonSharp is a complete Lua solution written entirely in C#, created and
maintained by **Marco Mastropaolo** — http://www.moonsharp.org

Everything below is the original library's work, unchanged by this fork:

* 99% compatible with Lua 5.2 (weak tables being the only unsupported feature)
* Support for metalua style anonymous functions (lambda-style)
* **Debugger** support via Debug Adapter Protocol, e.g. Visual Studio Code
* Runs on Ahead-of-time platforms like iOS, and on IL2CPP converted code
* No external dependencies
* Easy and performant interop with CLR objects, with runtime code generation
  where supported — methods, extension methods, overloads, fields, properties
  and indexers
* Complete Lua standard library with very few exceptions, plus extensions
* Async method support
* Bytecode dump/load for obfuscation and quicker parsing at runtime
* Embedded JSON parser to convert between JSON and Lua tables
* Easy opt-out of standard library modules to sandbox what scripts can access
* Script errors surface as exceptions
* Coroutines, including invocation of coroutines as C# iterators
* REPL interpreter, plus facilities to implement your own in a few lines

For differences between MoonSharp and standard Lua, see
http://www.moonsharp.org/moonluadifferences.html

Tutorials and further documentation: http://www.moonsharp.org

## License

Released under a 3-clause BSD license.

Copyright (c) 2014-2016, Marco Mastropaolo
All rights reserved.

Parts of the string library are based on the KopiLua project
(https://github.com/NLua/KopiLua) — Copyright (c) 2012 LoDC.

Debugger icons are from the Eclipse project (https://www.eclipse.org/).

Fork modifications copyright (c) 2026 Kelly Birr, released under the same
3-clause BSD license.
