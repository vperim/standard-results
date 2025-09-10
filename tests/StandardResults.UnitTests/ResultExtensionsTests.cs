namespace StandardResults.UnitTests;

public class ResultExtensionsTests
{
    [Fact]
    public void ExtensionMap_Success_TransformsValue()
    {
        var result = Result<int, string>.Success(5);
        
        var mapped = result.Map(x => x * 2);
        
        Assert.True(mapped.IsSuccess);
        Assert.Equal(10, mapped.Value);
    }

    [Fact]
    public void ExtensionMap_Failure_PreservesError()
    {
        var result = Result<int, string>.Failure("error");
        
        var mapped = result.Map(x => x * 2);
        
        Assert.True(mapped.IsFailure);
        Assert.Equal("error", mapped.Error);
    }

    [Fact]
    public void ExtensionBind_Success_ExecutesBind()
    {
        var result = Result<int, string>.Success(5);
        
        var bound = result.Bind(x => x > 0 
            ? Result<string, string>.Success($"positive: {x}")
            : Result<string, string>.Failure("negative"));
        
        Assert.True(bound.IsSuccess);
        Assert.Equal("positive: 5", bound.Value);
    }

    [Fact]
    public void ExtensionBind_Failure_ShortCircuits()
    {
        var result = Result<int, string>.Failure("original error");
        
        var bound = result.Bind(_ => Result<string, string>.Success("should not execute"));
        
        Assert.True(bound.IsFailure);
        Assert.Equal("original error", bound.Error);
    }

    [Fact]
    public void ExtensionBind_Success_CanReturnFailure()
    {
        var result = Result<int, string>.Success(-3);
        
        var bound = result.Bind(x => x > 0 
            ? Result<string, string>.Success($"positive: {x}")
            : Result<string, string>.Failure("negative value"));
        
        Assert.True(bound.IsFailure);
        Assert.Equal("negative value", bound.Error);
    }

    [Fact]
    public void ExtensionMapError_Success_PreservesValue()
    {
        var result = Result<int, string>.Success(42);
        
        var mapped = result.MapError(error => new InvalidOperationException(error));
        
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void ExtensionMapError_Failure_TransformsError()
    {
        var result = Result<int, string>.Failure("timeout");
        
        var mapped = result.MapError(error => new TimeoutException($"Mapped: {error}"));
        
        Assert.True(mapped.IsFailure);
        Assert.IsType<TimeoutException>(mapped.Error);
        Assert.Equal("Mapped: timeout", mapped.Error.Message);
    }

    [Fact]
    public async Task ExtensionMapAsync_Success_TransformsValueAsync()
    {
        var result = Result<int, string>.Success(7);
        
        var mapped = await result.MapAsync(async x => 
        {
            await Task.Yield();
            return x * 3;
        });
        
        Assert.True(mapped.IsSuccess);
        Assert.Equal(21, mapped.Value);
    }

    [Fact]
    public async Task ExtensionMapAsync_Failure_PreservesError()
    {
        var result = Result<int, string>.Failure("async error");
        
        var mapped = await result.MapAsync(async x => 
        {
            await Task.Yield();
            return x * 3;
        });
        
        Assert.True(mapped.IsFailure);
        Assert.Equal("async error", mapped.Error);
    }

    [Fact]
    public async Task ExtensionBindAsync_Success_ExecutesBindAsync()
    {
        var result = Result<int, string>.Success(4);
        
        var bound = await result.BindAsync(async x => 
        {
            await Task.Yield();
            return x % 2 == 0 
                ? Result<string, string>.Success($"even: {x}")
                : Result<string, string>.Failure("odd number");
        });
        
        Assert.True(bound.IsSuccess);
        Assert.Equal("even: 4", bound.Value);
    }

    [Fact]
    public async Task ExtensionBindAsync_Success_CanReturnFailure()
    {
        var result = Result<int, string>.Success(3);
        
        var bound = await result.BindAsync(async x => 
        {
            await Task.Yield();
            return x % 2 == 0 
                ? Result<string, string>.Success($"even: {x}")
                : Result<string, string>.Failure("odd number");
        });
        
        Assert.True(bound.IsFailure);
        Assert.Equal("odd number", bound.Error);
    }

    [Fact]
    public async Task ExtensionBindAsync_Failure_ShortCircuits()
    {
        var result = Result<int, string>.Failure("bind error");
        
        var bound = await result.BindAsync(async _ => 
        {
            await Task.Yield();
            return Result<string, string>.Success("should not execute");
        });
        
        Assert.True(bound.IsFailure);
        Assert.Equal("bind error", bound.Error);
    }

    [Fact]
    public void ChainedOperations_Success_ExecutesInSequence()
    {
        var result = Result<int, string>.Success(2)
            .Map(x => x * 3)                    // 6
            .Bind(x => Result<int, string>.Success(x + 4))  // 10
            .Map(x => x / 2);                   // 5

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void ChainedOperations_FailureInMiddle_ShortCircuits()
    {
        var result = Result<int, string>.Success(2)
            .Map(x => x * 3)                    // 6
            .Bind(_ => Result<int, string>.Failure("chain break"))
            .Map(x => x / 2);                   // should not execute

        Assert.True(result.IsFailure);
        Assert.Equal("chain break", result.Error);
    }

    [Fact]
    public void ChainedOperations_InitialFailure_ShortCircuitsAll()
    {
        var result = Result<int, string>.Failure("initial error")
            .Map(x => x * 3)
            .Bind(x => Result<int, string>.Success(x + 4))
            .Map(x => x / 2);

        Assert.True(result.IsFailure);
        Assert.Equal("initial error", result.Error);
    }

    [Fact]
    public void ChainedOperations_ErrorTypeTransformation()
    {
        var result = Result<int, string>.Success(10)
            .MapError(err => new InvalidOperationException(err))    // Change error type
            .Map(x => x / 2)                                        // 5
            .MapError(ex => new ArgumentException(ex.Message));     // Change error type again

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void ChainedOperations_WithErrorTypeTransformation_AndFailure()
    {
        var result = Result<int, string>.Failure("string error")
            .MapError(err => new InvalidOperationException(err))
            .Map(x => x / 2)
            .MapError(ex => new ArgumentException(ex.Message));

        Assert.True(result.IsFailure);
        Assert.IsType<ArgumentException>(result.Error);
        Assert.Equal("string error", result.Error.Message);
    }

    [Fact]
    public async Task ChainedAsyncOperations_Success_ExecutesInSequence()
    {
        var mappedResult = await Result<int, string>.Success(3)
            .MapAsync(async x => { await Task.Yield(); return x * 2; });     // 6
            
        var result = await mappedResult.BindAsync(async x => 
        { 
            await Task.Yield(); 
            return Result<string, string>.Success($"value: {x}"); 
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("value: 6", result.Value);
    }

    [Fact]
    public async Task ChainedAsyncOperations_FailureInMiddle_ShortCircuits()
    {
        var mappedResult = await Result<int, string>.Success(3)
            .MapAsync(async x => { await Task.Yield(); return x * 2; });
            
        var result = await mappedResult.BindAsync(async _ => 
        { 
            await Task.Yield(); 
            return Result<string, string>.Failure("async failure"); 
        });

        Assert.True(result.IsFailure);
        Assert.Equal("async failure", result.Error);
    }

    [Fact]
    public void ComplexChaining_RealWorldScenario()
    {
        const string userInput = "123";
        
        var result = Result.Try<string, string>(() => userInput, ex => ex.Message)
            .Bind(input => string.IsNullOrWhiteSpace(input) 
                ? Result<string, string>.Failure("Input cannot be empty")
                : Result<string, string>.Success(input.Trim()))
            .Bind(input => int.TryParse(input, out var number)
                ? Result<int, string>.Success(number)
                : Result<int, string>.Failure("Invalid number format"))
            .Map(number => number * 2)
            .Bind(number => number > 100
                ? Result<int, string>.Success(number)
                : Result<int, string>.Failure("Number too small"))
            .MapError(error => $"Validation failed: {error}");

        Assert.True(result.IsSuccess);
        Assert.Equal(246, result.Value);
    }

    [Fact]
    public void ComplexChaining_WithValidationFailure()
    {
        const string userInput = "abc";
        
        var result = Result.Try<string, string>(() => userInput, ex => ex.Message)
            .Bind(input => string.IsNullOrWhiteSpace(input) 
                ? Result<string, string>.Failure("Input cannot be empty")
                : Result<string, string>.Success(input.Trim()))
            .Bind(input => int.TryParse(input, out var number)
                ? Result<int, string>.Success(number)
                : Result<int, string>.Failure("Invalid number format"))
            .Map(number => number * 2)
            .Bind(number => number > 100
                ? Result<int, string>.Success(number)
                : Result<int, string>.Failure("Number too small"))
            .MapError(error => $"Validation failed: {error}");

        Assert.True(result.IsFailure);
        Assert.Equal("Validation failed: Invalid number format", result.Error);
    }
}