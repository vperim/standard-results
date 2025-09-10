# Result Type Guide

This guide covers the core `Result<T, TError>` type and its fundamental features.

## Basic Result Creation

### Creating Success Results
```csharp
var successResult = Result<int, string>.Success(42);
var userResult = Result<User, Error>.Success(new User { Id = 1, Name = "John" });
```

### Creating Failure Results
```csharp
var failureResult = Result<int, string>.Failure("Operation failed");
var errorResult = Result<User, Error>.Failure(Error.Permanent("not_found", "User not found"));
```

**Important**: Failure results cannot accept null errors - this throws `ArgumentNullException`.

## State Checking

### Basic State Properties
```csharp
var result = Result<int, string>.Success(10);

if (result.IsSuccess)
{
    Console.WriteLine($"Success: {result.Value}");
}

if (result.IsFailure)  
{
    Console.WriteLine($"Error: {result.Error}");
}
```

### Default/Uninitialized Results
```csharp
var defaultResult = default(Result<int, string>);

// This will throw InvalidOperationException
// if (defaultResult.IsSuccess) { ... }

// Check first:
if (defaultResult.IsDefault)
{
    // Handle uninitialized result
}
```

**Important**: Always check `IsDefault` before accessing other properties on potentially uninitialized Results.

## Safe Value Access

### TryGet Pattern
```csharp
var result = Result<int, string>.Success(42);

if (result.TryGetValue(out int value))
{
    Console.WriteLine($"Value: {value}");
}

if (result.TryGetError(out string error))
{
    Console.WriteLine($"Error: {error}");
}
```

### Default Values
```csharp
var result = Result<int, string>.Failure("error");

int value = result.GetValueOrDefault(-1);    // Returns -1
string error = result.GetErrorOrDefault(""); // Returns "error"
```

## Deconstruction

```csharp
var result = Result<int, string>.Success(42);

var (isSuccess, value, error) = result;
// isSuccess = true, value = 42, error = null
```

## Pattern Matching with Match

### Synchronous Match
```csharp
var result = Result<int, string>.Success(42);

// Match with return value
string message = result.Match(
    onSuccess: value => $"Got value: {value}",
    onFailure: error => $"Got error: {error}"
);

// Match with side effects only
result.Match(
    onSuccess: value => Console.WriteLine($"Success: {value}"),
    onFailure: error => Console.WriteLine($"Error: {error}")
);
```

## Transforming Results with Map

### Basic Map
```csharp
var result = Result<int, string>.Success(10);

Result<string, string> mapped = result.Map(x => x.ToString());
// Result<string, string>.Success("10")

Result<int, string> doubled = result.Map(x => x * 2);
// Result<int, string>.Success(20)
```

### Map Error
```csharp
var result = Result<int, string>.Failure("simple error");

Result<int, Error> mappedError = result.MapError(err => 
    Error.Permanent("conversion", err));
```

**Important**: Map only executes on success; failures pass through unchanged.

## Chaining Operations with Bind

### Basic Bind
```csharp
Result<int, string> ParseAndDouble(string input)
{
    if (!int.TryParse(input, out int parsed))
        return Result<int, string>.Failure("Invalid number");
    
    return Result<int, string>.Success(parsed * 2);
}

var result = Result<string, string>.Success("21")
    .Bind(ParseAndDouble);
// Result<int, string>.Success(42)
```

### Chaining Multiple Operations
```csharp
Result<User, Error> GetUser(int id) => /* ... */;
Result<Profile, Error> GetProfile(User user) => /* ... */;
Result<Settings, Error> GetSettings(Profile profile) => /* ... */;

var result = GetUser(1)
    .Bind(GetProfile)
    .Bind(GetSettings);
```

**Important**: Bind short-circuits on first failure; remaining operations don't execute.

## Exception-Safe Operations

### Try Pattern
```csharp
var result = Result.Try(
    func: () => int.Parse("42"),
    mapException: ex => Error.Permanent("parse_error", ex.Message)
);
```

### Try with Custom Cancellation Handling
```csharp
var result = Result.Try(
    func: () => DoSomethingCancellable(),
    mapException: ex => Error.Permanent("operation_error", ex.Message),
    onOperationCanceled: cancelEx => Error.Transient("cancelled", "Operation was cancelled")
);
```

**Important**: If `onOperationCanceled` is null, `OperationCanceledException` will be re-thrown instead of converted to Result.

## Equality and Comparison

```csharp
var r1 = Result<int, string>.Success(42);
var r2 = Result<int, string>.Success(42);
var r3 = Result<int, string>.Failure("error");

Console.WriteLine(r1 == r2); // True
Console.WriteLine(r1 == r3); // False

// Default results are equal to each other
var d1 = default(Result<int, string>);
var d2 = default(Result<int, string>);
Console.WriteLine(d1 == d2); // True
```

## String Representation

```csharp
var success = Result<int, string>.Success(42);
Console.WriteLine(success); // "Success(42)"

var failure = Result<int, string>.Failure("error");
Console.WriteLine(failure); // "Failure(error)"

var defaultResult = default(Result<int, string>);
Console.WriteLine(defaultResult); // "Result<default>"
```
