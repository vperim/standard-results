namespace StandardResults;

/// <summary>
/// Mutable accumulator for ValidationErrors. Use within a scope, then Build() to freeze.
/// </summary>
public sealed class ValidationErrorsBuilder
{
    private readonly List<Error> errors = [];
    private bool isTransient;

    public int Count => errors.Count;
    public bool HasErrors => Count != 0;

    public ValidationErrorsBuilder AddField(string fieldName, string message, bool transient = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        errors.Add(transient ? Error.Transient(fieldName, message) : Error.Permanent(fieldName, message));
        isTransient |= transient;
        return this;
    }

    /// <summary>
    /// Adds an error when <paramref name="invalidCondition"/> is true.
    /// </summary>
    public ValidationErrorsBuilder When(bool invalidCondition, string fieldName, string message, bool transient = false)
    {
        if (invalidCondition) AddField(fieldName, message, transient);
        return this;
    }

    /// <summary>
    /// Adds an error when <paramref name="invalidCondition"/> evaluates to true.
    /// Use this for expensive checks you want to defer.
    /// </summary>
    public ValidationErrorsBuilder When(Func<bool> invalidCondition, string fieldName, string message, bool transient = false)
    {
        if (invalidCondition == null) throw new ArgumentNullException(nameof(invalidCondition));
        if (invalidCondition()) AddField(fieldName, message, transient);
        return this;
    }

    /// <summary>
    /// Adds an error when <paramref name="condition"/> is false.
    /// Alias for "require X", i.e., failed requirement produces an error.
    /// </summary>
    public ValidationErrorsBuilder Require(bool condition, string fieldName, string message, bool transient = false)
        => When(!condition, fieldName, message, transient);

    /// <summary>
    /// Adds an error when <paramref name="condition"/> evaluates to false.
    /// </summary>
    public ValidationErrorsBuilder Require(Func<bool> condition, string fieldName, string message, bool transient = false)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        return When(!condition(), fieldName, message, transient);
    }

    /// <summary>Freeze the current contents into an immutable ValidationErrors.</summary>
    public ValidationErrors Build()
    {
        if (errors.Count == 0) return ValidationErrors.Empty;
        var arr = errors.ToArray();
        return new ValidationErrors(arr, isTransient);
    }
    
    public ValidationErrorsBuilder Merge(ValidationErrors other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (!other.HasErrors) return this;
        this.errors.AddRange(other.Errors);
        isTransient |= other.IsTransient;
        return this;
    }

    /// <summary>Clear the builder for reuse.</summary>
    public void Clear()
    {
        errors.Clear();
        isTransient = false;
    }
}