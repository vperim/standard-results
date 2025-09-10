namespace StandardResults;

public interface IError
{
    /// <summary>
    /// e.g., "validation", "not_found", "sql_timeout"
    /// </summary>
    string Code { get; }
    string Message { get; }
    /// <summary>
    /// Indicates whether the error is temporary and the operation can be retried.
    /// </summary>
    bool IsTransient { get; }
}