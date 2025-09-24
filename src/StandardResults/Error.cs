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
        ? $"{this.Message}"
        : $"{this.Code}: {this.Message}";

    public bool Equals(Error? other)
        => other is not null
           && string.Equals(this.Code, other.Code, StringComparison.Ordinal)
           && string.Equals(this.Message, other.Message, StringComparison.Ordinal)
           && this.IsTransient == other.IsTransient;

    public override int GetHashCode()
    {
        unchecked
        {
            var h = 17;
            h = h * 31 + StringComparer.Ordinal.GetHashCode(this.Code);
            h = h * 31 + StringComparer.Ordinal.GetHashCode(this.Message);
            h = h * 31 + (this.IsTransient ? 1 : 0);
            return h;
        }
    }
}