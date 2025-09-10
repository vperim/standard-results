namespace StandardResults;

public static class Result
{
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