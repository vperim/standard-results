using System.Diagnostics;

namespace StandardResults;

[DebuggerDisplay("{Code}: {Message}")]
public sealed record Error : IError
{
    public string Code { get; }
    public string Message { get; }
    public bool IsTransient { get; }

    private Error(string code, string message, bool transient)
        => (this.Code, this.Message, this.IsTransient) = (code, message, transient);

    public static Error Transient(string code, string msg) => new(code, msg, true);

    public static Error Permanent(string code, string msg) => new(code, msg, false);

    public override string ToString() => string.IsNullOrWhiteSpace(this.Code)
        ? $"{Message}"
        : $"{Code}: {Message}";

    public bool Equals(Error? other)
        => other is not null
           && string.Equals(Code, other.Code, StringComparison.Ordinal)
           && string.Equals(Message, other.Message, StringComparison.Ordinal)
           && IsTransient == other.IsTransient;

    public override int GetHashCode()
    {
        unchecked
        {
            var h = 17;
            h = h * 31 + StringComparer.Ordinal.GetHashCode(Code);
            h = h * 31 + StringComparer.Ordinal.GetHashCode(Message);
            h = h * 31 + (IsTransient ? 1 : 0);
            return h;
        }
    }
}