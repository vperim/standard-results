using System.Diagnostics;

namespace StandardResults;

/// <summary>
/// Immutable, order-sensitive collection of generic (non-field-scoped) errors.
/// Implements IError to fit Result&lt;T, IError&gt; directly.
/// </summary>
[DebuggerDisplay("{Summary(),nq}")]
public sealed class ErrorCollection : IError, IEquatable<ErrorCollection>
{
    public string Code => nameof(ErrorCollection);
    public string Message => this.Summary();
    public bool IsTransient { get; }
    public int Count => this.Errors.Count;
    public bool HasErrors => this.Count != 0;

    internal ErrorCollection(Error[] errors, bool isTransient)
    {
        this.Errors = Array.AsReadOnly(errors);
        this.IsTransient = isTransient;
    }

    public IReadOnlyList<Error> Errors { get; }

    public static ErrorCollection Empty { get; } = new([], false);

    public string Summary(string separator)
        =>
            this.HasErrors ? string.Join(separator, this.Errors.Select(e => e.ToString())).TrimEnd() : string.Empty;

    public string Summary() => this.Summary("; ");

    /// <summary>
    /// Pure add: returns a new instance with one more error (general, not field-scoped).
    /// </summary>
    public ErrorCollection WithError(string code, string message, bool transient = false)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be null or whitespace.", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        var err = transient ? Error.Transient(code, message) : Error.Permanent(code, message);
        Error[] arr = [.. this.Errors, err]; 
        return new ErrorCollection(arr, this.IsTransient || transient);
    }

    /// <summary>
    /// Pure add overload: use an already-constructed Error.
    /// </summary>
    public ErrorCollection WithError(Error error)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));
        Error[] arr = [.. this.Errors, error];
        return new ErrorCollection(arr, this.IsTransient || error.IsTransient);
    }

    /// <summary>
    /// Pure merge: returns a new instance concatenating errors in order.
    /// </summary>
    public ErrorCollection Merge(ErrorCollection other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (other.Count == 0) return this;
        if (this.Count == 0) return other;

        Error[] arr = [.. this.Errors, .. other.Errors];
        return new ErrorCollection(arr, this.IsTransient || other.IsTransient);
    }

    // Equality (order-sensitive) + transient flag
    public bool Equals(ErrorCollection? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        if (this.IsTransient != other.IsTransient) return false;
        if (this.Count != other.Count) return false;

        return this.Errors.SequenceEqual(other.Errors);
    }

    public override bool Equals(object? obj) => this.Equals(obj as ErrorCollection);

    public static bool operator ==(ErrorCollection? left, ErrorCollection? right)
        => ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(ErrorCollection? left, ErrorCollection? right) => !(left == right);

    public override int GetHashCode()
    {
        unchecked
        {
            var h = this.IsTransient ? 1 : 0;
            return this.Errors.Aggregate(h, (current, t) => current * 31 + t.GetHashCode());
        }
    }

    public override string ToString() => this.HasErrors ? $"Errors ({this.Count})" : "No errors";
}