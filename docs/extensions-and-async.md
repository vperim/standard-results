# Extensions and Async Operations Guide

This guide covers advanced async operations, extension methods, and patterns for working with Results in asynchronous contexts.

## Async Result Creation

### TryAsync Pattern
```csharp
public async Task<Result<string, Error>> FetchDataAsync(string url)
{
    return await Result.TryAsync(
        func: async () => await httpClient.GetStringAsync(url),
        mapException: ex => Error.Permanent("http_error", ex.Message)
    );
}
```

### TryAsync with Cancellation Handling
```csharp
public async Task<Result<Data, Error>> ProcessDataAsync(CancellationToken ct)
{
    return await Result.TryAsync(
        func: async () => await LongRunningOperationAsync(ct),
        mapException: ex => Error.Permanent("process_error", ex.Message),
        onOperationCanceled: cancelEx => Error.Transient("cancelled", "Operation was cancelled")
    );
}
```

**Important**: All async operations use `ConfigureAwait(false)` to prevent deadlocks in synchronous contexts.

## Async Transformations

### MapAsync
```csharp
var result = Result<int, Error>.Success(42);

Result<string, Error> mapped = await result.MapAsync(async value =>
{
    await Task.Delay(100);
    return value.ToString();
});
```

### MapAsync with Extension Method
```csharp
using StandardResults;

Result<User, Error> userResult = GetUser();
Result<UserProfile, Error> profileResult = await userResult.MapAsync(async user =>
{
    return await LoadUserProfileAsync(user.Id);
});
```

### MapErrorAsync
```csharp
var result = Result<User, string>.Failure("user_not_found");

Result<User, Error> enrichedError = await result.MapErrorAsync(async errorMsg =>
{
    var details = await GetErrorDetailsAsync(errorMsg);
    return Error.Permanent("user_error", $"{errorMsg}: {details}");
});
```

## Async Chaining with BindAsync

### Basic BindAsync
```csharp
public async Task<Result<UserProfile, Error>> GetUserProfileAsync(int userId)
{
    return await GetUserAsync(userId)
        .BindAsync(async user => await LoadProfileAsync(user))
        .BindAsync(async profile => await EnrichProfileAsync(profile));
}

async Task<Result<User, Error>> GetUserAsync(int id) { /* ... */ }
async Task<Result<UserProfile, Error>> LoadProfileAsync(User user) { /* ... */ }  
async Task<Result<UserProfile, Error>> EnrichProfileAsync(UserProfile profile) { /* ... */ }
```

### Mixed Sync and Async Chaining
```csharp
var result = await GetUserAsync(1)
    .Map(user => user.Email)                    // Sync transformation
    .BindAsync(email => ValidateEmailAsync(email))  // Async validation
    .Map(validEmail => new EmailAddress(validEmail)); // Sync construction
```

## Async Pattern Matching

### MatchAsync with Return Value
```csharp
var result = await GetUserAsync(1);

string response = await result.MatchAsync(
    onSuccess: async user => 
    {
        await LogSuccessAsync($"Found user: {user.Name}");
        return $"Hello, {user.Name}!";
    },
    onFailure: async error =>
    {
        await LogErrorAsync(error);
        return "User not found";
    }
);
```

### MatchAsync for Side Effects
```csharp
await result.MatchAsync(
    onSuccess: async user => await SendWelcomeEmailAsync(user),
    onFailure: async error => await NotifyAdminAsync(error)
);
```

## Side Effect Operations

### TapAsync - Success Side Effects
```csharp
var result = await GetUserAsync(1)
    .TapAsync(async user => await LogUserAccessAsync(user))
    .MapAsync(async user => await LoadUserPreferencesAsync(user));

// TapAsync executes the side effect but returns the original result
```

### TapErrorAsync - Failure Side Effects
```csharp
var result = await ProcessPaymentAsync(paymentRequest)
    .TapErrorAsync(async error => await LogPaymentFailureAsync(error))
    .TapErrorAsync(async error => await NotifyPaymentTeamAsync(error));
```

## Async Validation with EnsureAsync

### Basic Validation
```csharp
var result = await GetUserAsync(1)
    .EnsureAsync(
        predicate: async user => await IsActiveUserAsync(user.Id),
        errorFactory: () => Error.Permanent("user_inactive", "User account is inactive")
    );
```

### Complex Validation Chain
```csharp
var result = await CreateOrderAsync(orderRequest)
    .EnsureAsync(
        predicate: async order => await HasInventoryAsync(order.ProductId, order.Quantity),
        errorFactory: () => Error.Transient("insufficient_inventory", "Not enough inventory")
    )
    .EnsureAsync(
        predicate: async order => await ValidatePaymentMethodAsync(order.PaymentMethodId),
        errorFactory: () => Error.Permanent("invalid_payment", "Payment method is invalid")
    );
```

## Extension Methods Usage

The `ResultExtensions` class provides fluent extension methods:

```csharp
using StandardResults;

// All operations are available as extensions
var result = GetUser()
    .Map(user => user.Email)
    .Bind(email => ValidateEmail(email))
    .MapError(err => Error.Permanent("validation", err));

// Async extensions
var asyncResult = await GetUserAsync()
    .MapAsync(async user => await LoadProfileAsync(user))
    .BindAsync(async profile => await ValidateProfileAsync(profile));
```

## Advanced Async Patterns

### Parallel Operations
```csharp
public async Task<Result<CombinedData, Error>> GetCombinedDataAsync(int userId)
{
    var userTask = GetUserAsync(userId);
    var profileTask = GetProfileAsync(userId);
    var settingsTask = GetSettingsAsync(userId);

    await Task.WhenAll(userTask, profileTask, settingsTask);

    var user = await userTask;
    var profile = await profileTask;
    var settings = await settingsTask;

    // Combine results
    return user.Bind(u => 
        profile.Bind(p => 
            settings.Map(s => new CombinedData(u, p, s))));
}
```

### Result Collection Processing
```csharp
public async Task<Result<List<ProcessedItem>, Error>> ProcessItemsAsync(IEnumerable<Item> items)
{
    var tasks = items.Select(ProcessSingleItemAsync);
    var results = await Task.WhenAll(tasks);

    // Find first failure or collect all successes
    var firstFailure = results.FirstOrDefault(r => r.IsFailure);
    if (firstFailure.IsFailure)
        return Result<List<ProcessedItem>, Error>.Failure(firstFailure.Error);

    var successes = results.Select(r => r.Value).ToList();
    return Result<List<ProcessedItem>, Error>.Success(successes);
}
```

## Exception Handling in Async Context

### Proper Exception Boundaries
```csharp
public async Task<Result<Data, Error>> SafeOperationAsync()
{
    try
    {
        // Use TryAsync for exception-prone operations
        var dataResult = await Result.TryAsync(
            async () => await ExternalApiCallAsync(),
            ex => Error.Transient("api_error", ex.Message)
        );

        // Continue with safe Result operations
        return await dataResult
            .MapAsync(async data => await TransformDataAsync(data))
            .BindAsync(async transformed => await ValidateDataAsync(transformed));
    }
    catch (Exception ex)
    {
        // Catch any remaining exceptions
        return Result<Data, Error>.Failure(
            Error.Permanent("unexpected_error", ex.Message));
    }
}
```

### Cancellation Token Propagation
```csharp
public async Task<Result<Data, Error>> ProcessWithCancellationAsync(CancellationToken ct)
{
    return await Result.TryAsync(
        func: async () =>
        {
            var step1 = await Step1Async(ct);
            ct.ThrowIfCancellationRequested();
            
            var step2 = await Step2Async(step1, ct);
            ct.ThrowIfCancellationRequested();
            
            return step2;
        },
        mapException: ex => Error.Permanent("process_error", ex.Message),
        onOperationCanceled: _ => Error.Transient("cancelled", "Operation was cancelled by user")
    );
}
```
