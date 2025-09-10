using System.Diagnostics;

namespace StandardResults;

/// <summary>
/// Bulk validation container you can keep appending to and return once.
/// Implements IError so it fits Result&lt;T, IError&gt; directly.
/// </summary>
/// <summary>
/// Immutable, order-sensitive collection of field-scoped errors.
/// Implements IError to fit Result&lt;T, IError&gt; directly.
/// </summary>
[DebuggerDisplay("{Summary(),nq}")]
public sealed class ValidationErrors : IError, IEquatable<ValidationErrors>
{
    public string Code => nameof(ValidationErrors);
    public string Message => Summary();
    public bool IsTransient { get; }

    public int Count => Errors.Count;
    public bool HasErrors => Count != 0;

    // Internal ctor: used by builder/factory methods. Copies are created by callers as needed.
    internal ValidationErrors(Error[] errors, bool isTransient)
    {
        this.Errors = Array.AsReadOnly(errors);
        IsTransient = isTransient;
    }

    public IReadOnlyList<Error> Errors { get; }

    public static ValidationErrors Empty { get; } = new([], false);

    public string Summary(string separator)
        => HasErrors ? string.Join(separator, Errors.Select(e => e.ToString())).TrimEnd() : string.Empty;

    public string Summary() => this.Summary("; ");

    /// <summary>Pure add: returns a new instance with one more field error.</summary>
    public ValidationErrors WithField(string fieldName, string message, bool transient = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        var error = transient ? Error.Transient(fieldName, message) : Error.Permanent(fieldName, message);
        Error[] arr = [.. this.Errors, error];
        return new ValidationErrors(arr, IsTransient || transient);
    }

    /// <summary>Pure merge: returns a new instance concatenating errors in order.</summary>
    public ValidationErrors Merge(ValidationErrors other)
    {
        if (other.Count == 0) return this;
        if (this.Count == 0) return other;

        Error[] arr = [.. this.Errors, .. other.Errors];
        return new ValidationErrors(arr, this.IsTransient || other.IsTransient);
    }

    // Equality (order-sensitive) + transient flag
    public bool Equals(ValidationErrors? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        if (this.IsTransient != other.IsTransient) return false;
        if (this.Count != other.Count) return false;

        var a = Errors;
        var b = other.Errors;
        return !a.Where((t, i) => !t.Equals(b[i])).Any();
    }

    public override bool Equals(object? obj) => Equals(obj as ValidationErrors);

    public static bool operator ==(ValidationErrors left, ValidationErrors right) => Equals(left, right);
    public static bool operator !=(ValidationErrors left, ValidationErrors right) => !Equals(left, right);

    public override int GetHashCode()
    {
        unchecked
        {
            var h = IsTransient ? 1 : 0;
            return Errors.Aggregate(h, (current, t) => current * 31 + t.GetHashCode());
        }
    }

    public override string ToString() => HasErrors ? $"Invalid ({Count} errors)" : "Valid";
}