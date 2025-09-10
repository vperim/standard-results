# StandardResults

[![CI & Publish](https://github.com/OutlanderZOR/StandardResults/actions/workflows/ci.yml/badge.svg)](https://github.com/OutlanderZOR/StandardResults/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Lightweight C# result primitives for functional error handling with support for async operations, validation aggregation, and error collections.  
Targets **.NET Standard 2.0** and **.NET 9.0**.

## Features

- `Result<T, TError>` type with success/failure flow
- Async helpers (`MapAsync`, `BindAsync`, `TryAsync`, etc.)
- Immutable `Error`, `ErrorCollection`, and `ValidationErrors`
- Builders for efficient accumulation (`ErrorCollectionBuilder`, `ValidationErrorsBuilder`)
- Compatibility shims for .NET Standard 2.0 (nullable attributes, `Index`, `Range`)

## Installation

```bash
dotnet add package StandardResults
```

## Usage

### Basic Result

```csharp
using StandardResults;

Result<int, Error> Divide(int a, int b)
{
    if (b == 0)
        return Result<int, Error>.Failure(Error.Permanent("divide_by_zero", "Division by zero"));
    return Result<int, Error>.Success(a / b);
}

var result = Divide(10, 2);

if (result.IsSuccess)
{
    Console.WriteLine($"Value = {result.Value}");
}
else
{
    Console.WriteLine($"Error = {result.Error}");
}
```

### Mapping and Binding

```csharp
var r1 = Result<int, Error>.Success(5);
var r2 = r1.Map(x => x * 2);           // Success(10)
var r3 = r1.Bind(x => Divide(x, 0));   // Failure(Error)
```

### Async Helpers

```csharp
var r = await Result.TryAsync(
    async () => await Task.FromResult(42),
    ex => Error.Transient("exception", ex.Message)
);

var mapped = await r.MapAsync(x => Task.FromResult(x * 2));
```

### Error Collection

```csharp
var builder = new ErrorCollectionBuilder()
    .Add("not_found", "User not found")
    .Add("timeout", "Service unavailable", transient: true);

ErrorCollection errors = builder.Build();

Console.WriteLine(errors.Summary()); // "not_found: User not found; timeout: Service unavailable"
```

### Validation Errors

```csharp
var builder = new ValidationErrorsBuilder()
    .Require(!string.IsNullOrWhiteSpace(username), "username", "Username is required")
    .Require(password.Length >= 6, "password", "Password too short");

ValidationErrors validation = builder.Build();

if (validation.HasErrors)
{
    Console.WriteLine(validation.Summary());
}
```