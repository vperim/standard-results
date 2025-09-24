namespace StandardResults.UnitTests;

public class ValidationErrorsImmutabilityTests
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

public class ValidationErrorsFluentApiTests
{
    [Fact]
    public void When_Condition_True_AddsError()
    {
        var validation = ValidationErrors.Empty
            .When(true, "field1", "error1")
            .When(false, "field2", "error2")
            .When(true, "field3", "error3");

        Assert.Equal(2, validation.Count);
        Assert.Contains("field1: error1", validation.Summary());
        Assert.Contains("field3: error3", validation.Summary());
        Assert.DoesNotContain("field2", validation.Summary());
    }

    [Fact]
    public void When_LazyCondition_OnlyEvaluatedWhenTrue()
    {
        var evaluationCount = 0;

        var validation = ValidationErrors.Empty
            .When(() => { evaluationCount++; return true; }, "field1", "error1")
            .When(() => { evaluationCount++; return false; }, "field2", "error2");

        Assert.Equal(2, evaluationCount);
        Assert.Equal(1, validation.Count);
        Assert.Contains("field1: error1", validation.Summary());
    }

    [Fact]
    public void Require_Condition_False_AddsError()
    {
        var validation = ValidationErrors.Empty
            .Require(true, "field1", "required1")
            .Require(false, "field2", "required2")
            .Require(true, "field3", "required3");

        Assert.Equal(1, validation.Count);
        Assert.Contains("field2: required2", validation.Summary());
        Assert.DoesNotContain("field1", validation.Summary());
        Assert.DoesNotContain("field3", validation.Summary());
    }

    [Fact]
    public void Require_LazyCondition_EvaluatesCorrectly()
    {
        var callCount = 0;
        Func<bool> expensiveValidation = () => { callCount++; return false; };

        var validation = ValidationErrors.Empty
            .Require(expensiveValidation, "field", "validation failed");

        Assert.Equal(1, callCount);
        Assert.Equal(1, validation.Count);
        Assert.Contains("field: validation failed", validation.Summary());
    }

    [Fact]
    public void From_CreatesValidationErrors_FromTuples()
    {
        var validation = ValidationErrors.From(
            ("field1", "error1", false),
            ("field2", "error2", true),
            ("field3", "error3", false)
        );

        Assert.Equal(3, validation.Count);
        Assert.True(validation.IsTransient);
        Assert.Contains("field1: error1", validation.Summary());
        Assert.Contains("field2: error2", validation.Summary());
        Assert.Contains("field3: error3", validation.Summary());
    }

    [Fact]
    public void From_EmptyArray_ReturnsEmpty()
    {
        var validation = ValidationErrors.From();
        Assert.Same(ValidationErrors.Empty, validation);
    }

    [Fact]
    public void Merge_CombinesValidationErrors()
    {
        var first = ValidationErrors.Empty
            .WithField("field1", "error1");
        var second = ValidationErrors.Empty
            .WithField("field2", "error2", transient: true);

        var merged = first.Merge(second);

        Assert.Equal(2, merged.Count);
        Assert.True(merged.IsTransient);
        Assert.Contains("field1: error1", merged.Summary());
        Assert.Contains("field2: error2", merged.Summary());
    }
}

public class ValidationErrorsHelperMethodTests
{
    [Fact]
    public void RequireNotNull_ReferenceType_NullValue_AddsError()
    {
        string? nullString = null;
        string? nonNullString = "value";

        var validation = ValidationErrors.Empty
            .RequireNotNull(nullString, "field1")
            .RequireNotNull(nonNullString, "field2");

        Assert.Equal(1, validation.Count);
        Assert.Contains("field1 is required", validation.Summary());
        Assert.DoesNotContain("field2", validation.Summary());
    }

    [Fact]
    public void RequireNotNull_ReferenceType_CustomMessage()
    {
        object? nullObject = null;

        var validation = ValidationErrors.Empty
            .RequireNotNull(nullObject, "importantField", "This field is very important");

        Assert.Equal(1, validation.Count);
        Assert.Contains("This field is very important", validation.Summary());
    }

    [Fact]
    public void RequireNotNull_ValueType_NullableInt()
    {
        int? nullInt = null;
        int? nonNullInt = 42;

        var validation = ValidationErrors.Empty
            .RequireNotNull(nullInt, "nullableInt")
            .RequireNotNull(nonNullInt, "nonNullInt");

        Assert.Equal(1, validation.Count);
        Assert.Contains("nullableInt is required", validation.Summary());
        Assert.DoesNotContain("nonNullInt", validation.Summary());
    }

    [Fact]
    public void RequireNotNull_ValueType_CustomMessage()
    {
        DateTime? nullDate = null;

        var validation = ValidationErrors.Empty
            .RequireNotNull(nullDate, "birthDate", "Birth date must be provided");

        Assert.Equal(1, validation.Count);
        Assert.Contains("Birth date must be provided", validation.Summary());
    }

    [Fact]
    public void RequireNotEmpty_String_Various_Cases()
    {
        var validation = ValidationErrors.Empty
            .RequireNotEmpty(null, "field1")
            .RequireNotEmpty("", "field2")
            .RequireNotEmpty("   ", "field3")
            .RequireNotEmpty("valid", "field4");

        Assert.Equal(3, validation.Count);
        Assert.Contains("field1 is required", validation.Summary());
        Assert.Contains("field2 is required", validation.Summary());
        Assert.Contains("field3 is required", validation.Summary());
        Assert.DoesNotContain("field4", validation.Summary());
    }

    [Fact]
    public void RequireNotEmpty_String_CustomMessage()
    {
        var validation = ValidationErrors.Empty
            .RequireNotEmpty("", "email", "Email address cannot be empty");

        Assert.Equal(1, validation.Count);
        Assert.Contains("Email address cannot be empty", validation.Summary());
    }

    [Fact]
    public void RequireNotEmpty_Collection_Various_Cases()
    {
        List<int>? nullList = null;
        var emptyList = new List<int>();
        var nonEmptyList = new List<int> { 1, 2, 3 };
        int[]? nullArray = null;
        var emptyArray = Array.Empty<string>();
        var nonEmptyArray = new[] { "a", "b" };

        var validation = ValidationErrors.Empty
            .RequireNotEmpty(nullList, "nullList")
            .RequireNotEmpty(emptyList, "emptyList")
            .RequireNotEmpty(nonEmptyList, "nonEmptyList")
            .RequireNotEmpty(nullArray, "nullArray")
            .RequireNotEmpty(emptyArray, "emptyArray")
            .RequireNotEmpty(nonEmptyArray, "nonEmptyArray");

        Assert.Equal(4, validation.Count);
        Assert.Contains("nullList must not be empty", validation.Summary());
        Assert.Contains("emptyList must not be empty", validation.Summary());
        Assert.Contains("nullArray must not be empty", validation.Summary());
        Assert.Contains("emptyArray must not be empty", validation.Summary());
        Assert.DoesNotContain("nonEmptyList", validation.Summary());
        Assert.DoesNotContain("nonEmptyArray", validation.Summary());
    }

    [Fact]
    public void RequireNotEmpty_Collection_CustomMessage()
    {
        var emptyItems = new List<string>();

        var validation = ValidationErrors.Empty
            .RequireNotEmpty(emptyItems, "items", "Please select at least one item");

        Assert.Equal(1, validation.Count);
        Assert.Contains("Please select at least one item", validation.Summary());
    }

    [Fact]
    public void HelperMethods_ChainedTogether()
    {
        string? nullString = null;
        int? validInt = 42;
        var emptyList = new List<string>();

        var validation = ValidationErrors.Empty
            .RequireNotNull(nullString, "username")
            .RequireNotNull(validInt, "userId")
            .RequireNotEmpty("", "email")
            .RequireNotEmpty(emptyList, "roles");

        Assert.Equal(3, validation.Count);
        Assert.Contains("username is required", validation.Summary());
        Assert.Contains("email is required", validation.Summary());
        Assert.Contains("roles must not be empty", validation.Summary());
        Assert.DoesNotContain("userId", validation.Summary());
    }
}