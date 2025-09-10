namespace StandardResults;

public static class ResultExtensions
{
    public static Result<T2, TError> Map<T1, T2, TError>(
        this Result<T1, TError> result, Func<T1, T2> map)
        where TError : notnull
        => result.IsSuccess
            ? Result<T2, TError>.Success(map(result.Value))
            : Result<T2, TError>.Failure(result.Error);

    public static Result<T2, TError> Bind<T1, T2, TError>(
        this Result<T1, TError> result, Func<T1, Result<T2, TError>> bind)
        where TError : notnull
        => result.IsSuccess ? bind(result.Value) : Result<T2, TError>.Failure(result.Error);

    public static Result<T, TNewError> MapError<T, TError, TNewError>(
        this Result<T, TError> result, Func<TError, TNewError> map)
        where TError : notnull where TNewError : notnull
        => result.IsSuccess ? Result<T, TNewError>.Success(result.Value)
            : Result<T, TNewError>.Failure(map(result.Error));

    public static async Task<Result<T2, TError>> MapAsync<T1, T2, TError>(
        this Result<T1, TError> result, Func<T1, Task<T2>> map)
        where TError : notnull
        => result.IsSuccess
            ? Result<T2, TError>.Success(await map(result.Value).ConfigureAwait(false))
            : Result<T2, TError>.Failure(result.Error);

    public static async Task<Result<T2, TError>> BindAsync<T1, T2, TError>(
        this Result<T1, TError> result, Func<T1, Task<Result<T2, TError>>> bind)
        where TError : notnull
        => result.IsSuccess
            ? await bind(result.Value).ConfigureAwait(false)
            : Result<T2, TError>.Failure(result.Error);
}