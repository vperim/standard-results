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

    /// <summary>
    /// Awaits the Result task and returns the Result if successful, otherwise returns a new successful Result with the specified fallback value.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="fallbackValue">The value to use if the Result is a failure.</param>
    /// <returns>A task containing the Result if successful; otherwise, a successful Result containing the fallback value.</returns>
    public static async Task<Result<T, TError>> Or<T, TError>(
        this Task<Result<T, TError>> resultTask, T fallbackValue)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Or(fallbackValue);
    }

    /// <summary>
    /// Awaits the Result task and returns the Result if successful, otherwise returns a new successful Result with a value produced by the fallback factory.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="fallbackFactory">A function that produces the fallback value from the error. Only invoked if the Result is a failure.</param>
    /// <returns>A task containing the Result if successful; otherwise, a successful Result containing the factory-produced value.</returns>
    public static async Task<Result<T, TError>> Or<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<TError, T> fallbackFactory)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Or(fallbackFactory);
    }

    /// <summary>
    /// Awaits the Result task and returns the Result if successful, otherwise returns a new successful Result with a value produced by the async fallback factory.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="fallbackFactory">An async function that produces the fallback value from the error. Only invoked if the Result is a failure.</param>
    /// <returns>A task containing the Result if successful; otherwise, a successful Result containing the factory-produced value.</returns>
    public static async Task<Result<T, TError>> OrAsync<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<TError, Task<T>> fallbackFactory)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.OrAsync(fallbackFactory).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and returns the Result if successful, otherwise returns the specified fallback Result.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="fallback">The Result to return if the awaited Result is a failure.</param>
    /// <returns>A task containing the Result if successful; otherwise, the fallback Result.</returns>
    public static async Task<Result<T, TError>> OrElse<T, TError>(
        this Task<Result<T, TError>> resultTask, Result<T, TError> fallback)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.OrElse(fallback);
    }

    /// <summary>
    /// Awaits the Result task and returns the Result if successful, otherwise returns a Result produced by the fallback factory.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="fallbackFactory">A function that produces the fallback Result from the error. Only invoked if the Result is a failure.</param>
    /// <returns>A task containing the Result if successful; otherwise, the factory-produced Result.</returns>
    public static async Task<Result<T, TError>> OrElse<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<TError, Result<T, TError>> fallbackFactory)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.OrElse(fallbackFactory);
    }

    /// <summary>
    /// Awaits the Result task and returns the Result if successful, otherwise returns a Result produced by the async fallback factory.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="fallbackFactory">An async function that produces the fallback Result from the error. Only invoked if the Result is a failure.</param>
    /// <returns>A task containing the Result if successful; otherwise, the factory-produced Result.</returns>
    public static async Task<Result<T, TError>> OrElseAsync<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<TError, Task<Result<T, TError>>> fallbackFactory)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.OrElseAsync(fallbackFactory).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and transforms the success value using the specified function.
    /// </summary>
    /// <typeparam name="T1">The type of the input Result's value.</typeparam>
    /// <typeparam name="T2">The type of the transformed value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="map">Function to transform the success value.</param>
    /// <returns>A task containing a Result with the transformed value or the original error.</returns>
    public static async Task<Result<T2, TError>> Map<T1, T2, TError>(
        this Task<Result<T1, TError>> resultTask, Func<T1, T2> map)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Map(map);
    }

    /// <summary>
    /// Awaits the Result task and asynchronously transforms the success value.
    /// </summary>
    /// <typeparam name="T1">The type of the input Result's value.</typeparam>
    /// <typeparam name="T2">The type of the transformed value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="map">Async function to transform the success value.</param>
    /// <returns>A task containing a Result with the transformed value or the original error.</returns>
    public static async Task<Result<T2, TError>> MapAsync<T1, T2, TError>(
        this Task<Result<T1, TError>> resultTask, Func<T1, Task<T2>> map)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MapAsync(map).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and chains another Result-returning operation.
    /// </summary>
    /// <typeparam name="T1">The type of the input Result's value.</typeparam>
    /// <typeparam name="T2">The type of the value in the returned Result.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="bind">Function that takes the success value and returns a new Result.</param>
    /// <returns>A task containing the Result from the bind function or a failed Result with the original error.</returns>
    public static async Task<Result<T2, TError>> Bind<T1, T2, TError>(
        this Task<Result<T1, TError>> resultTask, Func<T1, Result<T2, TError>> bind)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Bind(bind);
    }

    /// <summary>
    /// Awaits the Result task and chains another async Result-returning operation.
    /// </summary>
    /// <typeparam name="T1">The type of the input Result's value.</typeparam>
    /// <typeparam name="T2">The type of the value in the returned Result.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="bind">Async function that takes the success value and returns a new Result.</param>
    /// <returns>A task containing the Result from the bind function or a failed Result with the original error.</returns>
    public static async Task<Result<T2, TError>> BindAsync<T1, T2, TError>(
        this Task<Result<T1, TError>> resultTask, Func<T1, Task<Result<T2, TError>>> bind)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(bind).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and transforms the error using the specified function.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the input error.</typeparam>
    /// <typeparam name="TNewError">The type of the transformed error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="map">Function to transform the error.</param>
    /// <returns>A task containing a Result with the same value or the transformed error.</returns>
    public static async Task<Result<T, TNewError>> MapError<T, TError, TNewError>(
        this Task<Result<T, TError>> resultTask, Func<TError, TNewError> map)
        where TError : notnull
        where TNewError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.MapError(map);
    }

    /// <summary>
    /// Awaits the Result task and asynchronously transforms the error.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the input error.</typeparam>
    /// <typeparam name="TNewError">The type of the transformed error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="map">Async function to transform the error.</param>
    /// <returns>A task containing a Result with the same value or the transformed error.</returns>
    public static async Task<Result<T, TNewError>> MapErrorAsync<T, TError, TNewError>(
        this Task<Result<T, TError>> resultTask, Func<TError, Task<TNewError>> map)
        where TError : notnull
        where TNewError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MapErrorAsync(map).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and executes a side effect on success.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="action">Action to execute on the success value.</param>
    /// <returns>A task containing the original Result unchanged.</returns>
    public static async Task<Result<T, TError>> Tap<T, TError>(
        this Task<Result<T, TError>> resultTask, Action<T> action)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Tap(action);
    }

    /// <summary>
    /// Awaits the Result task and executes an async side effect on success.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="action">Async action to execute on the success value.</param>
    /// <returns>A task containing the original Result unchanged.</returns>
    public static async Task<Result<T, TError>> TapAsync<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<T, Task> action)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.TapAsync(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and executes a side effect on failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="action">Action to execute on the error.</param>
    /// <returns>A task containing the original Result unchanged.</returns>
    public static async Task<Result<T, TError>> TapError<T, TError>(
        this Task<Result<T, TError>> resultTask, Action<TError> action)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.TapError(action);
    }

    /// <summary>
    /// Awaits the Result task and executes an async side effect on failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="action">Async action to execute on the error.</param>
    /// <returns>A task containing the original Result unchanged.</returns>
    public static async Task<Result<T, TError>> TapErrorAsync<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<TError, Task> action)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.TapErrorAsync(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and ensures the success value satisfies a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="predicate">Predicate that the success value must satisfy.</param>
    /// <param name="errorFactory">Factory that produces an error if the predicate fails. Receives the value that failed the predicate.</param>
    /// <returns>A task containing the original Result if the predicate passes; otherwise, a failed Result.</returns>
    public static async Task<Result<T, TError>> Ensure<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<T, bool> predicate, Func<T, TError> errorFactory)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, errorFactory);
    }

    /// <summary>
    /// Awaits the Result task and ensures the success value satisfies a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="predicate">Predicate that the success value must satisfy.</param>
    /// <param name="errorFactory">Factory that produces an error if the predicate fails.</param>
    /// <returns>A task containing the original Result if the predicate passes; otherwise, a failed Result.</returns>
    public static async Task<Result<T, TError>> Ensure<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<T, bool> predicate, Func<TError> errorFactory)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, errorFactory);
    }

    /// <summary>
    /// Awaits the Result task and asynchronously ensures the success value satisfies a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="predicate">Async predicate that the success value must satisfy.</param>
    /// <param name="errorFactory">Factory that produces an error if the predicate fails.</param>
    /// <returns>A task containing the original Result if the predicate passes; otherwise, a failed Result.</returns>
    public static async Task<Result<T, TError>> EnsureAsync<T, TError>(
        this Task<Result<T, TError>> resultTask, Func<T, Task<bool>> predicate, Func<TError> errorFactory)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.EnsureAsync(predicate, errorFactory).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and matches the state, returning a value.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <typeparam name="TResult">The type of the match result.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="onSuccess">Function to execute if the Result is successful.</param>
    /// <param name="onFailure">Function to execute if the Result is a failure.</param>
    /// <returns>A task containing the result of the matching function.</returns>
    public static async Task<TResult> Match<T, TError, TResult>(
        this Task<Result<T, TError>> resultTask,
        Func<T, TResult> onSuccess,
        Func<TError, TResult> onFailure)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Match(onSuccess, onFailure);
    }

    /// <summary>
    /// Awaits the Result task and matches the state, executing an action.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="onSuccess">Action to execute if the Result is successful.</param>
    /// <param name="onFailure">Action to execute if the Result is a failure.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Match<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Action<T> onSuccess,
        Action<TError> onFailure)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        result.Match(onSuccess, onFailure);
    }

    /// <summary>
    /// Awaits the Result task and asynchronously matches the state, returning a value.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <typeparam name="TResult">The type of the match result.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="onSuccess">Async function to execute if the Result is successful.</param>
    /// <param name="onFailure">Async function to execute if the Result is a failure.</param>
    /// <returns>A task containing the result of the matching function.</returns>
    public static async Task<TResult> MatchAsync<T, TError, TResult>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task<TResult>> onSuccess,
        Func<TError, Task<TResult>> onFailure)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the Result task and asynchronously matches the state, executing an action.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTask">The task containing the Result.</param>
    /// <param name="onSuccess">Async action to execute if the Result is successful.</param>
    /// <param name="onFailure">Async action to execute if the Result is a failure.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task MatchAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task> onSuccess,
        Func<TError, Task> onFailure)
        where TError : notnull
    {
        var result = await resultTask.ConfigureAwait(false);
        await result.MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
    }
}