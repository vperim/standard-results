namespace StandardResults.UnitTests;

public class ErrorCollectionTests
{
    [Fact]
    public void Empty_Has_NoErrors_And_Empty_Summary()
    {
        var e = ErrorCollection.Empty;
        Assert.False(e.HasErrors);
        Assert.Equal(0, e.Count);
        Assert.Equal(string.Empty, e.Summary());
        Assert.Equal("No errors", e.ToString());
    }

    [Fact]
    public void WithError_Adds_And_Tracks_Transient()
    {
        var a = ErrorCollection.Empty
            .WithError("validation", "bad")
            .WithError("timeout", "try later", transient: true);

        Assert.True(a.HasErrors);
        Assert.Equal(2, a.Count);
        Assert.True(a.IsTransient);
        Assert.Equal("validation: bad; timeout: try later", a.Summary("; "));
    }

    [Fact]
    public void Merge_Is_Order_Sensitive_And_Propagates_Transient()
    {
        var left  = ErrorCollection.Empty.WithError("a","1");
        var right = ErrorCollection.Empty.WithError("b","2", transient: true);

        var merged = left.Merge(right);

        Assert.Equal(2, merged.Count);
        Assert.True(merged.IsTransient);
        Assert.Equal("a: 1; b: 2", merged.Summary("; "));

        var same = ErrorCollection.Empty
            .WithError("a","1")
            .WithError("b","2", transient: true);

        Assert.True(merged.Equals(same));
        Assert.Equal(merged.GetHashCode(), same.GetHashCode());
    }
}

public class ErrorCollectionBuilderTests
{
    [Fact]
    public void Build_Empty_Returns_Singleton()
    {
        var b = new ErrorCollectionBuilder();
        Assert.Same(ErrorCollection.Empty, b.Build());
    }

    [Fact]
    public void Add_And_Merge_Then_Build_Produces_Immutable_Set()
    {
        var other = ErrorCollection.Empty.WithError("x", "y", transient: true);

        var b = new ErrorCollectionBuilder()
            .Add("a", "1")
            .Merge(other);

        var res = b.Build();
        Assert.Equal(2, res.Count);
        Assert.True(res.IsTransient);
        Assert.Equal("a: 1; x: y", res.Summary("; "));

        // Clear resets internal state
        b.Clear();
        Assert.Same(ErrorCollection.Empty, b.Build());
    }

    [Fact]
    public void Add_Error_Instance_Takes_Flags_From_Error()
    {
        var err = Error.Transient("t", "msg");
        var b = new ErrorCollectionBuilder().Add(err);
        var res = b.Build();

        Assert.True(res.IsTransient);
        Assert.Equal("t: msg", res.Summary());
    }
}

public class ErrorCollectionAdvancedTests
{
    [Fact]
    public void WithError_NullOrEmptyCode_ThrowsArgumentException()
    {
        var collection = ErrorCollection.Empty;
        
        Assert.Throws<ArgumentException>(() => collection.WithError(null!, "message"));
        Assert.Throws<ArgumentException>(() => collection.WithError("", "message"));
        Assert.Throws<ArgumentException>(() => collection.WithError("   ", "message"));
    }

    [Fact]
    public void WithError_NullOrEmptyMessage_ThrowsArgumentException()
    {
        var collection = ErrorCollection.Empty;
        
        Assert.Throws<ArgumentException>(() => collection.WithError("code", null!));
        Assert.Throws<ArgumentException>(() => collection.WithError("code", ""));
        Assert.Throws<ArgumentException>(() => collection.WithError("code", "   "));
    }

    [Fact]
    public void WithError_ErrorInstance_NullError_ThrowsArgumentNullException()
    {
        var collection = ErrorCollection.Empty;
        
        Assert.Throws<ArgumentNullException>(() => collection.WithError(null!));
    }

    [Fact]
    public void Merge_NullCollection_ThrowsArgumentNullException()
    {
        var collection = ErrorCollection.Empty;
        
        Assert.Throws<ArgumentNullException>(() => collection.Merge(null!));
    }

    [Fact]
    public void Merge_EmptyCollection_ReturnsOriginal()
    {
        var original = ErrorCollection.Empty.WithError("test", "message");
        var empty = ErrorCollection.Empty;
        
        var result = original.Merge(empty);
        
        Assert.Same(original, result);
    }

    [Fact]
    public void Merge_WithEmptyOriginal_ReturnsOther()
    {
        var original = ErrorCollection.Empty;
        var other = ErrorCollection.Empty.WithError("test", "message");
        
        var result = original.Merge(other);
        
        Assert.Same(other, result);
    }

    [Fact]
    public void OperatorEquals_NullReferences_HandledCorrectly()
    {
        ErrorCollection? left = null;
        ErrorCollection? right = null;
        var collection = ErrorCollection.Empty.WithError("test", "message");
        
        Assert.True(left == right);
        Assert.False(left == collection);
        Assert.False(collection == left);
#pragma warning disable CS1718 // Intentionally testing reflexivity
        Assert.True(collection == collection);
#pragma warning restore CS1718
    }

    [Fact]
    public void OperatorNotEquals_NullReferences_HandledCorrectly()
    {
        ErrorCollection? left = null;
        ErrorCollection? right = null;
        var collection = ErrorCollection.Empty.WithError("test", "message");
        
        Assert.False(left != right);
        Assert.True(left != collection);
        Assert.True(collection != left);
        Assert.False(false); // Testing reflexivity"
    }

    [Fact]
    public void Equals_ObjectOverload_HandlesNullAndWrongType()
    {
        var collection = ErrorCollection.Empty.WithError("test", "message");
        
        Assert.False(collection.Equals((object?)null));
        Assert.False(false);
        Assert.False(false);
        Assert.True(collection.Equals((object)collection));
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var collection = ErrorCollection.Empty.WithError("test", "message");
        
        Assert.True(collection.Equals(collection));
    }

    [Fact]
    public void Equals_DifferentTransientFlag_ReturnsFalse()
    {
        var permanent = ErrorCollection.Empty.WithError("test", "message", transient: false);
        var transient = ErrorCollection.Empty.WithError("test", "message", transient: true);
        
        Assert.False(permanent.Equals(transient));
        Assert.False(transient.Equals(permanent));
    }

    [Fact]
    public void Equals_DifferentOrder_ReturnsFalse()
    {
        var first = ErrorCollection.Empty
            .WithError("a", "1")
            .WithError("b", "2");
            
        var second = ErrorCollection.Empty
            .WithError("b", "2")
            .WithError("a", "1");
        
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void GetHashCode_ConsistentWithEquals()
    {
        var collection1 = ErrorCollection.Empty
            .WithError("test", "message", transient: true);
            
        var collection2 = ErrorCollection.Empty
            .WithError("test", "message", transient: true);
        
        Assert.True(collection1.Equals(collection2));
        Assert.Equal(collection1.GetHashCode(), collection2.GetHashCode());
    }

    [Fact]
    public void IErrorInterface_Implementation()
    {
        var collection = ErrorCollection.Empty
            .WithError("validation", "field required")
            .WithError("timeout", "service unavailable", transient: true);
        
        IError error = collection;
        
        Assert.Equal(nameof(ErrorCollection), error.Code);
        Assert.Equal("validation: field required; timeout: service unavailable", error.Message);
        Assert.True(error.IsTransient);
    }

    [Fact]
    public void Summary_CustomSeparator_FormatsCorrectly()
    {
        var collection = ErrorCollection.Empty
            .WithError("a", "first")
            .WithError("b", "second")
            .WithError("c", "third");
        
        Assert.Equal("a: first | b: second | c: third", collection.Summary(" | "));
        Assert.Equal("a: first\nb: second\nc: third", collection.Summary("\n"));
        Assert.Equal("a: firstb: secondc: third", collection.Summary(""));
    }

    [Fact]
    public void ToString_ShowsCorrectFormat()
    {
        var empty = ErrorCollection.Empty;
        var single = ErrorCollection.Empty.WithError("test", "message");
        var multiple = ErrorCollection.Empty
            .WithError("first", "message1")
            .WithError("second", "message2");
        
        Assert.Equal("No errors", empty.ToString());
        Assert.Equal("Errors (1)", single.ToString());
        Assert.Equal("Errors (2)", multiple.ToString());
    }

    [Fact]
    public void LargeCollection_PerformanceCharacteristics()
    {
        var collection = ErrorCollection.Empty;
        
        // Add many errors to test performance
        for (var i = 0; i < 1000; i++)
        {
            collection = collection.WithError($"code{i}", $"message{i}", i % 2 == 0);
        }
        
        Assert.Equal(1000, collection.Count);
        Assert.True(collection.IsTransient); // At least one transient error
        Assert.Contains("code999: message999", collection.Summary());
    }
}