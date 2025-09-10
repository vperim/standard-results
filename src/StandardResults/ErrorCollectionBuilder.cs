namespace StandardResults;

/// <summary>
/// Mutable accumulator for ErrorCollection. Use within a scope, then Build() to freeze.
/// </summary>
public sealed class ErrorCollectionBuilder
{
    private readonly List<Error> errors = [];
    private bool isTransient;

    public int Count => errors.Count;
    public bool HasErrors => Count != 0;

    /// <summary>Add a general error.</summary>
    public ErrorCollectionBuilder Add(string code, string message, bool transient = false)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be null or whitespace.", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        errors.Add(transient ? Error.Transient(code, message) : Error.Permanent(code, message));
        isTransient |= transient;
        return this;
    }

    /// <summary>Add an already-constructed Error.</summary>
    public ErrorCollectionBuilder Add(Error error)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));
        errors.Add(error);
        isTransient |= error.IsTransient;
        return this;
    }

    public ErrorCollectionBuilder Merge(ErrorCollection other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (!other.HasErrors) return this;
        this.errors.AddRange(other.Errors);
        isTransient |= other.IsTransient;
        return this;
    }

    /// <summary>Freeze the current contents into an immutable ErrorCollection.</summary>
    public ErrorCollection Build()
    {
        if (errors.Count == 0) return ErrorCollection.Empty;
        var arr = errors.ToArray();
        return new ErrorCollection(arr, isTransient);
    }

    /// <summary>Clear the builder for reuse.</summary>
    public void Clear()
    {
        errors.Clear();
        isTransient = false;
    }
}