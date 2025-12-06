namespace StandardResults.UnitTests;

public class ResultTests
{
    [Fact]
    public void Default_IsDefault_And_Throws_On_StateAccess()
    {
        var r = default(Result<int, string>);

        Assert.True(r.IsDefault);

        var ex1 = Assert.Throws<InvalidOperationException>(() => _ = r.IsSuccess);
        Assert.Equal("Result is default (uninitialized).", ex1.Message);

        var ex2 = Assert.Throws<InvalidOperationException>(() => _ = r.IsFailure);
        Assert.Equal("Result is default (uninitialized).", ex2.Message);

        var ex3 = Assert.Throws<InvalidOperationException>(() => _ = r.Value);
        Assert.Equal("Result is default (uninitialized).", ex3.Message);

        var ex4 = Assert.Throws<InvalidOperationException>(() => _ = r.Error);
        Assert.Equal("Result is default (uninitialized).", ex4.Message);
    }

    [Fact]
    public void Success_HasValue_NoError()
    {
        var r = Result<int, string>.Success(123);

        Assert.True(r.IsSuccess);
        Assert.False(r.IsFailure);
        Assert.Equal(123, r.Value);
        Assert.Throws<InvalidOperationException>(() => _ = r.Error);

        Assert.True(r.TryGetValue(out var v) && v == 123);
        Assert.False(r.TryGetError(out _));

        Assert.Equal("Success(123)", r.ToString());
    }

    [Fact]
    public void Failure_HasError_NoValue()
    {
        var r = Result<int, string>.Failure("boom");

        Assert.True(r.IsFailure);
        Assert.False(r.IsSuccess);
        Assert.Equal("boom", r.Error);
        Assert.Throws<InvalidOperationException>(() => _ = r.Value);

        Assert.True(r.TryGetError(out var e) && e == "boom");
        Assert.False(r.TryGetValue(out _));

        Assert.Equal("Failure(boom)", r.ToString());
    }

    [Fact]
    public void Equality_Rules()
    {
        var a1 = Result<int, string>.Success(5);
        var a2 = Result<int, string>.Success(5);
        var b  = Result<int, string>.Success(6);
        var f1 = Result<int, string>.Failure("err");
        var f2 = Result<int, string>.Failure("err");
        var f3 = Result<int, string>.Failure("other");

        Assert.True(a1 == a2);
        Assert.True(f1 == f2);
        Assert.True(a1 != b);
        Assert.True(f1 != f3);
        Assert.True(true);
        Assert.False(default == a1);

        // HashCodes should reflect equality contract (not necessarily distinct).
        Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
        Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
        Assert.Equal(0, default(Result<int, string>).GetHashCode());
    }

    [Fact]
    public void Map_And_Bind_Respect_ShortCircuiting()
    {
        var ok = Result<int, string>.Success(2);
        var fail = Result<int, string>.Failure("x");

        var mappedOk = ok.Map(v => v * 10);
        Assert.True(mappedOk.IsSuccess);
        Assert.Equal(20, mappedOk.Value);

        var mappedFail = fail.Map(v => v * 10);
        Assert.True(mappedFail.IsFailure);
        Assert.Equal("x", mappedFail.Error);

        var boundOk = ok.Bind(v => Result<string, string>.Success($"v={v}"));
        Assert.True(boundOk.IsSuccess);
        Assert.Equal("v=2", boundOk.Value);

        var boundFail = fail.Bind(_ => Result<string, string>.Success("should not run"));
        Assert.True(boundFail.IsFailure);
        Assert.Equal("x", boundFail.Error);
    }

    [Fact]
    public void MapError_Changes_Error_Type_And_Preserves_Success()
    {
        var ok = Result<int, string>.Success(7);
        var fail = Result<int, string>.Failure("bad");

        var ok2 = ok.MapError(e => new InvalidOperationException(e));
        Assert.True(ok2.IsSuccess);
        Assert.Equal(7, ok2.Value);

        var fail2 = fail.MapError(e => new InvalidOperationException(e));
        Assert.True(fail2.IsFailure);
        Assert.IsType<InvalidOperationException>(fail2.Error);
        Assert.Equal("bad", fail2.Error.Message);
    }

    [Fact]
    public async Task Async_Helpers_Work_As_Expected()
    {
        var ok = Result<int, string>.Success(3);
        var fail = Result<int, string>.Failure("nope");

        var mapOk = await ok.MapAsync(async v => { await Task.Yield(); return v + 1; });
        Assert.True(mapOk.IsSuccess);
        Assert.Equal(4, mapOk.Value);

        var mapFail = await fail.MapAsync(async v => { await Task.Yield(); return v + 1; });
        Assert.True(mapFail.IsFailure);
        Assert.Equal("nope", mapFail.Error);

        var tapFlag = false;
        var tapped = await ok.TapAsync(async _ => { await Task.Yield(); tapFlag = true; });
        Assert.True(tapFlag);
        Assert.True(tapped.IsSuccess);

        var tapErrFlag = false;
        var tappedErr = await fail.TapErrorAsync(async _ => { await Task.Yield(); tapErrFlag = true; });
        Assert.True(tapErrFlag);
        Assert.True(tappedErr.IsFailure);

        var ensured = await ok.EnsureAsync(async v => { await Task.Yield(); return v == 3; }, () => "bad");
        Assert.True(ensured.IsSuccess);

        var ensuredFail = await ok.EnsureAsync(async v => { await Task.Yield(); return v == 999; }, () => "bad");
        Assert.True(ensuredFail.IsFailure);
        Assert.Equal("bad", ensuredFail.Error);
    }

    [Fact]
    public void Static_Try_Maps_Exceptions_And_Optionally_OperationCanceled()
    {
        var ok = Result.Try<int, string>(() => 42, ex => $"E:{ex.GetType().Name}");
        Assert.True(ok.IsSuccess);
        Assert.Equal(42, ok.Value);

        var fail = Result.Try<int, string>(() => throw new InvalidOperationException("x"), ex => ex.Message);
        Assert.True(fail.IsFailure);
        Assert.Equal("x", fail.Error);

        // OperationCanceledException rethrows if handler not provided
        Assert.Throws<OperationCanceledException>(() =>
            Result.Try<int, string>(() => throw new OperationCanceledException(), _ => "ignored"));

        var canceledMapped = Result.Try<int, string>(
            () => throw new OperationCanceledException(),
            _ => "ignored",
            oce => $"CANCELED:{oce.GetType().Name}");
        Assert.True(canceledMapped.IsFailure);
        Assert.Equal("CANCELED:OperationCanceledException", canceledMapped.Error);
    }

    [Fact]
    public async Task Static_TryAsync_Maps_Exceptions_And_Optionally_OperationCanceled()
    {
        var ok = await Result.TryAsync<int, string>(async () => { await Task.Yield(); return 7; }, ex => $"E:{ex.GetType().Name}");
        Assert.True(ok.IsSuccess);
        Assert.Equal(7, ok.Value);

        var fail = await Result.TryAsync<int, string>(async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        }, ex => ex.Message);
        Assert.True(fail.IsFailure);
        Assert.Equal("boom", fail.Error);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Result.TryAsync<int, string>(async () =>
            {
                await Task.Yield();
                throw new OperationCanceledException();
            }, _ => "ignored"));

        var mappedCancel = await Result.TryAsync<int, string>(async () =>
        {
            await Task.Yield();
            throw new OperationCanceledException();
        }, _ => "ignored", oce => $"C:{oce.GetType().Name}");
        Assert.True(mappedCancel.IsFailure);
        Assert.Equal("C:OperationCanceledException", mappedCancel.Error);
    }

    [Fact]
    public void Failure_NullError_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Failure(null!));
    }

    [Fact]
    public void GetValueOrDefault_Success_ReturnsValue()
    {
        var result = Result<int, string>.Success(42);
        Assert.Equal(42, result.GetValueOrDefault(999));
    }

    [Fact]
    public void GetValueOrDefault_Failure_ReturnsDefault()
    {
        var result = Result<int, string>.Failure("error");
        Assert.Equal(999, result.GetValueOrDefault(999));
    }

    [Fact]
    public void GetErrorOrDefault_Success_ReturnsDefault()
    {
        var result = Result<int, string>.Success(42);
        Assert.Equal("default", result.GetErrorOrDefault("default"));
    }

    [Fact]
    public void GetErrorOrDefault_Failure_ReturnsError()
    {
        var result = Result<int, string>.Failure("actual error");
        Assert.Equal("actual error", result.GetErrorOrDefault("default"));
    }

    [Fact]
    public void Match_Success_ExecutesOnSuccessCallback()
    {
        var result = Result<int, string>.Success(5);
        var executed = false;
        
        result.Match(
            value => { executed = true; Assert.Equal(5, value); },
            _ => throw new InvalidOperationException("Should not execute")
        );
        
        Assert.True(executed);
    }

    [Fact]
    public void Match_Failure_ExecutesOnFailureCallback()
    {
        var result = Result<int, string>.Failure("test error");
        var executed = false;
        
        result.Match(
            _ => throw new InvalidOperationException("Should not execute"),
            error => { executed = true; Assert.Equal("test error", error); }
        );
        
        Assert.True(executed);
    }

    [Fact]
    public void Match_WithReturnValue_ReturnsCorrectResult()
    {
        var success = Result<int, string>.Success(10);
        var failure = Result<int, string>.Failure("error");

        var successResult = success.Match(
            value => value * 2,
            _ => -1
        );

        var failureResult = failure.Match(
            value => value * 2,
            error => error.Length
        );

        Assert.Equal(20, successResult);
        Assert.Equal(5, failureResult);
    }

    [Fact]
    public void Deconstruct_Success_ExtractsCorrectValues()
    {
        var result = Result<int, string>.Success(42);
        
        var (isSuccess, value, error) = result;
        
        Assert.True(isSuccess);
        Assert.Equal(42, value);
        Assert.Null(error);
    }

    [Fact]
    public void Deconstruct_Failure_ExtractsCorrectValues()
    {
        var result = Result<int, string>.Failure("test error");
        
        var (isSuccess, value, error) = result;
        
        Assert.False(isSuccess);
        Assert.Equal(0, value); // default value for int
        Assert.Equal("test error", error);
    }

    [Fact]
    public void ToString_Default_ShowsDefault()
    {
        var result = default(Result<int, string>);
        Assert.Equal("Result<default>", result.ToString());
    }

    [Fact]
    public void TryGetValue_Default_ReturnsFalse()
    {
        var result = default(Result<int, string>);
        Assert.Throws<InvalidOperationException>(() => result.TryGetValue(out _));
    }

    [Fact]
    public void TryGetError_Default_ReturnsFalse()
    {
        var result = default(Result<int, string>);
        Assert.Throws<InvalidOperationException>(() => result.TryGetError(out _));
    }

    [Fact]
    public void Equality_WithBoxing_WorksCorrectly()
    {
        var result1 = Result<int, string>.Success(5);
        object result2 = Result<int, string>.Success(5);
        object result3 = Result<int, string>.Success(6);
        object notResult = "not a result";

        Assert.True(result1.Equals(result2));
        Assert.False(result1.Equals(result3));
        Assert.False(result1.Equals(notResult));
        Assert.False(result1.Equals((object?)null));
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailureResult()
    {
        Result<int, string> result = "error message";

        Assert.True(result.IsFailure);
        Assert.Equal("error message", result.Error);
    }

    [Fact]
    public void ImplicitConversion_NullError_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<int, string> result = (string)null!;
        });
    }

    [Fact]
    public void ImplicitConversion_InMethodReturn_WorksForErrors()
    {
        static Result<int, string> GetValue(bool succeed)
        {
            if (!succeed)
                return "failed";  // Implicit conversion for error

            return Result<int, string>.Success(100);  // Explicit for success
        }

        var success = GetValue(true);
        var failure = GetValue(false);

        Assert.True(success.IsSuccess);
        Assert.Equal(100, success.Value);

        Assert.True(failure.IsFailure);
        Assert.Equal("failed", failure.Error);
    }

    [Fact]
    public void ImplicitConversion_WithCustomErrorType_Works()
    {
        Result<string, Error> GetUser(int id)
        {
            if (id <= 0)
                return Error.Permanent("invalid_id", "ID must be positive");  // Implicit for error

            return Result<string, Error>.Success("User data");  // Explicit for success
        }

        var failure = GetUser(-1);
        var success = GetUser(1);

        Assert.True(failure.IsFailure);
        Assert.Equal("invalid_id", failure.Error.Code);
        Assert.Equal("ID must be positive", failure.Error.Message);

        Assert.True(success.IsSuccess);
        Assert.Equal("User data", success.Value);
    }

    [Fact]
    public void ImplicitConversion_WithValidationErrors_Works()
    {
        Result<string, ValidationErrors> ValidateInput(string? input)
        {
            var validation = ValidationErrors.Empty
                .RequireNotEmpty(input, "input", "Input is required");

            if (validation.HasErrors)
                return validation;  // Implicit for error

            return Result<string, ValidationErrors>.Success(input!);
        }

        var failure = ValidateInput(null);
        var success = ValidateInput("valid");

        Assert.True(failure.IsFailure);
        Assert.True(failure.Error.HasErrors);

        Assert.True(success.IsSuccess);
        Assert.Equal("valid", success.Value);
    }
}