using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable ParameterHidesMember

namespace StandardResults;

/// <summary>
/// Represents the result of an operation that can either succeed with a value of type <typeparamref name="T"/> 
/// or fail with an error of type <typeparamref name="TError"/>.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error. Must be a non-null reference type.</typeparam>
/// <remarks>
/// <para>
/// This is a struct type that provides functional programming patterns for error handling without exceptions.
/// Results can be in one of three states:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Successful:</strong> Contains a value of type <typeparamref name="T"/></description></item>
/// <item><description><strong>Failed:</strong> Contains an error of type <typeparamref name="TError"/></description></item>
/// <item><description><strong>Uninitialized:</strong> Default/uninitialized state that will throw <see cref="InvalidOperationException"/> when accessed</description></item>
/// </list>
/// <para>
/// <strong>Important:</strong> Always ensure Results are properly initialized using <see cref="Success(T)"/> or <see cref="Failure(TError)"/> 
/// factory methods. Accessing properties or methods on an uninitialized Result will throw <see cref="InvalidOperationException"/>.
/// Use <see cref="IsDefault"/> to check for uninitialized state if needed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create successful result
/// var success = Result&lt;int, string&gt;.Success(42);
/// 
/// // Create failed result
/// var failure = Result&lt;int, string&gt;.Failure("Something went wrong");
/// 
/// // Safe pattern matching
/// var message = result.Match(
///     onSuccess: value => $"Got value: {value}",
///     onFailure: error => $"Got error: {error}"
/// );
/// 
/// // Functional composition
/// var doubled = result
///     .Map(x => x * 2)
///     .Bind(x => x > 100 ? Result&lt;int, string&gt;.Failure("Too large") : Result&lt;int, string&gt;.Success(x));
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Result<T, TError> : IEquatable<Result<T, TError>>
    where TError : notnull
{
    private readonly bool initialized;
    private readonly bool isSuccess;
    private readonly T? value;
    private readonly TError? error;

    private Result(bool ok, T? value, TError? error)
    {
        this.isSuccess = ok;
        this.value = value;
        this.error = error;
        this.initialized = true;
    }

    /// <summary>
    /// Gets a value indicating whether this Result is uninitialized (default value).
    /// </summary>
    /// <remarks>
    /// When true, accessing other properties or methods will throw <see cref="InvalidOperationException"/>.
    /// Always check this property before using a Result that might be uninitialized.
    /// </remarks>
    public bool IsDefault => !this.initialized;

    private void ThrowIfResultIsUninitialized()
    {
        if (!this.initialized)
            throw new InvalidOperationException("Result is default (uninitialized).");
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    [MemberNotNullWhen(true, nameof(value))]
    public bool IsSuccess
    {
        get
        {
            this.ThrowIfResultIsUninitialized();
            return this.isSuccess;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    [MemberNotNullWhen(true, nameof(error))]
    public bool IsFailure
    {
        get
        {
            this.ThrowIfResultIsUninitialized();
            return !this.isSuccess;
        }
    }

    /// <summary>
    /// Gets the success value.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value) or when accessing the value of a failed Result.
    /// </exception>
    public T Value =>
        this.IsSuccess
            ? this.value!
            : throw new InvalidOperationException(this.IsDefault
                ? "Result is default (uninitialized)."
                : "No value on failure");

    /// <summary>
    /// Gets the failure error.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value) or when accessing the error of a successful Result.
    /// </exception>
    public TError Error =>
        this.IsFailure
            ? this.error!
            : throw new InvalidOperationException(this.IsDefault
                ? "Result is default (uninitialized)."
                : "No error on success");

    /// <summary>
    /// Gets the success value or returns the specified default value if the Result failed.
    /// </summary>
    /// <param name="defaultValue">The value to return if the Result failed.</param>
    /// <returns>The success value or the default value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public T GetValueOrDefault(T defaultValue) => this.IsSuccess ? this.value! : defaultValue;

    /// <summary>
    /// Gets the failure error or returns the specified default error if the Result succeeded.
    /// </summary>
    /// <param name="defaultError">The error to return if the Result succeeded.</param>
    /// <returns>The failure error or the default error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public TError GetErrorOrDefault(TError defaultError) => this.IsFailure ? this.error! : defaultError;

    /// <summary>
    /// Creates a successful Result with the specified value.
    /// </summary>
    /// <param name="v">The success value.</param>
    /// <returns>A successful Result containing the value.</returns>
    public static Result<T, TError> Success(T v) => new(true, v, default);

    /// <summary>
    /// Creates a failed Result with the specified error.
    /// </summary>
    /// <param name="e">The failure error.</param>
    /// <returns>A failed Result containing the error.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="e"/> is null.
    /// </exception>
    public static Result<T, TError> Failure(TError e)
        => e is null ? throw new ArgumentNullException(nameof(e)) : new Result<T, TError>(false, default, e);

    /// <summary>
    /// Matches the Result state and executes the appropriate function, returning the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the match functions.</typeparam>
    /// <param name="onSuccess">Function to execute if the Result is successful.</param>
    /// <param name="onFailure">Function to execute if the Result is failed.</param>
    /// <returns>The result of the executed function.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure) 
        =>
            this.IsSuccess ? onSuccess(this.value!) : onFailure(this.Error);

    /// <summary>
    /// Matches the Result state and executes the appropriate action.
    /// </summary>
    /// <param name="onSuccess">Action to execute if the Result is successful.</param>
    /// <param name="onFailure">Action to execute if the Result is failed.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public void Match(Action<T> onSuccess, Action<TError> onFailure)
    {
        if (this.IsSuccess) onSuccess(this.value!);
        else onFailure(this.Error);
    }

    /// <summary>
    /// Transforms the success value using the specified function if the Result is successful, otherwise returns a failed Result with the same error.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="map">Function to transform the success value.</param>
    /// <returns>A Result with the transformed value or the original error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Result<TOut, TError> Map<TOut>(Func<T, TOut> map)
        =>
            this.IsSuccess ? Result<TOut, TError>.Success(map(this.value!)) : Result<TOut, TError>.Failure(this.Error);

    /// <summary>
    /// Transforms the error using the specified function if the Result is failed, otherwise returns a successful Result with the same value.
    /// </summary>
    /// <typeparam name="TErrorOut">The type of the transformed error.</typeparam>
    /// <param name="map">Function to transform the error.</param>
    /// <returns>A Result with the same value or the transformed error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Result<T, TErrorOut> MapError<TErrorOut>(Func<TError, TErrorOut> map)
        where TErrorOut : notnull
        =>
            this.IsFailure ? Result<T, TErrorOut>.Failure(map(this.error!)) : Result<T, TErrorOut>.Success(this.Value);

    /// <summary>
    /// Chains another Result-returning operation if the current Result is successful, otherwise returns a failed Result with the same error.
    /// </summary>
    /// <typeparam name="TOut">The type of the value in the returned Result.</typeparam>
    /// <param name="bind">Function that takes the success value and returns a new Result.</param>
    /// <returns>The Result from the bind function or a failed Result with the original error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Result<TOut, TError> Bind<TOut>(Func<T, Result<TOut, TError>> bind)
        =>
            this.IsSuccess ? bind(this.value!) : Result<TOut, TError>.Failure(this.Error);

    /// <summary>
    /// Attempts to get the success value.
    /// </summary>
    /// <param name="v">When this method returns, contains the success value if the Result is successful; otherwise, the default value.</param>
    /// <returns>true if the Result is successful; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public bool TryGetValue([NotNullWhen(true)] out T v)
    {
        if (this.IsSuccess) { v = this.value!; return true; }
        v = default!; return false;
    }
    
    /// <summary>
    /// Attempts to get the failure error.
    /// </summary>
    /// <param name="e">When this method returns, contains the failure error if the Result is failed; otherwise, the default value.</param>
    /// <returns>true if the Result is failed; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public bool TryGetError([NotNullWhen(true)] out TError e)
    {
        if (this.IsFailure) { e = this.error!; return true; }
        e = default!; return false;
    }

    /// <summary>
    /// Deconstructs the Result into its components.
    /// </summary>
    /// <param name="isSuccess">true if the Result is successful; otherwise, false.</param>
    /// <param name="value">The success value if successful; otherwise, the default value.</param>
    /// <param name="error">The failure error if failed; otherwise, the default value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public void Deconstruct(out bool isSuccess, out T? value, out TError? error)
        => (isSuccess, value, error) = (this.IsSuccess, this.value, this.error);

    /// <summary>
    /// Asynchronously matches the Result state and executes the appropriate function, returning the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the match functions.</typeparam>
    /// <param name="onSuccess">Async function to execute if the Result is successful.</param>
    /// <param name="onFailure">Async function to execute if the Result is failed.</param>
    /// <returns>A task that represents the asynchronous operation and contains the result of the executed function.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<TError, Task<TResult>> onFailure)
        =>
            this.IsSuccess
            ? onSuccess(this.value!)
            : onFailure(this.Error);

    /// <summary>
    /// Asynchronously matches the Result state and executes the appropriate action.
    /// </summary>
    /// <param name="onSuccess">Async action to execute if the Result is successful.</param>
    /// <param name="onFailure">Async action to execute if the Result is failed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Task MatchAsync(
        Func<T, Task> onSuccess,
        Func<TError, Task> onFailure)
        =>
            this.IsSuccess
            ? onSuccess(this.value!)
            : onFailure(this.Error);

    /// <summary>
    /// Asynchronously transforms the success value using the specified function if the Result is successful, otherwise returns a failed Result with the same error.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="map">Async function to transform the success value.</param>
    /// <returns>A task that represents the asynchronous operation and contains a Result with the transformed value or the original error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public async Task<Result<TOut, TError>> MapAsync<TOut>(
        Func<T, Task<TOut>> map)
        =>
            this.IsSuccess
            ? Result<TOut, TError>.Success(await map(this.value!).ConfigureAwait(false))
            : Result<TOut, TError>.Failure(this.Error);

    /// <summary>
    /// Asynchronously transforms the error using the specified function if the Result is failed, otherwise returns a successful Result with the same value.
    /// </summary>
    /// <typeparam name="TErrorOut">The type of the transformed error.</typeparam>
    /// <param name="map">Async function to transform the error.</param>
    /// <returns>A task that represents the asynchronous operation and contains a Result with the same value or the transformed error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public async Task<Result<T, TErrorOut>> MapErrorAsync<TErrorOut>(
        Func<TError, Task<TErrorOut>> map)
        where TErrorOut : notnull
        =>
            this.IsFailure
            ? Result<T, TErrorOut>.Failure(await map(this.error!).ConfigureAwait(false))
            : Result<T, TErrorOut>.Success(this.Value);

    /// <summary>
    /// Asynchronously chains another Result-returning operation if the current Result is successful, otherwise returns a failed Result with the same error.
    /// </summary>
    /// <typeparam name="TOut">The type of the value in the returned Result.</typeparam>
    /// <param name="bind">Async function that takes the success value and returns a new Result.</param>
    /// <returns>A task that represents the asynchronous operation and contains the Result from the bind function or a failed Result with the original error.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public async Task<Result<TOut, TError>> BindAsync<TOut>(
        Func<T, Task<Result<TOut, TError>>> bind)
        =>
            this.IsSuccess
            ? await bind(this.value!).ConfigureAwait(false)
            : Result<TOut, TError>.Failure(this.Error);

    /// <summary>
    /// Executes a side effect action on the success value if the Result is successful, then returns the original Result.
    /// </summary>
    /// <param name="action">Action to execute on the success value.</param>
    /// <returns>The original Result unchanged.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    /// <example>
    /// <code>
    /// var result = GetUser(userId)
    ///     .Tap(user => logger.LogInformation("Found user: {Name}", user.Name))
    ///     .Tap(user => metrics.Increment("users.retrieved"));
    /// </code>
    /// </example>
    public Result<T, TError> Tap(Action<T> action)
    {
        if (this.IsSuccess)
            action(this.value!);
        return this;
    }

    /// <summary>
    /// Executes a side effect action on the failure error if the Result is failed, then returns the original Result.
    /// </summary>
    /// <param name="action">Action to execute on the failure error.</param>
    /// <returns>The original Result unchanged.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    /// <example>
    /// <code>
    /// var result = GetUser(userId)
    ///     .TapError(error => logger.LogWarning("User lookup failed: {Error}", error))
    ///     .TapError(error => metrics.Increment("users.lookup_failed"));
    /// </code>
    /// </example>
    public Result<T, TError> TapError(Action<TError> action)
    {
        if (this.IsFailure)
            action(this.error!);
        return this;
    }

    /// <summary>
    /// Asynchronously executes a side effect action on the success value if the Result is successful, then returns the original Result.
    /// </summary>
    /// <param name="onSuccess">Async action to execute on the success value.</param>
    /// <returns>A task that represents the asynchronous operation and contains the original Result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public async Task<Result<T, TError>> TapAsync(
        Func<T, Task> onSuccess)
    {
        if (this.IsSuccess) await onSuccess(this.value!).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously executes a side effect action on the failure error if the Result is failed, then returns the original Result.
    /// </summary>
    /// <param name="onFailure">Async action to execute on the failure error.</param>
    /// <returns>A task that represents the asynchronous operation and contains the original Result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public async Task<Result<T, TError>> TapErrorAsync(
        Func<TError, Task> onFailure)
    {
        if (this.IsFailure) await onFailure(this.error!).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Ensures that the success value satisfies a predicate, otherwise converts the Result to a failure.
    /// </summary>
    /// <param name="predicate">Predicate to test the success value.</param>
    /// <param name="errorFactory">Function to create an error from the value if the predicate fails.</param>
    /// <returns>The original Result if successful and the predicate passes, or a failed Result if the predicate fails.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    /// <example>
    /// <code>
    /// var result = GetUser(userId)
    ///     .Ensure(user => user.IsActive, user => Error.Permanent("inactive", $"User {user.Id} is inactive"))
    ///     .Ensure(user => user.EmailVerified, _ => Error.Permanent("unverified", "Email not verified"));
    /// </code>
    /// </example>
    public Result<T, TError> Ensure(
        Func<T, bool> predicate,
        Func<T, TError> errorFactory)
    {
        if (this.IsFailure)
            return this;

        return predicate(this.value!)
            ? this
            : Failure(errorFactory(this.value!));
    }

    /// <summary>
    /// Ensures that the success value satisfies a predicate, otherwise converts the Result to a failure.
    /// </summary>
    /// <param name="predicate">Predicate to test the success value.</param>
    /// <param name="errorFactory">Function to create an error if the predicate fails.</param>
    /// <returns>The original Result if successful and the predicate passes, or a failed Result if the predicate fails.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Result<T, TError> Ensure(
        Func<T, bool> predicate,
        Func<TError> errorFactory)
    {
        if (this.IsFailure)
            return this;

        return predicate(this.value!)
            ? this
            : Failure(errorFactory());
    }

    /// <summary>
    /// Asynchronously ensures that the success value satisfies a predicate, otherwise converts the Result to a failure.
    /// </summary>
    /// <param name="predicate">Async predicate to test the success value.</param>
    /// <param name="errorFactory">Function to create an error if the predicate fails.</param>
    /// <returns>A task that represents the asynchronous operation and contains the original Result if the predicate succeeds, or a failed Result if it fails.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public async Task<Result<T, TError>> EnsureAsync(
        Func<T, Task<bool>> predicate,
        Func<TError> errorFactory)
    {
        if (this.IsFailure)
            return this;

        return await predicate(this.value!).ConfigureAwait(false)
            ? this
            : Failure(errorFactory());
    }

    /// <summary>
    /// Returns the current Result if successful, otherwise returns a new successful Result with the specified fallback value.
    /// </summary>
    /// <param name="fallbackValue">The value to use if the current Result is a failure.</param>
    /// <returns>The current Result if successful; otherwise, a successful Result containing the fallback value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Result<T, TError> Or(T fallbackValue)
        => this.IsSuccess ? this : Success(fallbackValue);

    /// <summary>
    /// Returns the current Result if successful, otherwise returns a new successful Result with a value produced by the fallback factory.
    /// </summary>
    /// <param name="fallbackFactory">A function that produces the fallback value from the error. Only invoked if the Result is a failure.</param>
    /// <returns>The current Result if successful; otherwise, a successful Result containing the factory-produced value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Result<T, TError> Or(Func<TError, T> fallbackFactory)
        => this.IsSuccess ? this : Success(fallbackFactory(this.error!));

    /// <summary>
    /// Returns the current Result if successful, otherwise returns the specified fallback Result.
    /// </summary>
    /// <param name="fallback">The Result to return if the current Result is a failure.</param>
    /// <returns>The current Result if successful; otherwise, the fallback Result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Result<T, TError> OrElse(Result<T, TError> fallback)
        => this.IsSuccess ? this : fallback;

    /// <summary>
    /// Returns the current Result if successful, otherwise returns a Result produced by the fallback factory.
    /// </summary>
    /// <param name="fallbackFactory">A function that produces the fallback Result from the error. Only invoked if the Result is a failure.</param>
    /// <returns>The current Result if successful; otherwise, the factory-produced Result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public Result<T, TError> OrElse(Func<TError, Result<T, TError>> fallbackFactory)
        => this.IsSuccess ? this : fallbackFactory(this.error!);

    /// <summary>
    /// Asynchronously returns the current Result if successful, otherwise returns a new successful Result with a value produced by the async fallback factory.
    /// </summary>
    /// <param name="fallbackFactory">An async function that produces the fallback value from the error. Only invoked if the Result is a failure.</param>
    /// <returns>A task containing the current Result if successful; otherwise, a successful Result containing the factory-produced value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public async Task<Result<T, TError>> OrAsync(Func<TError, Task<T>> fallbackFactory)
        => this.IsSuccess ? this : Success(await fallbackFactory(this.error!).ConfigureAwait(false));

    /// <summary>
    /// Asynchronously returns the current Result if successful, otherwise returns a Result produced by the async fallback factory.
    /// </summary>
    /// <param name="fallbackFactory">An async function that produces the fallback Result from the error. Only invoked if the Result is a failure.</param>
    /// <returns>A task containing the current Result if successful; otherwise, the factory-produced Result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value).
    /// </exception>
    public async Task<Result<T, TError>> OrElseAsync(Func<TError, Task<Result<T, TError>>> fallbackFactory)
        => this.IsSuccess ? this : await fallbackFactory(this.error!).ConfigureAwait(false);

    /// <summary>
    /// Determines whether the specified Result is equal to the current Result.
    /// </summary>
    /// <param name="other">The Result to compare with the current Result.</param>
    /// <returns>true if the specified Result is equal to the current Result; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value) and accessed during comparison.
    /// </exception>
    public bool Equals(Result<T, TError> other)
    {
        // Handle default(Result<,>) safely (both uninitialized -> equal)
        if (this.initialized != other.initialized) return false;
        if (!this.initialized) return true;

        // Discriminators must match
        if (this.IsSuccess != other.IsSuccess) return false;

        // Safe: Value/Error are non-null by contract
        return this.IsSuccess
            ? EqualityComparer<T>.Default.Equals(this.Value, other.Value)
            : EqualityComparer<TError>.Default.Equals(this.Error, other.Error);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Result.
    /// </summary>
    /// <param name="obj">The object to compare with the current Result.</param>
    /// <returns>true if the specified object is equal to the current Result; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value) and accessed during comparison.
    /// </exception>
    public override bool Equals(object? obj) => obj is Result<T, TError> other && this.Equals(other);

    /// <summary>
    /// Returns the hash code for the current Result.
    /// </summary>
    /// <returns>A hash code for the current Result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Result is uninitialized (default value) and accessed during hash code calculation.
    /// </exception>
    public override int GetHashCode()
    {
        unchecked
        {
            // Make default(Result<,>) safe and stable
            if (!this.initialized) return 0;

            var hash = 17;
            hash = hash * 31 + (this.IsSuccess ? 1 : 0);
            if (this.IsSuccess)
                hash = hash * 31 + EqualityComparer<T>.Default.GetHashCode(this.value!);
            else
                hash = hash * 31 + EqualityComparer<TError>.Default.GetHashCode(this.error!);
            return hash;
        }
    }


    /// <summary>
    /// Determines whether two Results are equal.
    /// </summary>
    /// <param name="left">The first Result to compare.</param>
    /// <param name="right">The second Result to compare.</param>
    /// <returns>true if the Results are equal; otherwise, false.</returns>
    public static bool operator ==(Result<T, TError> left, Result<T, TError> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two Results are not equal.
    /// </summary>
    /// <param name="left">The first Result to compare.</param>
    /// <param name="right">The second Result to compare.</param>
    /// <returns>true if the Results are not equal; otherwise, false.</returns>
    public static bool operator !=(Result<T, TError> left, Result<T, TError> right) => !left.Equals(right);

    public static implicit operator Result<T, TError>(TError error) => Failure(error);
    public static implicit operator Result<T, TError>(T success) => Success(success);

    /// <summary>
    /// Returns a string representation of the current Result.
    /// </summary>
    /// <returns>A string representation of the current Result.</returns>
    public override string ToString()
        =>
            this.IsDefault
           ? "Result<default>"
           : this.IsSuccess ? $"Success({this.value})" : $"Failure({this.error})";
}
