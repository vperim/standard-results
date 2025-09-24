namespace StandardResults;

/// <summary>
/// Mutable accumulator for ErrorCollection. Use within a scope, then Build() to freeze.
/// </summary>
public sealed class ErrorCollectionBuilder
{
    private readonly List<Error> errors = [];
    private bool isTransient;

    public int Count => this.errors.Count;
    public bool HasErrors => this.Count != 0;

    /// <summary>Add a general error.</summary>
    public ErrorCollectionBuilder Add(string code, string message, bool transient = false)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be null or whitespace.", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        this.errors.Add(transient ? Error.Transient(code, message) : Error.Permanent(code, message));
        this.isTransient |= transient;
        return this;
    }

    /// <summary>Add an already-constructed Error.</summary>
    public ErrorCollectionBuilder Add(Error error)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));
        this.errors.Add(error);
        this.isTransient |= error.IsTransient;
        return this;
    }

    public ErrorCollectionBuilder Merge(ErrorCollection other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (!other.HasErrors) return this;
        this.errors.AddRange(other.Errors);
        this.isTransient |= other.IsTransient;
        return this;
    }

    /// <summary>Freeze the current contents into an immutable ErrorCollection.</summary>
    public ErrorCollection Build()
    {
        if (this.errors.Count == 0) return ErrorCollection.Empty;
        var arr = this.errors.ToArray();
        return new ErrorCollection(arr, this.isTransient);
    }

    /// <summary>Clear the builder for reuse.</summary>
    public void Clear()
    {
        this.errors.Clear();
        this.isTransient = false;
    }
}