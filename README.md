# StandardResults

[![CI & Publish](https://github.com/OutlanderZOR/StandardResults/actions/workflows/ci.yml/badge.svg)](https://github.com/OutlanderZOR/StandardResults/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Lightweight C# result primitives for functional error handling with support for async operations, validation aggregation, and error collections.  
Targets **.NET Standard 2.0** and **.NET 9.0**.

## Features

- `Result<T, TError>` type with success/failure flow
- Async helpers (`MapAsync`, `BindAsync`, `TryAsync`, etc.)
- Immutable `Error`, `ErrorCollection`, and `ValidationErrors`
- Fluent API for error accumulation with conditional methods (`When`, `Require`)

## Installation

```bash
dotnet add package StandardResults
```

## Documentation

- **[Result Type Guide](docs/result-type-guide.md)** - Core Result features, creation, state checking, transformations, and pattern matching
- **[Extensions and Async Guide](docs/extensions-and-async.md)** - Advanced async operations, extension methods, and async patterns  
- **[Validation Guide](docs/validation-guide.md)** - Error handling, validation patterns, and error aggregation

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
var errors = ErrorCollection.Empty
    .WithError("not_found", "User not found")
    .WithError("timeout", "Service unavailable", transient: true);

Console.WriteLine(errors.Summary()); // "not_found: User not found; timeout: Service unavailable"
```

### Validation Errors

```csharp
var validation = ValidationErrors.Empty
    .RequireNotEmpty(username, "username")
    .Require(password?.Length >= 6, "password", "Password must be at least 6 characters");

if (validation.HasErrors)
{
    Console.WriteLine(validation.Summary());
}
```