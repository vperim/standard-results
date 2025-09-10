# Validation and Error Handling Guide

This guide covers comprehensive error handling, validation patterns, and error aggregation using the StandardResults library.

## Error Types

### IError Interface
```csharp
public interface IError
{
    string Code { get; }        // e.g., "validation", "not_found", "sql_timeout"
    string Message { get; }     // Human-readable error message
    bool IsTransient { get; }   // Can operation be retried?
}
```

### Basic Error Implementation
```csharp
// Create permanent errors (default)
var permanentError = Error.Permanent("not_found", "User not found");

// Create transient errors (can be retried)
var transientError = Error.Transient("timeout", "Database timeout occurred");

// Check error properties
Console.WriteLine($"{error.Code}: {error.Message}");
Console.WriteLine($"Can retry: {error.IsTransient}");
```

### Error String Representation
```csharp
var error1 = Error.Permanent("not_found", "User not found");
Console.WriteLine(error1); // "not_found: User not found"

var error2 = Error.Permanent("", "Simple message");
Console.WriteLine(error2); // "Simple message"
```

## General Error Collections

### ErrorCollection Basics
```csharp
// Start with empty collection
var errors = ErrorCollection.Empty;

// Add individual errors
errors = errors.WithError("timeout", "Connection timeout", transient: true)
              .WithError("auth", "Authentication failed");

// Add constructed Error objects
var customError = Error.Permanent("custom", "Custom error");
errors = errors.WithError(customError);

// Check collection state
Console.WriteLine($"Has errors: {errors.HasErrors}");
Console.WriteLine($"Count: {errors.Count}");
Console.WriteLine($"Is transient: {errors.IsTransient}"); // true if any error is transient
```

### ErrorCollection Summary
```csharp
var errors = ErrorCollection.Empty
    .WithError("auth", "Invalid credentials")
    .WithError("timeout", "Request timeout", transient: true);

// Default separator (semicolon + space)
Console.WriteLine(errors.Summary()); 
// "auth: Invalid credentials; timeout: Request timeout"

// Custom separator
Console.WriteLine(errors.Summary(" | "));
// "auth: Invalid credentials | timeout: Request timeout"
```

### Merging Error Collections
```csharp
var errors1 = ErrorCollection.Empty
    .WithError("auth", "Authentication failed");

var errors2 = ErrorCollection.Empty
    .WithError("timeout", "Connection timeout", transient: true);

var combined = errors1.Merge(errors2);
// Contains both errors, IsTransient = true
```

## Validation Errors (Field-Scoped)

### Basic ValidationErrors
```csharp
// Start with empty validation
var validation = ValidationErrors.Empty;

// Add field-specific errors
validation = validation.WithField("email", "Email is required")
                      .WithField("password", "Password too short")
                      .WithField("age", "Must be 18 or older");

// Check validation state
Console.WriteLine($"Is valid: {!validation.HasErrors}");
Console.WriteLine($"Error count: {validation.Count}");
```

### ValidationErrors Summary
```csharp
var validation = ValidationErrors.Empty
    .WithField("email", "Email is required")
    .WithField("password", "Password must be at least 6 characters");

Console.WriteLine(validation.Summary());
// "email: Email is required; password: Password must be at least 6 characters"

Console.WriteLine(validation); // "Invalid (2 errors)"
```

## Builder Patterns

### ErrorCollectionBuilder
```csharp
var builder = new ErrorCollectionBuilder();

// Add errors fluently
builder.Add("network", "Connection failed", transient: true)
       .Add("auth", "Invalid token")
       .Add(Error.Permanent("custom", "Custom error"));

// Merge with existing collections
var existingErrors = ErrorCollection.Empty.WithError("prior", "Prior error");
builder.Merge(existingErrors);

// Build immutable collection
ErrorCollection errors = builder.Build();

// Reuse builder
builder.Clear();
builder.Add("new", "New error batch");
```

### ValidationErrorsBuilder
```csharp
public ValidationErrors ValidateUserRegistration(UserRegistration request)
{
    var builder = new ValidationErrorsBuilder();

    // Simple conditional validation
    builder.Require(!string.IsNullOrWhiteSpace(request.Email), 
                    "email", "Email is required")
           .Require(request.Email.Contains("@"), 
                    "email", "Email must be valid")
           .Require(request.Password.Length >= 6, 
                    "password", "Password must be at least 6 characters")
           .Require(request.Age >= 18, 
                    "age", "Must be 18 or older");

    return builder.Build();
}
```

### Advanced Validation Patterns

#### Conditional Validation with When
```csharp
var builder = new ValidationErrorsBuilder();

// When condition is true, add error
builder.When(string.IsNullOrEmpty(email), "email", "Email is required")
       .When(password.Length < 6, "password", "Password too short")
       .When(() => ExpensiveValidationCheck(), "data", "Complex validation failed");

// Require is inverse of When (!condition)
builder.Require(email.Contains("@"), "email", "Email must contain @")
       .Require(() => IsValidDomain(email), "email", "Invalid email domain");
```

#### Multi-Step Validation
```csharp
public ValidationErrors ValidateOrder(OrderRequest request)
{
    var builder = new ValidationErrorsBuilder();

    // Basic field validation
    builder.Require(!string.IsNullOrWhiteSpace(request.ProductId), 
                    "productId", "Product ID is required")
           .Require(request.Quantity > 0, 
                    "quantity", "Quantity must be positive");

    // Conditional validation based on other fields
    if (!string.IsNullOrWhiteSpace(request.ProductId))
    {
        builder.When(!ProductExists(request.ProductId), 
                     "productId", "Product does not exist")
               .When(() => !IsProductAvailable(request.ProductId), 
                     "productId", "Product is not available");
    }

    if (request.Quantity > 0)
    {
        builder.When(() => GetInventory(request.ProductId) < request.Quantity,
                     "quantity", "Insufficient inventory");
    }

    return builder.Build();
}
```

## Integration with Result Type

### Validation as Result
```csharp
public Result<User, ValidationErrors> CreateUser(CreateUserRequest request)
{
    var validation = ValidateUserRequest(request);
    
    if (validation.HasErrors)
        return Result<User, ValidationErrors>.Failure(validation);
        
    var user = new User 
    { 
        Email = request.Email, 
        Password = HashPassword(request.Password) 
    };
    
    return Result<User, ValidationErrors>.Success(user);
}
```

### Combined Validation and Business Logic
```csharp
public async Task<Result<Order, IError>> ProcessOrderAsync(OrderRequest request)
{
    // Step 1: Input validation
    var validation = ValidateOrderRequest(request);
    if (validation.HasErrors)
        return Result<Order, IError>.Failure(validation);

    // Step 2: Business rule validation  
    var businessValidation = await ValidateBusinessRulesAsync(request);
    if (businessValidation.HasErrors)
        return Result<Order, IError>.Failure(businessValidation);

    // Step 3: Create order
    return await CreateOrderAsync(request);
}
```

### Error Collection as Result Error
```csharp
public Result<ProcessedData, ErrorCollection> ProcessMultipleItems(List<Item> items)
{
    var errorBuilder = new ErrorCollectionBuilder();
    var processedItems = new List<ProcessedData>();

    foreach (var item in items)
    {
        var result = ProcessSingleItem(item);
        if (result.IsSuccess)
        {
            processedItems.Add(result.Value);
        }
        else
        {
            errorBuilder.Add("processing", $"Failed to process item {item.Id}: {result.Error}");
        }
    }

    var errors = errorBuilder.Build();
    if (errors.HasErrors)
        return Result<ProcessedData, ErrorCollection>.Failure(errors);

    return Result<ProcessedData, ErrorCollection>.Success(new ProcessedData(processedItems));
}
```

## Real-World Patterns

### API Validation Pattern
```csharp
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    var result = await userService.CreateUserAsync(request);
    
    return result.Match(
        onSuccess: user => Ok(new { UserId = user.Id, Message = "User created successfully" }),
        onFailure: error => error switch
        {
            ValidationErrors validation => BadRequest(new 
            { 
                Errors = validation.Errors.Select(e => new { Field = e.Code, Message = e.Message })
            }),
            Error businessError => businessError.Code switch
            {
                "duplicate_email" => Conflict(new { Message = businessError.Message }),
                "external_service" when businessError.IsTransient => 
                    StatusCode(503, new { Message = "Service temporarily unavailable" }),
                _ => StatusCode(500, new { Message = "Internal server error" })
            },
            _ => StatusCode(500, new { Message = "Unknown error occurred" })
        }
    );
}
```

### Transient Error Handling
```csharp
public async Task<Result<Data, IError>> GetDataWithRetryAsync(string endpoint)
{
    const int maxRetries = 3;
    const int delayMs = 1000;

    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        var result = await Result.TryAsync(
            async () => await httpClient.GetAsync<Data>(endpoint),
            ex => ex switch
            {
                TimeoutException => Error.Transient("timeout", "Request timeout"),
                HttpRequestException httpEx when httpEx.Message.Contains("503") => 
                    Error.Transient("service_unavailable", "Service temporarily unavailable"),
                _ => Error.Permanent("http_error", ex.Message)
            }
        );

        if (result.IsSuccess || !result.Error.IsTransient)
            return result;

        if (attempt < maxRetries - 1)
            await Task.Delay(delayMs * (attempt + 1));
    }

    return Result<Data, IError>.Failure(
        Error.Permanent("max_retries", "Max retry attempts exceeded"));
}
```

### Aggregate Validation Pattern
```csharp
public async Task<Result<CompletedOrder, ValidationErrors>> ValidateCompleteOrderAsync(Order order)
{
    var tasks = new[]
    {
        ValidateInventoryAsync(order),
        ValidatePaymentAsync(order),
        ValidateShippingAsync(order),
        ValidateCustomerAsync(order)
    };

    var results = await Task.WhenAll(tasks);
    
    var builder = new ValidationErrorsBuilder();
    foreach (var validation in results)
    {
        builder.Merge(validation);
    }

    var finalValidation = builder.Build();
    if (finalValidation.HasErrors)
        return Result<CompletedOrder, ValidationErrors>.Failure(finalValidation);

    return Result<CompletedOrder, ValidationErrors>.Success(
        new CompletedOrder(order));
}
```
