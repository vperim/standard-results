namespace StandardResults.UnitTests;

public class ResultAsyncTests
{
    [Fact]
    public async Task MatchAsync_Success_ExecutesOnSuccessCallback()
    {
        var result = Result<int, string>.Success(42);
        var executed = false;

        await result.MatchAsync(
            async _ => { await Task.Yield(); executed = true; },
            async _ => { await Task.Yield(); throw new InvalidOperationException("Should not execute"); }
        );

        Assert.True(executed);
    }

    [Fact]
    public async Task MatchAsync_Failure_ExecutesOnFailureCallback()
    {
        var result = Result<int, string>.Failure("error");
        var executed = false;

        await result.MatchAsync(
            async _ => { await Task.Yield(); throw new InvalidOperationException("Should not execute"); },
            async _ => { await Task.Yield(); executed = true; }
        );

        Assert.True(executed);
    }

    [Fact]
    public async Task MatchAsync_WithReturnValue_ReturnsCorrectResult()
    {
        var success = Result<int, string>.Success(10);
        var failure = Result<int, string>.Failure("boom");

        var successResult = await success.MatchAsync(
            async value => { await Task.Yield(); return value * 2; },
            async _ => { await Task.Yield(); return -1; }
        );

        var failureResult = await failure.MatchAsync(
            async value => { await Task.Yield(); return value * 2; },
            async error => { await Task.Yield(); return error.Length; }
        );

        Assert.Equal(20, successResult);
        Assert.Equal(4, failureResult);
    }

    [Fact]
    public async Task MapErrorAsync_Success_PreservesValue()
    {
        var result = Result<int, string>.Success(5);

        var mapped = await result.MapErrorAsync(async error => 
        {
            await Task.Yield();
            return new InvalidOperationException(error);
        });

        Assert.True(mapped.IsSuccess);
        Assert.Equal(5, mapped.Value);
    }

    [Fact]
    public async Task MapErrorAsync_Failure_TransformsError()
    {
        var result = Result<int, string>.Failure("timeout");

        var mapped = await result.MapErrorAsync(async error => 
        {
            await Task.Yield();
            return new TimeoutException($"Wrapped: {error}");
        });

        Assert.True(mapped.IsFailure);
        Assert.IsType<TimeoutException>(mapped.Error);
        Assert.Equal("Wrapped: timeout", mapped.Error.Message);
    }

    [Fact]
    public async Task BindAsync_Success_ExecutesBind()
    {
        var result = Result<int, string>.Success(3);

        var bound = await result.BindAsync(async value =>
        {
            await Task.Yield();
            return value > 0 
                ? Result<string, string>.Success($"positive:{value}")
                : Result<string, string>.Failure("negative");
        });

        Assert.True(bound.IsSuccess);
        Assert.Equal("positive:3", bound.Value);
    }

    [Fact]
    public async Task BindAsync_Failure_ShortCircuits()
    {
        var result = Result<int, string>.Failure("original error");

        var bound = await result.BindAsync(async _ =>
        {
            await Task.Yield();
            return Result<string, string>.Success("should not execute");
        });

        Assert.True(bound.IsFailure);
        Assert.Equal("original error", bound.Error);
    }

    [Fact]
    public async Task BindAsync_Success_CanReturnFailure()
    {
        var result = Result<int, string>.Success(-5);

        var bound = await result.BindAsync(async value =>
        {
            await Task.Yield();
            return value > 0 
                ? Result<string, string>.Success($"positive:{value}")
                : Result<string, string>.Failure("validation failed");
        });

        Assert.True(bound.IsFailure);
        Assert.Equal("validation failed", bound.Error);
    }

    [Fact]
    public async Task TapAsync_Success_ExecutesSideEffect_PreservesResult()
    {
        var result = Result<int, string>.Success(7);
        var sideEffectValue = 0;

        var tapped = await result.TapAsync(async value =>
        {
            await Task.Yield();
            sideEffectValue = value * 2;
        });

        Assert.Equal(14, sideEffectValue);
        Assert.True(tapped.IsSuccess);
        Assert.Equal(7, tapped.Value);
        Assert.Equal(result, tapped);
    }

    [Fact]
    public async Task TapAsync_Failure_DoesNotExecute_PreservesResult()
    {
        var result = Result<int, string>.Failure("error");
        var sideEffectExecuted = false;

        var tapped = await result.TapAsync(async _ =>
        {
            await Task.Yield();
            sideEffectExecuted = true;
        });

        Assert.False(sideEffectExecuted);
        Assert.True(tapped.IsFailure);
        Assert.Equal("error", tapped.Error);
        Assert.Equal(result, tapped);
    }

    [Fact]
    public async Task TapErrorAsync_Success_DoesNotExecute_PreservesResult()
    {
        var result = Result<int, string>.Success(8);
        var sideEffectExecuted = false;

        var tapped = await result.TapErrorAsync(async _ =>
        {
            await Task.Yield();
            sideEffectExecuted = true;
        });

        Assert.False(sideEffectExecuted);
        Assert.True(tapped.IsSuccess);
        Assert.Equal(8, tapped.Value);
        Assert.Equal(result, tapped);
    }

    [Fact]
    public async Task TapErrorAsync_Failure_ExecutesSideEffect_PreservesResult()
    {
        var result = Result<int, string>.Failure("network error");
        var capturedError = "";

        var tapped = await result.TapErrorAsync(async error =>
        {
            await Task.Yield();
            capturedError = $"logged: {error}";
        });

        Assert.Equal("logged: network error", capturedError);
        Assert.True(tapped.IsFailure);
        Assert.Equal("network error", tapped.Error);
        Assert.Equal(result, tapped);
    }

    [Fact]
    public async Task EnsureAsync_Success_PredicateTrue_PreservesResult()
    {
        var result = Result<int, string>.Success(10);

        var ensured = await result.EnsureAsync(
            async value => { await Task.Yield(); return value > 5; },
            () => "too small"
        );

        Assert.True(ensured.IsSuccess);
        Assert.Equal(10, ensured.Value);
        Assert.Equal(result, ensured);
    }

    [Fact]
    public async Task EnsureAsync_Success_PredicateFalse_ReturnsFailure()
    {
        var result = Result<int, string>.Success(3);

        var ensured = await result.EnsureAsync(
            async value => { await Task.Yield(); return value > 5; },
            () => "too small"
        );

        Assert.True(ensured.IsFailure);
        Assert.Equal("too small", ensured.Error);
    }

    [Fact]
    public async Task EnsureAsync_Failure_DoesNotExecutePredicate_CallsErrorFactory()
    {
        var result = Result<int, string>.Failure("original error");
        var predicateExecuted = false;

        var ensured = await result.EnsureAsync(
            async _ => { await Task.Yield(); predicateExecuted = true; return true; },
            () => "error factory called"
        );

        Assert.False(predicateExecuted);
        Assert.True(ensured.IsFailure);
        Assert.Equal("error factory called", ensured.Error);
    }

    [Fact]
    public async Task AsyncOperations_ConfigureAwaitFalse_DoesNotCaptureContext()
    {
        var result = Result<int, string>.Success(1);
        
        var mapped = await result.MapAsync(async value =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            return value + 1;
        });

        Assert.True(mapped.IsSuccess);
        Assert.Equal(2, mapped.Value);
    }

    [Fact]
    public async Task ConcurrentAsyncOperations_MaintainResultIntegrity()
    {
        var result = Result<int, string>.Success(5);

        var tasks = new[]
        {
            result.MapAsync(async v => { await Task.Yield(); return v * 2; }),
            result.MapAsync(async v => { await Task.Yield(); return v * 3; }),
            result.MapAsync(async v => { await Task.Yield(); return v * 4; })
        };

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(10, results[0].Value);
        Assert.Equal(15, results[1].Value);
        Assert.Equal(20, results[2].Value);
    }

    [Fact] 
    public async Task AsyncException_InMapAsync_PropagatesCorrectly()
    {
        var result = Result<int, string>.Success(1);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await result.MapAsync<int>(async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("async error");
            })
        );
    }

    [Fact]
    public async Task AsyncException_InBindAsync_PropagatesCorrectly()
    {
        var result = Result<int, string>.Success(1);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await result.BindAsync<int>(async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("async bind error");
            })
        );
    }
}