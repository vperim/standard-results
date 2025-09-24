using System.Collections.Immutable;
using System.Diagnostics;

namespace StandardResults;

/// <summary>
/// Immutable, order-sensitive collection of field-scoped errors.
/// Implements IError to fit Result&lt;T, IError&gt; directly.
/// </summary>
[DebuggerDisplay("{Summary(),nq}")]
public sealed class ValidationErrors : IError, IEquatable<ValidationErrors>
{
    public string Code => nameof(ValidationErrors);
    public string Message => this.Summary();
    public bool IsTransient { get; }

    public int Count => this.errors.Count;
    public bool HasErrors => this.Count != 0;
    private readonly ImmutableList<Error> errors;

    public IReadOnlyList<Error> Errors => this.errors;

    public static ValidationErrors Empty { get; } =
        new(ImmutableList<Error>.Empty, isTransient: false);

    private ValidationErrors(ImmutableList<Error> errors, bool isTransient)
    {
        this.errors = errors;
        this.IsTransient = isTransient;
    }

    public string Summary(string separator)
        => this.HasErrors ? string.Join(separator, this.errors.Select(e => e.ToString())).TrimEnd() : string.Empty;

    public string Summary() => this.Summary("; ");

    /// <summary>Returns a new instance with one more field error.</summary>
    public ValidationErrors WithField(string fieldName, string message, bool transient = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        var error = transient ? Error.Transient(fieldName, message) : Error.Permanent(fieldName, message);
        return new ValidationErrors(this.errors.Add(error), this.IsTransient || transient);
    }

    /// <summary>Pure merge: returns a new instance concatenating errors in order.</summary>
    public ValidationErrors Merge(ValidationErrors other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (other.Count == 0) return this;
        if (this.Count == 0) return other;

        var merged = this.errors.AddRange(other.Errors);
        return new ValidationErrors(merged, this.IsTransient || other.IsTransient);
    }

    public bool Equals(ValidationErrors? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        if (this.IsTransient != other.IsTransient) return false;
        if (this.Count != other.Count) return false;

        return this.errors.SequenceEqual(other.Errors);
    }

    public override bool Equals(object? obj) => this.Equals(obj as ValidationErrors);

    public static bool operator ==(ValidationErrors? left, ValidationErrors? right)
        => ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(ValidationErrors? left, ValidationErrors? right) => !(left == right);

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

    public override string ToString() => this.HasErrors ? $"Invalid ({this.Count} errors)" : "Valid";

    public static ValidationErrors From(params (string Field, string Message, bool Transient)[] items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (items.Length == 0) return Empty;

        var list = ImmutableList.CreateBuilder<Error>();
        var transient = false;
        foreach (var (f, m, t) in items)
        {
            if (string.IsNullOrWhiteSpace(f)) throw new ArgumentException("Field name cannot be null or whitespace.", nameof(items));
            if (string.IsNullOrWhiteSpace(m)) throw new ArgumentException("Message cannot be null or whitespace.", nameof(items));
            list.Add(t ? Error.Transient(f, m) : Error.Permanent(f, m));
            transient |= t;
        }
        return new ValidationErrors(list.ToImmutable(), transient);
    }

    public ValidationErrors When(bool invalidCondition, string fieldName, string message, bool transient = false)
        => invalidCondition ? this.WithField(fieldName, message, transient) : this;

    public ValidationErrors When(Func<bool> invalidCondition, string fieldName, string message, bool transient = false)
    {
        if (invalidCondition is null) throw new ArgumentNullException(nameof(invalidCondition));
        return invalidCondition() ? this.WithField(fieldName, message, transient) : this;
    }

    public ValidationErrors Require(bool condition, string fieldName, string message, bool transient = false)
        => this.When(!condition, fieldName, message, transient);

    public ValidationErrors Require(Func<bool> condition, string fieldName, string message, bool transient = false)
    {
        if (condition is null) throw new ArgumentNullException(nameof(condition));
        return this.When(!condition(), fieldName, message, transient);
    }

    /// <summary>Adds an error if the value is null. For reference types.</summary>
    public ValidationErrors RequireNotNull<T>(T? value, string fieldName, string? message = null) where T : class
        => this.Require(value != null, fieldName, message ?? $"{fieldName} is required");

    /// <summary>Adds an error if the value is null. For nullable value types.</summary>
    public ValidationErrors RequireNotNull<T>(T? value, string fieldName, string? message = null) where T : struct
        => this.Require(value.HasValue, fieldName, message ?? $"{fieldName} is required");

    /// <summary>Adds an error if the string is null, empty, or whitespace.</summary>
    public ValidationErrors RequireNotEmpty(string? value, string fieldName, string? message = null)
        => this.Require(!string.IsNullOrWhiteSpace(value), fieldName, message ?? $"{fieldName} is required");

    /// <summary>Adds an error if the collection is null or empty.</summary>
    public ValidationErrors RequireNotEmpty<T>(IEnumerable<T>? value, string fieldName, string? message = null)
        => this.Require(value?.Any() == true, fieldName, message ?? $"{fieldName} must not be empty");
}
