namespace StandardResults.UnitTests;

public class ErrorTests
{
    [Fact]
    public void Equality_And_Hash_Follow_Members()
    {
        var a = Error.Permanent("c", "m");
        var b = Error.Permanent("c", "m");
        var c = Error.Transient("c", "m"); // differs only by IsTransient

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_Shows_Code_When_Present()
    {
        var e = Error.Permanent("validation", "bad");
        Assert.Equal("validation: bad", e.ToString());
    }
}