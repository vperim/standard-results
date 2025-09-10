using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable ParameterHidesMember

namespace StandardResults;

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
        isSuccess = ok;
        this.value = value;
        this.error = error;
        initialized = true;
    }

    public bool IsDefault => !initialized;

    private void ThrowIfResultIsUninitialized()
    {
        if (!initialized)
            throw new InvalidOperationException("Result is default (uninitialized).");
    }

    [MemberNotNullWhen(true, nameof(value))]
    public bool IsSuccess
    {
        get
        {
            ThrowIfResultIsUninitialized();
            return isSuccess;
        }
    }

    [MemberNotNullWhen(true, nameof(error))]
    public bool IsFailure
    {
        get
        {
            ThrowIfResultIsUninitialized();
            return !isSuccess;
        }
    }

    public T Value =>
        IsSuccess
            ? value!
            : throw new InvalidOperationException(IsDefault
                ? "Result is default (uninitialized)."
                : "No value on failure");

    public TError Error =>
        IsFailure
            ? error!
            : throw new InvalidOperationException(IsDefault
                ? "Result is default (uninitialized)."
                : "No error on success");

    public T GetValueOrDefault(T defaultValue) => IsSuccess ? value! : defaultValue;

    public TError GetErrorOrDefault(TError defaultError) => IsFailure ? error! : defaultError;

    public static Result<T, TError> Success(T v) => new(true, v, default);

    public static Result<T, TError> Failure(TError e)
        => e is null ? throw new ArgumentNullException(nameof(e)) : new Result<T, TError>(false, default, e);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure) 
        => IsSuccess ? onSuccess(value!) : onFailure(Error);

    public void Match(Action<T> onSuccess, Action<TError> onFailure)
    {
        if (IsSuccess) onSuccess(value!);
        else onFailure(Error);
    }

    public Result<TOut, TError> Map<TOut>(Func<T, TOut> map)
        => IsSuccess ? Result<TOut, TError>.Success(map(value!)) : Result<TOut, TError>.Failure(Error);

    public Result<T, TErrorOut> MapError<TErrorOut>(Func<TError, TErrorOut> map)
        where TErrorOut : notnull
        => IsFailure ? Result<T, TErrorOut>.Failure(map(error!)) : Result<T, TErrorOut>.Success(Value);

    public Result<TOut, TError> Bind<TOut>(Func<T, Result<TOut, TError>> bind)
        => IsSuccess ? bind(value!) : Result<TOut, TError>.Failure(Error);

    public bool TryGetValue([NotNullWhen(true)] out T v)
    {
        if (IsSuccess) { v = value!; return true; }
        v = default!; return false;
    }
    
    public bool TryGetError([NotNullWhen(true)] out TError e)
    {
        if (IsFailure) { e = error!; return true; }
        e = default!; return false;
    }

    public void Deconstruct(out bool isSuccess, out T? value, out TError? error)
        => (isSuccess, value, error) = (IsSuccess, this.value, this.error);

    public Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<TError, Task<TResult>> onFailure)
        => IsSuccess
            ? onSuccess(value!)
            : onFailure(Error);

    public Task MatchAsync(
        Func<T, Task> onSuccess,
        Func<TError, Task> onFailure)
        => IsSuccess
            ? onSuccess(value!)
            : onFailure(Error);

    public async Task<Result<TOut, TError>> MapAsync<TOut>(
        Func<T, Task<TOut>> map)
        => IsSuccess
            ? Result<TOut, TError>.Success(await map(value!).ConfigureAwait(false))
            : Result<TOut, TError>.Failure(Error);

    public async Task<Result<T, TErrorOut>> MapErrorAsync<TErrorOut>(
        Func<TError, Task<TErrorOut>> map)
        where TErrorOut : notnull
        => IsFailure
            ? Result<T, TErrorOut>.Failure(await map(error!).ConfigureAwait(false))
            : Result<T, TErrorOut>.Success(Value);

    public async Task<Result<TOut, TError>> BindAsync<TOut>(
        Func<T, Task<Result<TOut, TError>>> bind)
        => IsSuccess
            ? await bind(value!).ConfigureAwait(false)
            : Result<TOut, TError>.Failure(Error);

    public async Task<Result<T, TError>> TapAsync(
        Func<T, Task> onSuccess)
    {
        if (IsSuccess) await onSuccess(value!).ConfigureAwait(false);
        return this;
    }

    public async Task<Result<T, TError>> TapErrorAsync(
        Func<TError, Task> onFailure)
    {
        if (IsFailure) await onFailure(error!).ConfigureAwait(false);
        return this;
    }

    public async Task<Result<T, TError>> EnsureAsync(
        Func<T, Task<bool>> predicate,
        Func<TError> errorFactory)
        => IsSuccess && await predicate(value!).ConfigureAwait(false)
            ? this
            : Failure(errorFactory());

    public bool Equals(Result<T, TError> other)
    {
        // Handle default(Result<,>) safely (both uninitialized -> equal)
        if (initialized != other.initialized) return false;
        if (!initialized) return true;

        // Discriminators must match
        if (IsSuccess != other.IsSuccess) return false;

        // Safe: Value/Error are non-null by contract
        return IsSuccess
            ? EqualityComparer<T>.Default.Equals(Value, other.Value)
            : EqualityComparer<TError>.Default.Equals(Error, other.Error);
    }

    public override bool Equals(object? obj) => obj is Result<T, TError> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            // Make default(Result<,>) safe and stable
            if (!initialized) return 0;

            var hash = 17;
            hash = hash * 31 + (IsSuccess ? 1 : 0);
            if (IsSuccess)
                hash = hash * 31 + EqualityComparer<T>.Default.GetHashCode(value!);
            else
                hash = hash * 31 + EqualityComparer<TError>.Default.GetHashCode(error!);
            return hash;
        }
    }


    public static bool operator ==(Result<T, TError> left, Result<T, TError> right) => left.Equals(right);
    public static bool operator !=(Result<T, TError> left, Result<T, TError> right) => !left.Equals(right);

    public override string ToString()
        => IsDefault
           ? "Result<default>"
           : IsSuccess ? $"Success({value})" : $"Failure({error})";
}