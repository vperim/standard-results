namespace StandardResults.UnitTests;

public class ResultStaticHelpersTests
{
    [Fact]
    public void Try_SuccessfulFunction_ReturnsSuccess()
    {
        var result = Result.Try<string, Error>(
            () => "hello world",
            ex => Error.Permanent("exception", ex.Message)
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("hello world", result.Value);
    }

    [Fact]
    public void Try_ThrowingFunction_ReturnsMappedFailure()
    {
        var result = Result.Try<int, string>(
            () => throw new InvalidOperationException("test error"),
            ex => $"Caught: {ex.Message}"
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Caught: test error", result.Error);
    }

    [Fact]
    public void Try_OperationCanceledException_WithoutHandler_Rethrows()
    {
        Assert.Throws<OperationCanceledException>(() =>
            Result.Try<int, string>(
                () => throw new OperationCanceledException("canceled"),
                ex => ex.Message
            )
        );
    }

    [Fact]
    public void Try_OperationCanceledException_WithHandler_ReturnsMappedResult()
    {
        var result = Result.Try<int, Error>(
            () => throw new OperationCanceledException("operation canceled"),
            ex => Error.Permanent("general", ex.Message),
            oce => Error.Transient("canceled", $"Operation was canceled: {oce.Message}")
        );

        Assert.True(result.IsFailure);
        Assert.Equal("canceled", result.Error.Code);
        Assert.Equal("Operation was canceled: operation canceled", result.Error.Message);
        Assert.True(result.Error.IsTransient);
    }

    [Fact]
    public void Try_TaskCanceledException_WithHandler_TreatedAsOperationCanceled()
    {
        var result = Result.Try<int, string>(
            () => throw new TaskCanceledException("task canceled"),
            ex => $"General: {ex.Message}",
            oce => $"Canceled: {oce.Message}"
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Canceled: task canceled", result.Error);
    }

    [Fact]
    public void Try_AggregateException_UnwrapsAndMapsInnerException()
    {
        var innerException = new ArgumentException("inner error");
        var aggregateException = new AggregateException("aggregate", innerException);

        var result = Result.Try<int, string>(
            () => throw aggregateException,
            ex => ex is AggregateException agg && agg.InnerExceptions.Count == 1
                ? $"Unwrapped: {agg.InnerExceptions[0].Message}"
                : $"General: {ex.Message}"
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Unwrapped: inner error", result.Error);
    }

    [Fact]
    public async Task TryAsync_SuccessfulAsyncFunction_ReturnsSuccess()
    {
        var result = await Result.TryAsync<int, Error>(
            async () => { await Task.Delay(1); return 100; },
            ex => Error.Permanent("exception", ex.Message)
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public async Task TryAsync_ThrowingAsyncFunction_ReturnsMappedFailure()
    {
        var result = await Result.TryAsync<string, string>(
            async () => 
            { 
                await Task.Delay(1); 
                throw new TimeoutException("async timeout"); 
            },
            ex => $"Async error: {ex.Message}"
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Async error: async timeout", result.Error);
    }

    [Fact]
    public async Task TryAsync_OperationCanceledException_WithoutHandler_Rethrows()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Result.TryAsync<int, string>(
                async () => 
                { 
                    await Task.Delay(1); 
                    throw new OperationCanceledException("async canceled"); 
                },
                ex => ex.Message
            )
        );
    }

    [Fact]
    public async Task TryAsync_OperationCanceledException_WithHandler_ReturnsMappedResult()
    {
        var result = await Result.TryAsync<int, Error>(
            async () => 
            { 
                await Task.Delay(1); 
                throw new OperationCanceledException("async operation canceled"); 
            },
            ex => Error.Permanent("general", ex.Message),
            oce => Error.Transient("async_canceled", $"Async operation canceled: {oce.Message}")
        );

        Assert.True(result.IsFailure);
        Assert.Equal("async_canceled", result.Error.Code);
        Assert.Equal("Async operation canceled: async operation canceled", result.Error.Message);
        Assert.True(result.Error.IsTransient);
    }

    [Fact]
    public async Task TryAsync_TaskCanceledException_WithHandler_TreatedAsOperationCanceled()
    {
        var result = await Result.TryAsync<bool, string>(
            async () => 
            { 
                await Task.Delay(1); 
                throw new TaskCanceledException("async task canceled"); 
            },
            ex => $"General: {ex.Message}",
            oce => $"Task canceled: {oce.Message}"
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Task canceled: async task canceled", result.Error);
    }

    [Fact]
    public async Task TryAsync_WithCancellationToken_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await Result.TryAsync<int, string>(
            async () => 
            { 
                await Task.Delay(1000, cts.Token); 
                return 42; 
            },
            ex => $"Error: {ex.Message}",
            oce => $"Canceled: {oce.Message}"
        );

        Assert.True(result.IsFailure);
        Assert.Contains("Canceled:", result.Error);
    }

    [Fact]
    public async Task TryAsync_NestedExceptions_HandledCorrectly()
    {
        var result = await Result.TryAsync<int, string>(
            async () => 
            { 
                await Task.Delay(1); 
                try 
                {
                    throw new ArgumentException("inner");
                }
                catch (Exception inner)
                {
                    throw new InvalidOperationException("outer", inner);
                }
            },
            ex => ex.InnerException != null 
                ? $"Nested: {ex.Message} -> {ex.InnerException.Message}"
                : $"Single: {ex.Message}"
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Nested: outer -> inner", result.Error);
    }

    [Fact]
    public void Try_MultipleExceptionTypes_MappedCorrectly()
    {
        var argumentResult = Result.Try<int, string>(
            () => throw new ArgumentException("arg error"),
            ex => ex switch
            {
                ArgumentException => "argument_error",
                InvalidOperationException => "invalid_operation",
                _ => "unknown_error"
            }
        );

        var invalidOpResult = Result.Try<int, string>(
            () => throw new InvalidOperationException("invalid op"),
            ex => ex switch
            {
                ArgumentException => "argument_error",
                InvalidOperationException => "invalid_operation", 
                _ => "unknown_error"
            }
        );

        Assert.Equal("argument_error", argumentResult.Error);
        Assert.Equal("invalid_operation", invalidOpResult.Error);
    }

    [Fact]
    public async Task TryAsync_ConcurrentOperations_HandledIndependently()
    {
        var tasks = new[]
        {
            Result.TryAsync<int, string>(async () => { await Task.Delay(10); return 1; }, _ => "error1"),
            Result.TryAsync<int, string>(async () => { await Task.Delay(20); throw new Exception("fail"); }, _ => "error2"),
            Result.TryAsync<int, string>(async () => { await Task.Delay(5); return 3; }, _ => "error3")
        };

        var results = await Task.WhenAll(tasks);

        Assert.True(results[0].IsSuccess);
        Assert.Equal(1, results[0].Value);
        
        Assert.True(results[1].IsFailure);
        Assert.Equal("error2", results[1].Error);
        
        Assert.True(results[2].IsSuccess);
        Assert.Equal(3, results[2].Value);
    }
}