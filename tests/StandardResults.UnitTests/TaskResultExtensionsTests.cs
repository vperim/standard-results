namespace StandardResults.UnitTests;

public class TaskResultExtensionsTests
{
    #region Map/MapAsync

    [Theory]
    [InlineData(true, 5, 10)]
    [InlineData(false, 0, 0)]
    public async Task Map_TransformsValueOnSuccessOnly(bool isSuccess, int input, int expected)
    {
        var resultTask = Task.FromResult(isSuccess
            ? Result<int, string>.Success(input)
            : Result<int, string>.Failure("error"));

        var mapped = await resultTask.Map(x => x * 2);

        Assert.Equal(isSuccess, mapped.IsSuccess);
        if (isSuccess)
            Assert.Equal(expected, mapped.Value);
        else
            Assert.Equal("error", mapped.Error);
    }

    [Theory]
    [InlineData(true, 7, 21)]
    [InlineData(false, 0, 0)]
    public async Task MapAsync_TransformsValueOnSuccessOnly(bool isSuccess, int input, int expected)
    {
        var resultTask = Task.FromResult(isSuccess
            ? Result<int, string>.Success(input)
            : Result<int, string>.Failure("error"));

        var mapped = await resultTask.MapAsync(async x =>
        {
            await Task.Yield();
            return x * 3;
        });

        Assert.Equal(isSuccess, mapped.IsSuccess);
        if (isSuccess)
            Assert.Equal(expected, mapped.Value);
        else
            Assert.Equal("error", mapped.Error);
    }

    #endregion

    #region Bind/BindAsync

    [Fact]
    public async Task Bind_Success_ExecutesBind()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(5));

        var bound = await resultTask.Bind(x => x > 0
            ? Result<string, string>.Success($"positive: {x}")
            : Result<string, string>.Failure("negative"));

        Assert.True(bound.IsSuccess);
        Assert.Equal("positive: 5", bound.Value);
    }

    [Fact]
    public async Task Bind_Success_CanReturnFailure()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(-3));

        var bound = await resultTask.Bind(x => x > 0
            ? Result<string, string>.Success($"positive: {x}")
            : Result<string, string>.Failure("negative value"));

        Assert.True(bound.IsFailure);
        Assert.Equal("negative value", bound.Error);
    }

    [Fact]
    public async Task Bind_Failure_ShortCircuits()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("original error"));
        var binderInvoked = false;

        var bound = await resultTask.Bind(x =>
        {
            binderInvoked = true;
            return Result<string, string>.Success($"value: {x}");
        });

        Assert.True(bound.IsFailure);
        Assert.Equal("original error", bound.Error);
        Assert.False(binderInvoked);
    }

    [Fact]
    public async Task BindAsync_Success_ExecutesBindAsync()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(4));

        var bound = await resultTask.BindAsync(async x =>
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
    public async Task BindAsync_Failure_ShortCircuits()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("bind error"));
        var binderInvoked = false;

        var bound = await resultTask.BindAsync(async x =>
        {
            binderInvoked = true;
            await Task.Yield();
            return Result<string, string>.Success($"value: {x}");
        });

        Assert.True(bound.IsFailure);
        Assert.Equal("bind error", bound.Error);
        Assert.False(binderInvoked);
    }

    #endregion

    #region MapError/MapErrorAsync

    [Fact]
    public async Task MapError_Failure_TransformsError()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("timeout"));

        var mapped = await resultTask.MapError(error => new TimeoutException($"Mapped: {error}"));

        Assert.True(mapped.IsFailure);
        Assert.IsType<TimeoutException>(mapped.Error);
        Assert.Equal("Mapped: timeout", mapped.Error.Message);
    }

    [Fact]
    public async Task MapError_Success_PreservesValueAndDoesNotInvokeMapper()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var mapperInvoked = false;

        var mapped = await resultTask.MapError(error =>
        {
            mapperInvoked = true;
            return new InvalidOperationException(error);
        });

        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
        Assert.False(mapperInvoked);
    }

    [Fact]
    public async Task MapErrorAsync_Failure_TransformsError()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("timeout"));

        var mapped = await resultTask.MapErrorAsync(async error =>
        {
            await Task.Yield();
            return new TimeoutException($"Mapped: {error}");
        });

        Assert.True(mapped.IsFailure);
        Assert.IsType<TimeoutException>(mapped.Error);
        Assert.Equal("Mapped: timeout", mapped.Error.Message);
    }

    [Fact]
    public async Task MapErrorAsync_Success_PreservesValueAndDoesNotInvokeMapper()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var mapperInvoked = false;

        var mapped = await resultTask.MapErrorAsync(async error =>
        {
            mapperInvoked = true;
            await Task.Yield();
            return new InvalidOperationException(error);
        });

        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
        Assert.False(mapperInvoked);
    }

    #endregion

    #region Tap/TapAsync

    [Fact]
    public async Task Tap_Success_ExecutesActionAndPreservesResult()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var capturedValue = 0;

        var result = await resultTask.Tap(x => capturedValue = x);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(42, capturedValue);
    }

    [Fact]
    public async Task Tap_Failure_DoesNotExecuteAction()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("error"));
        var actionExecuted = false;

        var result = await resultTask.Tap(_ => actionExecuted = true);

        Assert.True(result.IsFailure);
        Assert.Equal("error", result.Error);
        Assert.False(actionExecuted);
    }

    [Fact]
    public async Task TapAsync_Success_ExecutesAsyncActionAndPreservesResult()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var capturedValue = 0;

        var result = await resultTask.TapAsync(async x =>
        {
            await Task.Yield();
            capturedValue = x;
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(42, capturedValue);
    }

    [Fact]
    public async Task TapAsync_Failure_DoesNotExecuteAction()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("error"));
        var actionExecuted = false;

        var result = await resultTask.TapAsync(async _ =>
        {
            actionExecuted = true;
            await Task.Yield();
        });

        Assert.True(result.IsFailure);
        Assert.Equal("error", result.Error);
        Assert.False(actionExecuted);
    }

    #endregion

    #region TapError/TapErrorAsync

    [Fact]
    public async Task TapError_Failure_ExecutesActionAndPreservesResult()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("error"));
        var capturedError = string.Empty;

        var result = await resultTask.TapError(e => capturedError = e);

        Assert.True(result.IsFailure);
        Assert.Equal("error", result.Error);
        Assert.Equal("error", capturedError);
    }

    [Fact]
    public async Task TapError_Success_DoesNotExecuteAction()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var actionExecuted = false;

        var result = await resultTask.TapError(_ => actionExecuted = true);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(actionExecuted);
    }

    [Fact]
    public async Task TapErrorAsync_Failure_ExecutesAsyncActionAndPreservesResult()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("error"));
        var capturedError = string.Empty;

        var result = await resultTask.TapErrorAsync(async e =>
        {
            await Task.Yield();
            capturedError = e;
        });

        Assert.True(result.IsFailure);
        Assert.Equal("error", result.Error);
        Assert.Equal("error", capturedError);
    }

    [Fact]
    public async Task TapErrorAsync_Success_DoesNotExecuteAction()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var actionExecuted = false;

        var result = await resultTask.TapErrorAsync(async _ =>
        {
            actionExecuted = true;
            await Task.Yield();
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(actionExecuted);
    }

    #endregion

    #region Ensure

    [Theory]
    [InlineData(42, true, 42, null)]
    [InlineData(-5, false, 0, "Value -5 must be positive")]
    public async Task Ensure_WithValueFactory_BehavesCorrectly(
        int input, bool expectSuccess, int expectedValue, string? expectedError)
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(input));

        var result = await resultTask.Ensure(x => x > 0, x => $"Value {x} must be positive");

        Assert.Equal(expectSuccess, result.IsSuccess);
        if (expectSuccess)
            Assert.Equal(expectedValue, result.Value);
        else
            Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public async Task Ensure_WithValueFactory_Failure_PreservesOriginalError()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("original error"));
        var predicateExecuted = false;

        var result = await resultTask.Ensure(
            x => { predicateExecuted = true; return x > 0; },
            x => $"Value {x} must be positive");

        Assert.True(result.IsFailure);
        Assert.Equal("original error", result.Error);
        Assert.False(predicateExecuted);
    }

    [Theory]
    [InlineData(42, true, 42, null)]
    [InlineData(-5, false, 0, "Must be positive")]
    public async Task Ensure_WithSimpleFactory_BehavesCorrectly(
        int input, bool expectSuccess, int expectedValue, string? expectedError)
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(input));

        var result = await resultTask.Ensure(x => x > 0, () => "Must be positive");

        Assert.Equal(expectSuccess, result.IsSuccess);
        if (expectSuccess)
            Assert.Equal(expectedValue, result.Value);
        else
            Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public async Task Ensure_WithSimpleFactory_Failure_PreservesOriginalError()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("original error"));
        var predicateExecuted = false;

        var result = await resultTask.Ensure(
            x => { predicateExecuted = true; return x > 0; },
            () => "Must be positive");

        Assert.True(result.IsFailure);
        Assert.Equal("original error", result.Error);
        Assert.False(predicateExecuted);
    }

    #endregion

    #region EnsureAsync

    [Theory]
    [InlineData(42, true, 42, null)]
    [InlineData(-5, false, 0, "Must be positive")]
    public async Task EnsureAsync_BehavesCorrectly(
        int input, bool expectSuccess, int expectedValue, string? expectedError)
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(input));

        var result = await resultTask.EnsureAsync(
            async x => { await Task.Yield(); return x > 0; },
            () => "Must be positive");

        Assert.Equal(expectSuccess, result.IsSuccess);
        if (expectSuccess)
            Assert.Equal(expectedValue, result.Value);
        else
            Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public async Task EnsureAsync_Failure_DoesNotExecutePredicate_PreservesOriginalError()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("original error"));
        var predicateExecuted = false;
        var errorFactoryCalled = false;

        var result = await resultTask.EnsureAsync(
            async x => { predicateExecuted = true; await Task.Yield(); return x > 0; },
            () => { errorFactoryCalled = true; return "error factory called"; });

        Assert.True(result.IsFailure);
        Assert.Equal("original error", result.Error);
        Assert.False(predicateExecuted);
        Assert.False(errorFactoryCalled);
    }

    #endregion

    #region Match

    [Theory]
    [InlineData(true, 42, "Success: 42")]
    [InlineData(false, 0, "Failure: error")]
    public async Task Match_ExecutesCorrectBranchAndReturnsValue(bool isSuccess, int value, string expected)
    {
        var resultTask = Task.FromResult(isSuccess
            ? Result<int, string>.Success(value)
            : Result<int, string>.Failure("error"));
        var wrongBranchExecuted = false;

        var matchResult = await resultTask.Match(
            onSuccess: x => { if (!isSuccess) wrongBranchExecuted = true; return $"Success: {x}"; },
            onFailure: e => { if (isSuccess) wrongBranchExecuted = true; return $"Failure: {e}"; });

        Assert.Equal(expected, matchResult);
        Assert.False(wrongBranchExecuted);
    }

    [Theory]
    [InlineData(true, 42, 42, "")]
    [InlineData(false, 0, 0, "error")]
    public async Task Match_Action_ExecutesCorrectBranch(bool isSuccess, int value, int expectedCaptured, string expectedError)
    {
        var resultTask = Task.FromResult(isSuccess
            ? Result<int, string>.Success(value)
            : Result<int, string>.Failure("error"));
        var capturedValue = 0;
        var capturedError = string.Empty;

        await resultTask.Match(
            onSuccess: x => capturedValue = x,
            onFailure: e => capturedError = e);

        Assert.Equal(expectedCaptured, capturedValue);
        Assert.Equal(expectedError, capturedError);
    }

    [Theory]
    [InlineData(true, 42, "Success: 42")]
    [InlineData(false, 0, "Failure: error")]
    public async Task MatchAsync_ExecutesCorrectBranchAndReturnsValue(bool isSuccess, int value, string expected)
    {
        var resultTask = Task.FromResult(isSuccess
            ? Result<int, string>.Success(value)
            : Result<int, string>.Failure("error"));
        var wrongBranchExecuted = false;

        var matchResult = await resultTask.MatchAsync(
            onSuccess: async x => { if (!isSuccess) wrongBranchExecuted = true; await Task.Yield(); return $"Success: {x}"; },
            onFailure: async e => { if (isSuccess) wrongBranchExecuted = true; await Task.Yield(); return $"Failure: {e}"; });

        Assert.Equal(expected, matchResult);
        Assert.False(wrongBranchExecuted);
    }

    [Theory]
    [InlineData(true, 42, 42, "")]
    [InlineData(false, 0, 0, "error")]
    public async Task MatchAsync_Action_ExecutesCorrectBranch(bool isSuccess, int value, int expectedCaptured, string expectedError)
    {
        var resultTask = Task.FromResult(isSuccess
            ? Result<int, string>.Success(value)
            : Result<int, string>.Failure("error"));
        var capturedValue = 0;
        var capturedError = string.Empty;

        await resultTask.MatchAsync(
            onSuccess: async x => { await Task.Yield(); capturedValue = x; },
            onFailure: async e => { await Task.Yield(); capturedError = e; });

        Assert.Equal(expectedCaptured, capturedValue);
        Assert.Equal(expectedError, capturedError);
    }

    #endregion

    #region Exception Propagation

    [Fact]
    public async Task MapAsync_ExceptionInMapper_PropagatesCorrectly()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await resultTask.MapAsync<int, int, string>(async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("Test exception");
            }));
    }

    [Fact]
    public async Task BindAsync_ExceptionInBinder_PropagatesCorrectly()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await resultTask.BindAsync<int, int, string>(async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("Test exception");
            }));
    }

    [Fact]
    public async Task TapAsync_ExceptionInAction_PropagatesCorrectly()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await resultTask.TapAsync(async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("Test exception");
            }));
    }

    [Fact]
    public async Task MatchAsync_ExceptionInHandler_PropagatesCorrectly()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(42));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await resultTask.MatchAsync<int, string, string>(
                async _ => { await Task.Yield(); throw new InvalidOperationException("Test exception"); },
                async e => { await Task.Yield(); return e; }));
    }

    #endregion

    #region Fluent Chaining

    [Fact]
    public async Task FluentChaining_AllOperationsChainCorrectly()
    {
        var result = await Task.FromResult(Result<int, string>.Success(5))
            .Map(x => x * 2)
            .Bind(x => Result<int, string>.Success(x + 5))
            .Map(x => x * 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.Value);
    }

    [Fact]
    public async Task FluentChaining_FailureInMiddle_ShortCircuits()
    {
        var mapAfterFailureExecuted = false;

        var result = await Task.FromResult(Result<int, string>.Success(5))
            .Map(x => x * 2)
            .Bind(_ => Result<int, string>.Failure("chain break"))
            .Map(x =>
            {
                mapAfterFailureExecuted = true;
                return x * 3;
            });

        Assert.True(result.IsFailure);
        Assert.Equal("chain break", result.Error);
        Assert.False(mapAfterFailureExecuted);
    }

    [Fact]
    public async Task FluentChaining_WithMixedSyncAndAsyncOperations()
    {
        var result = await Task.FromResult(Result<int, string>.Success(3))
            .MapAsync(async x => { await Task.Yield(); return x * 2; })
            .BindAsync(async x =>
            {
                await Task.Yield();
                return Result<string, string>.Success($"value: {x}");
            })
            .Map(s => s.ToUpper());

        Assert.True(result.IsSuccess);
        Assert.Equal("VALUE: 6", result.Value);
    }

    [Fact]
    public async Task FluentChaining_ComplexRealWorldScenario()
    {
        async Task<Result<int, string>> FetchFromDatabaseAsync(int id) =>
            await Task.FromResult(id > 0
                ? Result<int, string>.Success(id * 10)
                : Result<int, string>.Failure("Invalid ID"));

        var successLogs = new List<string>();
        var errorLogs = new List<string>();

        var result = await FetchFromDatabaseAsync(5)
            .Tap(x => successLogs.Add($"Fetched: {x}"))
            .TapError(e => errorLogs.Add($"Error: {e}"))
            .Ensure(x => x > 0, () => "Value must be positive")
            .Map(x => x * 2)
            .Tap(x => successLogs.Add($"Transformed: {x}"));

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
        Assert.Equal(2, successLogs.Count);
        Assert.Contains("Fetched: 50", successLogs);
        Assert.Contains("Transformed: 100", successLogs);
        Assert.Empty(errorLogs);
    }

    #endregion
}
