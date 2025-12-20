namespace StandardResults;

public static class Result
{
    /// <summary>
    /// Creates a Success result if the value is not null, otherwise creates a Failure with the error from the factory.
    /// Use this to convert nullable reference types into Results.
    /// </summary>
    public static Result<T, TError> FromNullable<T, TError>(T? value, Func<TError> errorFactory)
        where T : class
        where TError : notnull
    {
        return value is not null
            ? Result<T, TError>.Success(value)
            : Result<T, TError>.Failure(errorFactory());
    }

    /// <summary>
    /// Creates a Success result if the value has a value, otherwise creates a Failure with the error from the factory.
    /// Use this to convert nullable value types into Results.
    /// </summary>
    public static Result<T, TError> FromNullable<T, TError>(T? value, Func<TError> errorFactory)
        where T : struct
        where TError : notnull
    {
        return value.HasValue
            ? Result<T, TError>.Success(value.Value)
            : Result<T, TError>.Failure(errorFactory());
    }

    /// <summary>
    /// Combines two Results into a single Result containing a tuple.
    /// Returns the first failure encountered (fail-fast).
    /// </summary>
    public static Result<(T1, T2), TError> Combine<T1, T2, TError>(
        Result<T1, TError> result1,
        Result<T2, TError> result2)
        where TError : notnull
    {
        if (result1.IsFailure) return Result<(T1, T2), TError>.Failure(result1.Error);
        if (result2.IsFailure) return Result<(T1, T2), TError>.Failure(result2.Error);
        return Result<(T1, T2), TError>.Success((result1.Value, result2.Value));
    }

    /// <summary>
    /// Combines three Results into a single Result containing a tuple.
    /// Returns the first failure encountered (fail-fast).
    /// </summary>
    public static Result<(T1, T2, T3), TError> Combine<T1, T2, T3, TError>(
        Result<T1, TError> result1,
        Result<T2, TError> result2,
        Result<T3, TError> result3)
        where TError : notnull
    {
        if (result1.IsFailure) return Result<(T1, T2, T3), TError>.Failure(result1.Error);
        if (result2.IsFailure) return Result<(T1, T2, T3), TError>.Failure(result2.Error);
        if (result3.IsFailure) return Result<(T1, T2, T3), TError>.Failure(result3.Error);
        return Result<(T1, T2, T3), TError>.Success((result1.Value, result2.Value, result3.Value));
    }

    /// <summary>
    /// Combines four Results into a single Result containing a tuple.
    /// Returns the first failure encountered (fail-fast).
    /// </summary>
    public static Result<(T1, T2, T3, T4), TError> Combine<T1, T2, T3, T4, TError>(
        Result<T1, TError> result1,
        Result<T2, TError> result2,
        Result<T3, TError> result3,
        Result<T4, TError> result4)
        where TError : notnull
    {
        if (result1.IsFailure) return Result<(T1, T2, T3, T4), TError>.Failure(result1.Error);
        if (result2.IsFailure) return Result<(T1, T2, T3, T4), TError>.Failure(result2.Error);
        if (result3.IsFailure) return Result<(T1, T2, T3, T4), TError>.Failure(result3.Error);
        if (result4.IsFailure) return Result<(T1, T2, T3, T4), TError>.Failure(result4.Error);
        return Result<(T1, T2, T3, T4), TError>.Success((result1.Value, result2.Value, result3.Value, result4.Value));
    }

    /// <summary>
    /// Combines two Results, collecting ALL errors from all failed results.
    /// Use this when you want to show all validation errors at once instead of failing fast.
    /// </summary>
    public static Result<(T1, T2), ValidationErrors> CombineAll<T1, T2>(
        Result<T1, ValidationErrors> result1,
        Result<T2, ValidationErrors> result2)
    {
        var errors = ValidationErrors.Empty;
        if (result1.IsFailure) errors = errors.Merge(result1.Error);
        if (result2.IsFailure) errors = errors.Merge(result2.Error);

        return errors.HasErrors
            ? Result<(T1, T2), ValidationErrors>.Failure(errors)
            : Result<(T1, T2), ValidationErrors>.Success((result1.Value, result2.Value));
    }

    /// <summary>
    /// Combines three Results, collecting ALL errors from all failed results.
    /// Use this when you want to show all validation errors at once instead of failing fast.
    /// </summary>
    public static Result<(T1, T2, T3), ValidationErrors> CombineAll<T1, T2, T3>(
        Result<T1, ValidationErrors> result1,
        Result<T2, ValidationErrors> result2,
        Result<T3, ValidationErrors> result3)
    {
        var errors = ValidationErrors.Empty;
        if (result1.IsFailure) errors = errors.Merge(result1.Error);
        if (result2.IsFailure) errors = errors.Merge(result2.Error);
        if (result3.IsFailure) errors = errors.Merge(result3.Error);

        return errors.HasErrors
            ? Result<(T1, T2, T3), ValidationErrors>.Failure(errors)
            : Result<(T1, T2, T3), ValidationErrors>.Success((result1.Value, result2.Value, result3.Value));
    }

    /// <summary>
    /// Combines four Results, collecting ALL errors from all failed results.
    /// Use this when you want to show all validation errors at once instead of failing fast.
    /// </summary>
    public static Result<(T1, T2, T3, T4), ValidationErrors> CombineAll<T1, T2, T3, T4>(
        Result<T1, ValidationErrors> result1,
        Result<T2, ValidationErrors> result2,
        Result<T3, ValidationErrors> result3,
        Result<T4, ValidationErrors> result4)
    {
        var errors = ValidationErrors.Empty;
        if (result1.IsFailure) errors = errors.Merge(result1.Error);
        if (result2.IsFailure) errors = errors.Merge(result2.Error);
        if (result3.IsFailure) errors = errors.Merge(result3.Error);
        if (result4.IsFailure) errors = errors.Merge(result4.Error);

        return errors.HasErrors
            ? Result<(T1, T2, T3, T4), ValidationErrors>.Failure(errors)
            : Result<(T1, T2, T3, T4), ValidationErrors>.Success((result1.Value, result2.Value, result3.Value, result4.Value));
    }

    /// <summary>
    /// Combines two Results, collecting ALL errors from all failed results.
    /// Use this when you want to show all errors at once instead of failing fast.
    /// </summary>
    public static Result<(T1, T2), ErrorCollection> CombineAll<T1, T2>(
        Result<T1, ErrorCollection> result1,
        Result<T2, ErrorCollection> result2)
    {
        var errors = ErrorCollection.Empty;
        if (result1.IsFailure) errors = errors.Merge(result1.Error);
        if (result2.IsFailure) errors = errors.Merge(result2.Error);

        return errors.HasErrors
            ? Result<(T1, T2), ErrorCollection>.Failure(errors)
            : Result<(T1, T2), ErrorCollection>.Success((result1.Value, result2.Value));
    }

    /// <summary>
    /// Combines three Results, collecting ALL errors from all failed results.
    /// Use this when you want to show all errors at once instead of failing fast.
    /// </summary>
    public static Result<(T1, T2, T3), ErrorCollection> CombineAll<T1, T2, T3>(
        Result<T1, ErrorCollection> result1,
        Result<T2, ErrorCollection> result2,
        Result<T3, ErrorCollection> result3)
    {
        var errors = ErrorCollection.Empty;
        if (result1.IsFailure) errors = errors.Merge(result1.Error);
        if (result2.IsFailure) errors = errors.Merge(result2.Error);
        if (result3.IsFailure) errors = errors.Merge(result3.Error);

        return errors.HasErrors
            ? Result<(T1, T2, T3), ErrorCollection>.Failure(errors)
            : Result<(T1, T2, T3), ErrorCollection>.Success((result1.Value, result2.Value, result3.Value));
    }

    /// <summary>
    /// Combines four Results, collecting ALL errors from all failed results.
    /// Use this when you want to show all errors at once instead of failing fast.
    /// </summary>
    public static Result<(T1, T2, T3, T4), ErrorCollection> CombineAll<T1, T2, T3, T4>(
        Result<T1, ErrorCollection> result1,
        Result<T2, ErrorCollection> result2,
        Result<T3, ErrorCollection> result3,
        Result<T4, ErrorCollection> result4)
    {
        var errors = ErrorCollection.Empty;
        if (result1.IsFailure) errors = errors.Merge(result1.Error);
        if (result2.IsFailure) errors = errors.Merge(result2.Error);
        if (result3.IsFailure) errors = errors.Merge(result3.Error);
        if (result4.IsFailure) errors = errors.Merge(result4.Error);

        return errors.HasErrors
            ? Result<(T1, T2, T3, T4), ErrorCollection>.Failure(errors)
            : Result<(T1, T2, T3, T4), ErrorCollection>.Success((result1.Value, result2.Value, result3.Value, result4.Value));
    }

    public static Result<T, TError> Try<T, TError>(
        Func<T> func,
        Func<Exception, TError> mapException,
        Func<OperationCanceledException, TError>? onOperationCanceled = null)
        where TError : notnull
    {
        try
        {
            return Result<T, TError>.Success(func());
        }
        catch (OperationCanceledException operationCanceledException)
        {
            if (onOperationCanceled is null)
                throw;
            return Result<T, TError>.Failure(onOperationCanceled(operationCanceledException));
        }
        catch (Exception ex)
        {
            return Result<T, TError>.Failure(mapException(ex));
        }
    }

    public static async Task<Result<T, TError>> TryAsync<T, TError>(
        Func<Task<T>> func,
        Func<Exception, TError> mapException,
        Func<OperationCanceledException, TError>? onOperationCanceled = null)
        where TError : notnull
    {
        try
        {
            return Result<T, TError>.Success(await func().ConfigureAwait(false));
        }
        catch (OperationCanceledException operationCanceledException)
        {
            if (onOperationCanceled is null)
                throw;
            return Result<T, TError>.Failure(onOperationCanceled(operationCanceledException));
        }
        catch (Exception ex)
        {
            return Result<T, TError>.Failure(mapException(ex));
        }
    }
}