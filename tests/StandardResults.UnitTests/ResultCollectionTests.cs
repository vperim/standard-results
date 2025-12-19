namespace StandardResults.UnitTests;

public class ResultCollectionTests
{
    #region Sequence (fail-fast)

    public static TheoryData<Result<int, string>[], bool, int[], string?> SequenceCases => new()
    {
        // All success
        {
            new[] { Result<int, string>.Success(1), Result<int, string>.Success(2), Result<int, string>.Success(3) },
            true, new[] { 1, 2, 3 }, null
        },
        // Empty collection
        {
            Array.Empty<Result<int, string>>(),
            true, Array.Empty<int>(), null
        },
        // First failure (fail-fast)
        {
            new[] { Result<int, string>.Success(1), Result<int, string>.Failure("error1"), Result<int, string>.Failure("error2") },
            false, Array.Empty<int>(), "error1"
        }
    };

    [Theory]
    [MemberData(nameof(SequenceCases))]
    public void Sequence_ReturnsExpectedResult(
        Result<int, string>[] results,
        bool expectSuccess,
        int[] expectedValues,
        string? expectedError)
    {
        var sequenced = results.Sequence();

        Assert.Equal(expectSuccess, sequenced.IsSuccess);
        if (expectSuccess)
            Assert.Equal(expectedValues, sequenced.Value);
        else
            Assert.Equal(expectedError, sequenced.Error);
    }

    [Fact]
    public void Sequence_FailsOnFirstError_DoesNotEvaluateRest()
    {
        var evaluationCount = 0;

        IEnumerable<Result<int, string>> GetResults()
        {
            evaluationCount++;
            yield return Result<int, string>.Success(1);
            evaluationCount++;
            yield return Result<int, string>.Failure("error");
            evaluationCount++;
            yield return Result<int, string>.Success(3);
        }

        var sequenced = GetResults().Sequence();

        Assert.True(sequenced.IsFailure);
        Assert.Equal(2, evaluationCount); // Should stop after second item
    }

    #endregion

    #region SequenceAll (ValidationErrors)

    [Fact]
    public void SequenceAll_ValidationErrors_AllSuccess_ReturnsAllValues()
    {
        var results = new[]
        {
            Result<int, ValidationErrors>.Success(1),
            Result<int, ValidationErrors>.Success(2),
            Result<int, ValidationErrors>.Success(3)
        };

        var sequenced = results.SequenceAll();

        Assert.True(sequenced.IsSuccess);
        Assert.Equal(new[] { 1, 2, 3 }, sequenced.Value);
    }

    [Fact]
    public void SequenceAll_ValidationErrors_CollectsAllErrors()
    {
        var results = new[]
        {
            Result<int, ValidationErrors>.Success(1),
            Result<int, ValidationErrors>.Failure(
                ValidationErrors.Empty.WithField("Field1", "Error 1")),
            Result<int, ValidationErrors>.Success(3),
            Result<int, ValidationErrors>.Failure(
                ValidationErrors.Empty.WithField("Field2", "Error 2"))
        };

        var sequenced = results.SequenceAll();

        Assert.True(sequenced.IsFailure);
        Assert.Equal(2, sequenced.Error.Count);
        Assert.Contains(sequenced.Error.Errors, e => e.Code == "Field1");
        Assert.Contains(sequenced.Error.Errors, e => e.Code == "Field2");
    }

    [Fact]
    public void SequenceAll_ValidationErrors_EmptyCollection_ReturnsEmptyList()
    {
        var results = Array.Empty<Result<int, ValidationErrors>>();

        var sequenced = results.SequenceAll();

        Assert.True(sequenced.IsSuccess);
        Assert.Empty(sequenced.Value);
    }

    #endregion

    #region SequenceAll (ErrorCollection)

    [Fact]
    public void SequenceAll_ErrorCollection_CollectsAllErrors()
    {
        var results = new[]
        {
            Result<int, ErrorCollection>.Success(1),
            Result<int, ErrorCollection>.Failure(
                ErrorCollection.Empty.WithError("code1", "Error 1")),
            Result<int, ErrorCollection>.Failure(
                ErrorCollection.Empty.WithError("code2", "Error 2"))
        };

        var sequenced = results.SequenceAll();

        Assert.True(sequenced.IsFailure);
        Assert.Equal(2, sequenced.Error.Count);
    }

    #endregion

    #region Traverse (fail-fast)

    public static TheoryData<int[], bool, string[], string?> TraverseCases => new()
    {
        // All success (odd numbers only - transform succeeds for all)
        { new[] { 1, 3, 5 }, true, new[] { "item-1", "item-3", "item-5" }, null },
        // Empty collection
        { Array.Empty<int>(), true, Array.Empty<string>(), null },
        // First failure (fail-fast) - Real-world example: Athena's BoletoService uses this pattern
        // to generate boletos for parcelas, failing fast on the first error encountered.
        { new[] { 1, 2, 3, 4 }, false, Array.Empty<string>(), "even-2" }
    };

    [Theory]
    [MemberData(nameof(TraverseCases))]
    public void Traverse_ReturnsExpectedResult(
        int[] items,
        bool expectSuccess,
        string[] expectedValues,
        string? expectedError)
    {
        var result = items.Traverse(i =>
            i % 2 == 0
                ? Result<string, string>.Failure($"even-{i}")
                : Result<string, string>.Success($"item-{i}"));

        Assert.Equal(expectSuccess, result.IsSuccess);
        if (expectSuccess)
            Assert.Equal(expectedValues, result.Value);
        else
            Assert.Equal(expectedError, result.Error);
    }

    #endregion

    #region TraverseAll (ValidationErrors)

    [Fact]
    public void TraverseAll_ValidationErrors_CollectsAllErrors()
    {
        var items = new[] { 1, 2, 3, 4 };

        var result = items.TraverseAll(i =>
            i % 2 == 0
                ? Result<string, ValidationErrors>.Failure(
                    ValidationErrors.Empty.WithField($"Item{i}", $"Even number: {i}"))
                : Result<string, ValidationErrors>.Success($"odd-{i}"));

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Error.Count); // 2 and 4 are even
        Assert.Contains(result.Error.Errors, e => e.Code == "Item2");
        Assert.Contains(result.Error.Errors, e => e.Code == "Item4");
    }

    [Fact]
    public void TraverseAll_ValidationErrors_AllSuccess_ReturnsAllValues()
    {
        var items = new[] { 1, 3, 5 };

        var result = items.TraverseAll(i =>
            Result<string, ValidationErrors>.Success($"odd-{i}"));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "odd-1", "odd-3", "odd-5" }, result.Value);
    }

    #endregion

    #region SequenceAsync (fail-fast)

    public static TheoryData<Task<Result<int, string>>[], bool, int[], string?> SequenceAsyncCases => new()
    {
        // All success
        {
            new[]
            {
                Task.FromResult(Result<int, string>.Success(1)),
                Task.FromResult(Result<int, string>.Success(2)),
                Task.FromResult(Result<int, string>.Success(3))
            },
            true, new[] { 1, 2, 3 }, null
        },
        // First failure
        {
            new[]
            {
                Task.FromResult(Result<int, string>.Success(1)),
                Task.FromResult(Result<int, string>.Failure("error1")),
                Task.FromResult(Result<int, string>.Failure("error2"))
            },
            false, Array.Empty<int>(), "error1"
        }
    };

    [Theory]
    [MemberData(nameof(SequenceAsyncCases))]
    public async Task SequenceAsync_ReturnsExpectedResult(
        Task<Result<int, string>>[] tasks,
        bool expectSuccess,
        int[] expectedValues,
        string? expectedError)
    {
        var result = await tasks.SequenceAsync();

        Assert.Equal(expectSuccess, result.IsSuccess);
        if (expectSuccess)
            Assert.Equal(expectedValues, result.Value);
        else
            Assert.Equal(expectedError, result.Error);
    }

    #endregion

    #region SequenceAllAsync (ValidationErrors)

    [Fact]
    public async Task SequenceAllAsync_ValidationErrors_CollectsAllErrors()
    {
        var tasks = new[]
        {
            Task.FromResult(Result<int, ValidationErrors>.Success(1)),
            Task.FromResult(Result<int, ValidationErrors>.Failure(
                ValidationErrors.Empty.WithField("Field1", "Error 1"))),
            Task.FromResult(Result<int, ValidationErrors>.Failure(
                ValidationErrors.Empty.WithField("Field2", "Error 2")))
        };

        var result = await tasks.SequenceAllAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Error.Count);
    }

    [Fact]
    public async Task SequenceAllAsync_ValidationErrors_AllSuccess_ReturnsAllValues()
    {
        var tasks = new[]
        {
            Task.FromResult(Result<int, ValidationErrors>.Success(1)),
            Task.FromResult(Result<int, ValidationErrors>.Success(2)),
            Task.FromResult(Result<int, ValidationErrors>.Success(3))
        };

        var result = await tasks.SequenceAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 2, 3 }, result.Value);
    }

    #endregion

    #region TraverseAsync (fail-fast)

    [Fact]
    public async Task TraverseAsync_AllSuccess_ReturnsAllTransformedValues()
    {
        var items = new[] { 1, 2, 3 };

        var result = await items.TraverseAsync(async i =>
        {
            await Task.Yield();
            return Result<string, string>.Success($"item-{i}");
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "item-1", "item-2", "item-3" }, result.Value);
    }

    [Fact]
    public async Task TraverseAsync_FirstFailure_ReturnsFirstError()
    {
        var items = new[] { 1, 2, 3 };

        var result = await items.TraverseAsync(async i =>
        {
            await Task.Yield();
            return i == 2
                ? Result<string, string>.Failure("error-2")
                : Result<string, string>.Success($"item-{i}");
        });

        Assert.True(result.IsFailure);
        Assert.Equal("error-2", result.Error);
    }

    #endregion

    #region TraverseAllAsync (ValidationErrors)

    [Fact]
    public async Task TraverseAllAsync_ValidationErrors_CollectsAllErrors()
    {
        var items = new[] { 1, 2, 3, 4 };

        var result = await items.TraverseAllAsync(async i =>
        {
            await Task.Yield();
            return i % 2 == 0
                ? Result<string, ValidationErrors>.Failure(
                    ValidationErrors.Empty.WithField($"Item{i}", $"Even: {i}"))
                : Result<string, ValidationErrors>.Success($"odd-{i}");
        });

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Error.Count);
    }

    [Fact]
    [Trait("Category", "Slow")]
    public async Task TraverseAllAsync_ValidationErrors_ExecutesInParallel()
    {
        var items = new[] { 1, 2, 3 };
        var startedSignals = items.Select(_ => new TaskCompletionSource<bool>()).ToArray();
        var releaseSignal = new TaskCompletionSource<bool>();

        var resultTask = items.TraverseAllAsync(async i =>
        {
            // Signal that this task has started
            startedSignals[i - 1].SetResult(true);

            // Wait for release signal
            await releaseSignal.Task;

            return Result<string, ValidationErrors>.Success($"item-{i}");
        });

        // Wait for all tasks to start (proves parallel execution)
        var allStarted = await Task.WhenAll(startedSignals.Select(s => s.Task))
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(allStarted.All(s => s), "All tasks should start in parallel");

        // Release all tasks to complete
        releaseSignal.SetResult(true);

        var result = await resultTask;
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Real-world scenarios

    [Fact]
    public void SequenceAll_ValidationScenario_CollectAllErrors()
    {
        // Simulating collecting all validation errors from multiple fields
        var fields = new[]
        {
            ("Email", ""),
            ("Password", "123"),
            ("Username", "a"),
            ("Phone", "valid-phone")
        };

        Result<string, ValidationErrors> ValidateField((string name, string value) field) =>
            field switch
            {
                ("Email", "") => ValidationErrors.Empty.WithField("Email", "Email is required"),
                ("Password", var p) when p.Length < 8 => ValidationErrors.Empty.WithField("Password", "Password too short"),
                ("Username", var u) when u.Length < 3 => ValidationErrors.Empty.WithField("Username", "Username too short"),
                _ => Result<string, ValidationErrors>.Success(field.value)
            };

        var result = fields.TraverseAll(ValidateField);

        Assert.True(result.IsFailure);
        Assert.Equal(3, result.Error.Count); // Email, Password, Username errors
        Assert.DoesNotContain(result.Error.Errors, e => e.Code == "Phone");
    }

    [Fact]
    public async Task TraverseAllAsync_ParallelFetch_CollectsAllErrors()
    {
        // Simulating parallel fetching with some failures
        var userIds = new[] { 1, 2, 3, 4, 5 };

        async Task<Result<string, ValidationErrors>> FetchUser(int id)
        {
            await Task.Yield();
            return id % 2 == 0
                ? ValidationErrors.Empty.WithField($"User{id}", $"User {id} not found")
                : Result<string, ValidationErrors>.Success($"user-{id}");
        }

        var result = await userIds.TraverseAllAsync(FetchUser);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Error.Count); // Users 2 and 4 failed
    }

    #endregion
}
