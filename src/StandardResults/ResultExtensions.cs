namespace StandardResults;

public static class ResultExtensions
{
    /// <summary>
    /// Transforms the success value using the specified function if the Result is successful, otherwise returns a failed Result with the same error.
    /// </summary>
    /// <typeparam name="T1">The type of the input Result's value.</typeparam>
    /// <typeparam name="T2">The type of the transformed value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="result">The Result to transform.</param>
    /// <param name="map">Function to transform the success value.</param>
    /// <returns>A Result with the transformed value or the original error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public static Result<T2, TError> Map<T1, T2, TError>(
        this Result<T1, TError> result, Func<T1, T2> map)
        where TError : notnull
        => result.IsSuccess
            ? Result<T2, TError>.Success(map(result.Value))
            : Result<T2, TError>.Failure(result.Error);

    /// <summary>
    /// Chains another Result-returning operation if the current Result is successful, otherwise returns a failed Result with the same error.
    /// </summary>
    /// <typeparam name="T1">The type of the input Result's value.</typeparam>
    /// <typeparam name="T2">The type of the value in the returned Result.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="result">The Result to chain from.</param>
    /// <param name="bind">Function that takes the success value and returns a new Result.</param>
    /// <returns>The Result from the bind function or a failed Result with the original error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public static Result<T2, TError> Bind<T1, T2, TError>(
        this Result<T1, TError> result, Func<T1, Result<T2, TError>> bind)
        where TError : notnull
        => result.IsSuccess ? bind(result.Value) : Result<T2, TError>.Failure(result.Error);

    /// <summary>
    /// Transforms the error using the specified function if the Result is failed, otherwise returns a successful Result with the same value.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the input error.</typeparam>
    /// <typeparam name="TNewError">The type of the transformed error.</typeparam>
    /// <param name="result">The Result to transform.</param>
    /// <param name="map">Function to transform the error.</param>
    /// <returns>A Result with the same value or the transformed error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public static Result<T, TNewError> MapError<T, TError, TNewError>(
        this Result<T, TError> result, Func<TError, TNewError> map)
        where TError : notnull where TNewError : notnull
        => result.IsSuccess ? Result<T, TNewError>.Success(result.Value)
            : Result<T, TNewError>.Failure(map(result.Error));

    /// <summary>
    /// Asynchronously transforms the success value using the specified function if the Result is successful, otherwise returns a failed Result with the same error.
    /// </summary>
    /// <typeparam name="T1">The type of the input Result's value.</typeparam>
    /// <typeparam name="T2">The type of the transformed value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="result">The Result to transform.</param>
    /// <param name="map">Async function to transform the success value.</param>
    /// <returns>A task that represents the asynchronous operation and contains a Result with the transformed value or the original error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public static async Task<Result<T2, TError>> MapAsync<T1, T2, TError>(
        this Result<T1, TError> result, Func<T1, Task<T2>> map)
        where TError : notnull
        => result.IsSuccess
            ? Result<T2, TError>.Success(await map(result.Value).ConfigureAwait(false))
            : Result<T2, TError>.Failure(result.Error);

    /// <summary>
    /// Asynchronously chains another Result-returning operation if the current Result is successful, otherwise returns a failed Result with the same error.
    /// </summary>
    /// <typeparam name="T1">The type of the input Result's value.</typeparam>
    /// <typeparam name="T2">The type of the value in the returned Result.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="result">The Result to chain from.</param>
    /// <param name="bind">Async function that takes the success value and returns a new Result.</param>
    /// <returns>A task that represents the asynchronous operation and contains the Result from the bind function or a failed Result with the original error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public static async Task<Result<T2, TError>> BindAsync<T1, T2, TError>(
        this Result<T1, TError> result, Func<T1, Task<Result<T2, TError>>> bind)
        where TError : notnull
        => result.IsSuccess
            ? await bind(result.Value).ConfigureAwait(false)
            : Result<T2, TError>.Failure(result.Error);
}