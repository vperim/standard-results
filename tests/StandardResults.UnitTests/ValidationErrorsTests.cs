namespace StandardResults.UnitTests;

public class ValidationErrorsTests
{
    [Fact]
    public void Empty_Is_Singleton_With_NoErrors()
    {
        var e = ValidationErrors.Empty;
        Assert.Equal("Valid", e.ToString());
        Assert.False(e.HasErrors);
        Assert.Equal(0, e.Count);
        Assert.Equal(string.Empty, e.Summary());
    }

    [Fact]
    public void WithField_Appends_And_Transient_Bubbles_Up()
    {
        var a = ValidationErrors.Empty
            .WithField("name", "required")                // permanent
            .WithField("age", "temporary outage", true);  // transient

        Assert.True(a.HasErrors);
        Assert.Equal(2, a.Count);
        Assert.True(a.IsTransient);
        Assert.Contains("name: required", a.Summary());
        Assert.Contains("age: temporary outage", a.Summary());
    }

    [Fact]
    public void Merge_Preserves_Order_And_Transient()
    {
        var left  = ValidationErrors.Empty.WithField("a", "a1");
        var right = ValidationErrors.Empty.WithField("b", "b1", transient: true);

        var merged = left.Merge(right);

        Assert.Equal(2, merged.Count);
        Assert.True(merged.IsTransient);
        Assert.Equal("a: a1; b: b1", merged.Summary("; "));
    }

    [Fact]
    public void Equality_Is_Order_Sensitive()
    {
        var x = ValidationErrors.Empty.WithField("a","1").WithField("b","2");
        var y = ValidationErrors.Empty.WithField("a","1").WithField("b","2");
        var z = ValidationErrors.Empty.WithField("b","2").WithField("a","1");

        Assert.True(x.Equals(y));
        Assert.False(x.Equals(z));
        Assert.Equal(x.GetHashCode(), y.GetHashCode());
    }
}

public class ValidationErrorsBuilderTests
{
    [Fact]
    public void Build_Empty_Returns_Singleton()
    {
        var b = new ValidationErrorsBuilder();
        var built = b.Build();
        Assert.Same(ValidationErrors.Empty, built);
    }

    [Fact]
    public void Add_Require_When_Work_As_Expected()
    {
        var b = new ValidationErrorsBuilder()
            .AddField("name", "required")
            .Require(false, "age", "must be >= 18")
            .When(() => true, "email", "invalid format");

        var res = b.Build();
        Assert.Equal(3, res.Count);
        Assert.Contains("name: required", res.Summary());
        Assert.Contains("age: must be >= 18", res.Summary());
        Assert.Contains("email: invalid format", res.Summary());
        Assert.False(res.IsTransient);

        // Clear allows reuse
        b.Clear();
        Assert.Same(ValidationErrors.Empty, b.Build());
    }

    [Fact]
    public void Merge_Pulls_In_Existing_Set_And_Tracks_Transient()
    {
        var other = ValidationErrors.Empty
            .WithField("x", "oops", transient: true);

        var b = new ValidationErrorsBuilder()
            .Merge(other);

        var res = b.Build();
        Assert.True(res.IsTransient);
        Assert.Equal(1, res.Count);
        Assert.Contains("x: oops", res.Summary());
    }
}