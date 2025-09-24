using System.Collections.Immutable;
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
    public int Count => this.errors.Count;
    public bool HasErrors => this.Count != 0;
    private readonly ImmutableList<Error> errors;

    public IReadOnlyList<Error> Errors => this.errors;

    public static ErrorCollection Empty { get; } =
        new(ImmutableList<Error>.Empty, isTransient: false);

    private ErrorCollection(ImmutableList<Error> errors, bool isTransient)
    {
        this.errors = errors;
        this.IsTransient = isTransient;
    }

    public string Summary(string separator)
        => this.HasErrors ? string.Join(separator, this.errors.Select(e => e.ToString())).TrimEnd() : string.Empty;

    public string Summary() => this.Summary("; ");

    /// <summary>Returns a new instance with one more error (general, not field-scoped).</summary>
    public ErrorCollection WithError(string code, string message, bool transient = false)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be null or whitespace.", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        var err = transient ? Error.Transient(code, message) : Error.Permanent(code, message);
        return new ErrorCollection(this.errors.Add(err), this.IsTransient || transient);
    }

    /// <summary>Pure add overload: use an already-constructed Error.</summary>
    public ErrorCollection WithError(Error error)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));
        return new ErrorCollection(this.errors.Add(error), this.IsTransient || error.IsTransient);
    }

    /// <summary>Pure merge: returns a new instance concatenating errors in order.</summary>
    public ErrorCollection Merge(ErrorCollection other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (other.Count == 0) return this;
        if (this.Count == 0) return other;

        var merged = ((ImmutableList<Error>) this.errors).AddRange(other.Errors);
        return new ErrorCollection(merged, this.IsTransient || other.IsTransient);
    }

    // Equality (order-sensitive) + transient flag
    public bool Equals(ErrorCollection? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        if (this.IsTransient != other.IsTransient) return false;
        if (this.Count != other.Count) return false;

        return this.errors.SequenceEqual(other.Errors);
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
            foreach (var e in this.errors)
                h = h * 31 + e.GetHashCode();
            return h;
        }
    }

    public override string ToString() => this.HasErrors ? $"Errors ({this.Count})" : "No errors";

    // Convenience factory for creating from a set of errors
    public static ErrorCollection From(params Error[] errors)
    {
        if (errors is null) throw new ArgumentNullException(nameof(errors));
        if (errors.Length == 0) return Empty;

        var list = ImmutableList.CreateRange(errors);
        var transient = errors.Any(e => e.IsTransient);
        return new ErrorCollection(list, transient);
    }

    /// <summary>Adds an error when the condition is true.</summary>
    public ErrorCollection When(bool condition, string code, string message, bool transient = false)
        => condition ? this.WithError(code, message, transient) : this;

    /// <summary>Adds an error when the condition evaluates to true. Use for expensive checks.</summary>
    public ErrorCollection When(Func<bool> condition, string code, string message, bool transient = false)
    {
        if (condition is null) throw new ArgumentNullException(nameof(condition));
        return condition() ? this.WithError(code, message, transient) : this;
    }

    /// <summary>Adds an error when the condition is false.</summary>
    public ErrorCollection Require(bool condition, string code, string message, bool transient = false)
        => this.When(!condition, code, message, transient);

    /// <summary>Adds an error when the condition evaluates to false. Use for expensive checks.</summary>
    public ErrorCollection Require(Func<bool> condition, string code, string message, bool transient = false)
    {
        if (condition is null) throw new ArgumentNullException(nameof(condition));
        return this.When(!condition(), code, message, transient);
    }
}
