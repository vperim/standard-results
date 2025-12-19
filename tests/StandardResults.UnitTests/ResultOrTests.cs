namespace StandardResults.UnitTests;

public class ResultOrTests
{
    #region Or with value

    public static TheoryData<Result<int, string>, int, int> OrWithValueCases => new()
    {
        { Result<int, string>.Success(42), 100, 42 },       // Success: returns original
        { Result<int, string>.Failure("error"), 100, 100 }  // Failure: returns fallback
    };

    [Theory]
    [MemberData(nameof(OrWithValueCases))]
    public void Or_WithValue_ReturnsExpectedResult(Result<int, string> input, int fallback, int expected)
    {
        var result = input.Or(fallback);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    #endregion

    #region Or with factory

    [Fact]
    public void Or_WithFactory_OnSuccess_DoesNotInvokeFactory()
    {
        var success = Result<int, string>.Success(42);
        var factoryInvoked = false;

        var result = success.Or(_ =>
        {
            factoryInvoked = true;
            return 100;
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(factoryInvoked);
    }

    [Fact]
    public void Or_WithFactory_OnFailure_InvokesFactoryWithError()
    {
        var failure = Result<int, string>.Failure("error");
        string? receivedError = null;

        var result = failure.Or(e =>
        {
            receivedError = e;
            return 100;
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
        Assert.Equal("error", receivedError);
    }

    #endregion

    #region OrElse with Result

    public static TheoryData<Result<int, string>, Result<int, string>, bool, int, string?> OrElseCases => new()
    {
        // Success: returns original, ignores fallback
        { Result<int, string>.Success(42), Result<int, string>.Success(100), true, 42, null },
        // Failure + Success fallback: returns fallback success
        { Result<int, string>.Failure("error"), Result<int, string>.Success(100), true, 100, null },
        // Failure + Failure fallback: returns fallback failure
        { Result<int, string>.Failure("error1"), Result<int, string>.Failure("error2"), false, 0, "error2" }
    };

    [Theory]
    [MemberData(nameof(OrElseCases))]
    public void OrElse_ReturnsExpectedResult(
        Result<int, string> input,
        Result<int, string> fallback,
        bool expectSuccess,
        int expectedValue,
        string? expectedError)
    {
        var result = input.OrElse(fallback);

        Assert.Equal(expectSuccess, result.IsSuccess);
        if (expectSuccess)
            Assert.Equal(expectedValue, result.Value);
        else
            Assert.Equal(expectedError, result.Error);
    }

    #endregion

    #region OrElse with factory

    [Fact]
    public void OrElse_WithFactory_OnSuccess_DoesNotInvokeFactory()
    {
        var success = Result<int, string>.Success(42);
        var factoryInvoked = false;

        var result = success.OrElse(_ =>
        {
            factoryInvoked = true;
            return Result<int, string>.Success(100);
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(factoryInvoked);
    }

    [Fact]
    public void OrElse_WithFactory_OnFailure_InvokesFactoryWithError()
    {
        var failure = Result<int, string>.Failure("error");
        string? receivedError = null;

        var result = failure.OrElse(e =>
        {
            receivedError = e;
            return Result<int, string>.Success(100);
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
        Assert.Equal("error", receivedError);
    }

    [Theory]
    [InlineData("transient", true, 0, null)]
    [InlineData("permanent", false, 0, "permanent")]
    public void OrElse_WithFactory_BehavesBasedOnErrorType(
        string errorType,
        bool expectSuccess,
        int expectedValue,
        string? expectedError)
    {
        var failure = Result<int, string>.Failure(errorType);

        var result = failure.OrElse(e =>
            e == "transient"
                ? Result<int, string>.Success(0)
                : Result<int, string>.Failure(e));

        Assert.Equal(expectSuccess, result.IsSuccess);
        if (expectSuccess)
            Assert.Equal(expectedValue, result.Value);
        else
            Assert.Equal(expectedError, result.Error);
    }

    #endregion

    #region OrAsync

    [Fact]
    public async Task OrAsync_OnSuccess_DoesNotInvokeFactory()
    {
        var success = Result<int, string>.Success(42);
        var factoryInvoked = false;

        var result = await success.OrAsync(async _ =>
        {
            factoryInvoked = true;
            await Task.Yield();
            return 100;
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(factoryInvoked);
    }

    [Fact]
    public async Task OrAsync_OnFailure_InvokesAsyncFactory()
    {
        var failure = Result<int, string>.Failure("error");
        string? receivedError = null;

        var result = await failure.OrAsync(async e =>
        {
            receivedError = e;
            await Task.Yield();
            return 100;
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
        Assert.Equal("error", receivedError);
    }

    #endregion

    #region OrElseAsync

    [Fact]
    public async Task OrElseAsync_OnSuccess_DoesNotInvokeFactory()
    {
        var success = Result<int, string>.Success(42);
        var factoryInvoked = false;

        var result = await success.OrElseAsync(async _ =>
        {
            factoryInvoked = true;
            await Task.Yield();
            return Result<int, string>.Success(100);
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(factoryInvoked);
    }

    [Fact]
    public async Task OrElseAsync_OnFailure_InvokesAsyncFactory()
    {
        var failure = Result<int, string>.Failure("error");

        var result = await failure.OrElseAsync(async _ =>
        {
            await Task.Yield();
            return Result<int, string>.Success(100);
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public async Task OrElseAsync_CanChainMultipleFallbacks()
    {
        var failure = Result<int, string>.Failure("error1");

        var result = await failure
            .OrElseAsync(async _ =>
            {
                await Task.Yield();
                return Result<int, string>.Failure("error2");
            })
            .OrElseAsync(async _ =>
            {
                await Task.Yield();
                return Result<int, string>.Failure("error3");
            })
            .OrElseAsync(async _ =>
            {
                await Task.Yield();
                return Result<int, string>.Success(999);
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(999, result.Value);
    }

    #endregion

    #region Task extension methods

    [Fact]
    public async Task TaskOr_OnSuccess_ReturnsOriginalValue()
    {
        var successTask = Task.FromResult(Result<int, string>.Success(42));

        var result = await successTask.Or(100);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TaskOr_OnFailure_ReturnsFallbackValue()
    {
        var failureTask = Task.FromResult(Result<int, string>.Failure("error"));

        var result = await failureTask.Or(100);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public async Task TaskOrElseAsync_CanChain()
    {
        Task<Result<int, string>> GetFromPrimary() =>
            Task.FromResult(Result<int, string>.Failure("primary failed"));

        Task<Result<int, string>> GetFromSecondary() =>
            Task.FromResult(Result<int, string>.Failure("secondary failed"));

        Task<Result<int, string>> GetFromTertiary() =>
            Task.FromResult(Result<int, string>.Success(42));

        var result = await GetFromPrimary()
            .OrElseAsync(_ => GetFromSecondary())
            .OrElseAsync(_ => GetFromTertiary());

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    #endregion

    #region Default/uninitialized handling

    [Fact]
    public void Or_OnDefault_Throws()
    {
        var def = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => def.Or(100));
    }

    [Fact]
    public void OrElse_OnDefault_Throws()
    {
        var def = default(Result<int, string>);
        var fallback = Result<int, string>.Success(100);

        Assert.Throws<InvalidOperationException>(() => def.OrElse(fallback));
    }

    #endregion
}
