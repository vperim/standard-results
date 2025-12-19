namespace StandardResults;

/// <summary>
/// Provides extension methods for working with collections of Results.
/// </summary>
public static class ResultCollectionExtensions
{
    /// <summary>
    /// Converts a collection of Results into a Result containing a collection of values.
    /// Fails fast on the first failure encountered.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="results">The collection of Results to sequence.</param>
    /// <returns>A Result containing all success values, or the first error encountered.</returns>
    public static Result<IReadOnlyList<T>, TError> Sequence<T, TError>(
        this IEnumerable<Result<T, TError>> results)
        where TError : notnull
    {
        var list = new List<T>();
        foreach (var result in results)
        {
            if (result.IsFailure)
                return Result<IReadOnlyList<T>, TError>.Failure(result.Error);
            list.Add(result.Value);
        }
        return Result<IReadOnlyList<T>, TError>.Success(list);
    }

    /// <summary>
    /// Converts a collection of Results into a Result containing a collection of values.
    /// Collects ALL errors instead of failing fast.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="results">The collection of Results to sequence.</param>
    /// <returns>A Result containing all success values, or all errors merged together.</returns>
    public static Result<IReadOnlyList<T>, ValidationErrors> SequenceAll<T>(
        this IEnumerable<Result<T, ValidationErrors>> results)
    {
        var list = new List<T>();
        var errors = ValidationErrors.Empty;

        foreach (var result in results)
        {
            if (result.IsFailure)
                errors = errors.Merge(result.Error);
            else
                list.Add(result.Value);
        }

        return errors.HasErrors
            ? Result<IReadOnlyList<T>, ValidationErrors>.Failure(errors)
            : Result<IReadOnlyList<T>, ValidationErrors>.Success(list);
    }

    /// <summary>
    /// Converts a collection of Results into a Result containing a collection of values.
    /// Collects ALL errors instead of failing fast.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="results">The collection of Results to sequence.</param>
    /// <returns>A Result containing all success values, or all errors merged together.</returns>
    public static Result<IReadOnlyList<T>, ErrorCollection> SequenceAll<T>(
        this IEnumerable<Result<T, ErrorCollection>> results)
    {
        var list = new List<T>();
        var errors = ErrorCollection.Empty;

        foreach (var result in results)
        {
            if (result.IsFailure)
                errors = errors.Merge(result.Error);
            else
                list.Add(result.Value);
        }

        return errors.HasErrors
            ? Result<IReadOnlyList<T>, ErrorCollection>.Failure(errors)
            : Result<IReadOnlyList<T>, ErrorCollection>.Success(list);
    }

    /// <summary>
    /// Applies a Result-returning function to each element and sequences the results.
    /// Fails fast on the first failure encountered.
    /// </summary>
    /// <typeparam name="T">The type of the input elements.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="source">The collection of input elements.</param>
    /// <param name="selector">A function that transforms each element into a Result.</param>
    /// <returns>A Result containing all transformed values, or the first error encountered.</returns>
    public static Result<IReadOnlyList<TResult>, TError> Traverse<T, TResult, TError>(
        this IEnumerable<T> source,
        Func<T, Result<TResult, TError>> selector)
        where TError : notnull
    {
        var list = new List<TResult>();
        foreach (var item in source)
        {
            var result = selector(item);
            if (result.IsFailure)
                return Result<IReadOnlyList<TResult>, TError>.Failure(result.Error);
            list.Add(result.Value);
        }
        return Result<IReadOnlyList<TResult>, TError>.Success(list);
    }

    /// <summary>
    /// Applies a Result-returning function to each element and sequences the results.
    /// Collects ALL errors instead of failing fast.
    /// </summary>
    /// <typeparam name="T">The type of the input elements.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="source">The collection of input elements.</param>
    /// <param name="selector">A function that transforms each element into a Result.</param>
    /// <returns>A Result containing all transformed values, or all errors merged together.</returns>
    public static Result<IReadOnlyList<TResult>, ValidationErrors> TraverseAll<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Result<TResult, ValidationErrors>> selector)
    {
        var list = new List<TResult>();
        var errors = ValidationErrors.Empty;

        foreach (var item in source)
        {
            var result = selector(item);
            if (result.IsFailure)
                errors = errors.Merge(result.Error);
            else
                list.Add(result.Value);
        }

        return errors.HasErrors
            ? Result<IReadOnlyList<TResult>, ValidationErrors>.Failure(errors)
            : Result<IReadOnlyList<TResult>, ValidationErrors>.Success(list);
    }

    /// <summary>
    /// Applies a Result-returning function to each element and sequences the results.
    /// Collects ALL errors instead of failing fast.
    /// </summary>
    /// <typeparam name="T">The type of the input elements.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="source">The collection of input elements.</param>
    /// <param name="selector">A function that transforms each element into a Result.</param>
    /// <returns>A Result containing all transformed values, or all errors merged together.</returns>
    public static Result<IReadOnlyList<TResult>, ErrorCollection> TraverseAll<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Result<TResult, ErrorCollection>> selector)
    {
        var list = new List<TResult>();
        var errors = ErrorCollection.Empty;

        foreach (var item in source)
        {
            var result = selector(item);
            if (result.IsFailure)
                errors = errors.Merge(result.Error);
            else
                list.Add(result.Value);
        }

        return errors.HasErrors
            ? Result<IReadOnlyList<TResult>, ErrorCollection>.Failure(errors)
            : Result<IReadOnlyList<TResult>, ErrorCollection>.Success(list);
    }

    /// <summary>
    /// Asynchronously converts a collection of Result tasks into a Result containing a collection of values.
    /// Fails fast on the first failure encountered (sequential evaluation).
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="resultTasks">The collection of Result tasks to sequence.</param>
    /// <returns>A task containing a Result with all success values, or the first error encountered.</returns>
    public static async Task<Result<IReadOnlyList<T>, TError>> SequenceAsync<T, TError>(
        this IEnumerable<Task<Result<T, TError>>> resultTasks)
        where TError : notnull
    {
        var list = new List<T>();
        foreach (var resultTask in resultTasks)
        {
            var result = await resultTask.ConfigureAwait(false);
            if (result.IsFailure)
                return Result<IReadOnlyList<T>, TError>.Failure(result.Error);
            list.Add(result.Value);
        }
        return Result<IReadOnlyList<T>, TError>.Success(list);
    }

    /// <summary>
    /// Asynchronously converts a collection of Result tasks into a Result containing a collection of values.
    /// Awaits all tasks in parallel and collects ALL errors.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="resultTasks">The collection of Result tasks to sequence.</param>
    /// <returns>A task containing a Result with all success values, or all errors merged together.</returns>
    public static async Task<Result<IReadOnlyList<T>, ValidationErrors>> SequenceAllAsync<T>(
        this IEnumerable<Task<Result<T, ValidationErrors>>> resultTasks)
    {
        var results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        return results.SequenceAll();
    }

    /// <summary>
    /// Asynchronously converts a collection of Result tasks into a Result containing a collection of values.
    /// Awaits all tasks in parallel and collects ALL errors.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="resultTasks">The collection of Result tasks to sequence.</param>
    /// <returns>A task containing a Result with all success values, or all errors merged together.</returns>
    public static async Task<Result<IReadOnlyList<T>, ErrorCollection>> SequenceAllAsync<T>(
        this IEnumerable<Task<Result<T, ErrorCollection>>> resultTasks)
    {
        var results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        return results.SequenceAll();
    }

    /// <summary>
    /// Asynchronously applies a Result-returning function to each element and sequences the results.
    /// Fails fast on the first failure encountered (sequential evaluation).
    /// </summary>
    /// <typeparam name="T">The type of the input elements.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="source">The collection of input elements.</param>
    /// <param name="selector">An async function that transforms each element into a Result.</param>
    /// <returns>A task containing a Result with all transformed values, or the first error encountered.</returns>
    public static async Task<Result<IReadOnlyList<TResult>, TError>> TraverseAsync<T, TResult, TError>(
        this IEnumerable<T> source,
        Func<T, Task<Result<TResult, TError>>> selector)
        where TError : notnull
    {
        var list = new List<TResult>();
        foreach (var item in source)
        {
            var result = await selector(item).ConfigureAwait(false);
            if (result.IsFailure)
                return Result<IReadOnlyList<TResult>, TError>.Failure(result.Error);
            list.Add(result.Value);
        }
        return Result<IReadOnlyList<TResult>, TError>.Success(list);
    }

    /// <summary>
    /// Asynchronously applies a Result-returning function to each element and sequences the results.
    /// Executes all operations in parallel and collects ALL errors.
    /// </summary>
    /// <typeparam name="T">The type of the input elements.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="source">The collection of input elements.</param>
    /// <param name="selector">An async function that transforms each element into a Result.</param>
    /// <returns>A task containing a Result with all transformed values, or all errors merged together.</returns>
    public static async Task<Result<IReadOnlyList<TResult>, ValidationErrors>> TraverseAllAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Task<Result<TResult, ValidationErrors>>> selector)
    {
        var tasks = source.Select(selector);
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.SequenceAll();
    }

    /// <summary>
    /// Asynchronously applies a Result-returning function to each element and sequences the results.
    /// Executes all operations in parallel and collects ALL errors.
    /// </summary>
    /// <typeparam name="T">The type of the input elements.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="source">The collection of input elements.</param>
    /// <param name="selector">An async function that transforms each element into a Result.</param>
    /// <returns>A task containing a Result with all transformed values, or all errors merged together.</returns>
    public static async Task<Result<IReadOnlyList<TResult>, ErrorCollection>> TraverseAllAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Task<Result<TResult, ErrorCollection>>> selector)
    {
        var tasks = source.Select(selector);
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.SequenceAll();
    }
}
