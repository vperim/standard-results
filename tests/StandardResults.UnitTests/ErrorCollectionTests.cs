namespace StandardResults.UnitTests;

public class ErrorCollectionImmutabilityTests
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

public class ErrorCollectionFluentApiTests
{
    [Fact]
    public void When_Condition_True_AddsError()
    {
        var collection = ErrorCollection.Empty
            .When(true, "error1", "First error")
            .When(false, "error2", "Second error")
            .When(true, "error3", "Third error");

        Assert.Equal(2, collection.Count);
        Assert.Contains("error1: First error", collection.Summary());
        Assert.Contains("error3: Third error", collection.Summary());
        Assert.DoesNotContain("error2", collection.Summary());
    }

    [Fact]
    public void When_LazyCondition_EvaluatesCorrectly()
    {
        var evaluationCount = 0;

        var collection = ErrorCollection.Empty
            .When(() => { evaluationCount++; return true; }, "error1", "Error 1")
            .When(() => { evaluationCount++; return false; }, "error2", "Error 2");

        Assert.Equal(2, evaluationCount);
        Assert.Equal(1, collection.Count);
        Assert.Contains("error1: Error 1", collection.Summary());
    }

    [Fact]
    public void Require_Condition_False_AddsError()
    {
        var collection = ErrorCollection.Empty
            .Require(true, "check1", "Check 1 failed")
            .Require(false, "check2", "Check 2 failed")
            .Require(true, "check3", "Check 3 failed");

        Assert.Equal(1, collection.Count);
        Assert.Contains("check2: Check 2 failed", collection.Summary());
        Assert.DoesNotContain("check1", collection.Summary());
        Assert.DoesNotContain("check3", collection.Summary());
    }

    [Fact]
    public void Require_LazyCondition_OnlyEvaluatedOnce()
    {
        var callCount = 0;
        Func<bool> expensiveCheck = () => { callCount++; return false; };

        var collection = ErrorCollection.Empty
            .Require(expensiveCheck, "validation", "Validation failed");

        Assert.Equal(1, callCount);
        Assert.Equal(1, collection.Count);
        Assert.Contains("validation: Validation failed", collection.Summary());
    }

    [Fact]
    public void From_CreatesCollection_FromErrorArray()
    {
        var error1 = Error.Permanent("error1", "Message 1");
        var error2 = Error.Transient("error2", "Message 2");
        var error3 = Error.Permanent("error3", "Message 3");

        var collection = ErrorCollection.From(error1, error2, error3);

        Assert.Equal(3, collection.Count);
        Assert.True(collection.IsTransient);
        Assert.Contains("error1: Message 1", collection.Summary());
        Assert.Contains("error2: Message 2", collection.Summary());
        Assert.Contains("error3: Message 3", collection.Summary());
    }

    [Fact]
    public void From_EmptyArray_ReturnsEmpty()
    {
        var collection = ErrorCollection.From();
        Assert.Same(ErrorCollection.Empty, collection);
    }

    [Fact]
    public void ConditionalMethods_WithTransient_PropagatesFlag()
    {
        var collection = ErrorCollection.Empty
            .When(true, "timeout", "Connection timeout", transient: true)
            .Require(false, "auth", "Authentication required", transient: false);

        Assert.Equal(2, collection.Count);
        Assert.True(collection.IsTransient);
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

public class ErrorCollectionConditionalMethodTests
{
    [Fact]
    public void When_NullConditionFunc_ThrowsArgumentNullException()
    {
        var collection = ErrorCollection.Empty;
        Func<bool>? nullFunc = null;

        Assert.Throws<ArgumentNullException>(() => collection.When(nullFunc!, "code", "message"));
    }

    [Fact]
    public void Require_NullConditionFunc_ThrowsArgumentNullException()
    {
        var collection = ErrorCollection.Empty;
        Func<bool>? nullFunc = null;

        Assert.Throws<ArgumentNullException>(() => collection.Require(nullFunc!, "code", "message"));
    }

    [Fact]
    public void When_And_Require_ChainedTogether()
    {
        var isValid = false;
        var hasPermission = true;

        var collection = ErrorCollection.Empty
            .When(!hasPermission, "permission", "No permission")
            .Require(isValid, "validation", "Validation failed")
            .When(string.IsNullOrEmpty(""), "data", "Data is empty")
            .Require(!string.IsNullOrEmpty("test"), "test", "Test failed");

        Assert.Equal(2, collection.Count);
        Assert.Contains("validation: Validation failed", collection.Summary());
        Assert.Contains("data: Data is empty", collection.Summary());
    }

    [Fact]
    public void ConditionalMethods_PreserveImmutability()
    {
        var original = ErrorCollection.Empty.WithError("initial", "Initial error");

        var modified = original
            .When(true, "added", "Added error")
            .Require(false, "required", "Required error");

        // Original should remain unchanged
        Assert.Equal(1, original.Count);
        Assert.Contains("initial: Initial error", original.Summary());

        // Modified should have all errors
        Assert.Equal(3, modified.Count);
        Assert.Contains("initial: Initial error", modified.Summary());
        Assert.Contains("added: Added error", modified.Summary());
        Assert.Contains("required: Required error", modified.Summary());
    }

    [Fact]
    public void LazyEvaluation_NotExecutedWhenNotNeeded()
    {
        var expensiveCheckExecuted = false;
        Func<bool> expensiveCheck = () =>
        {
            expensiveCheckExecuted = true;
            return true;
        };

        // When with false condition - lambda should not execute
        var collection1 = ErrorCollection.Empty
            .When(() => false && expensiveCheck(), "error", "Should not appear");

        Assert.False(expensiveCheckExecuted);
        Assert.Equal(0, collection1.Count);

        // Require with true condition - lambda should not execute
        expensiveCheckExecuted = false;
        var collection2 = ErrorCollection.Empty
            .Require(() => true || expensiveCheck(), "error", "Should not appear");

        Assert.False(expensiveCheckExecuted);
        Assert.Equal(0, collection2.Count);
    }

    [Fact]
    public void ComplexBusinessLogicScenario()
    {
        // Simulate a complex validation scenario
        var user = new { IsActive = false, HasSubscription = true, AccountAge = 5 };
        var request = new { Amount = 150.0, RequiresApproval = true, ApprovalCode = "" };

        var errors = ErrorCollection.Empty
            .Require(user.IsActive, "user_status", "User account is not active")
            .When(!user.HasSubscription, "subscription", "User does not have an active subscription")
            .When(request.Amount > 100 && string.IsNullOrEmpty(request.ApprovalCode),
                  "approval", "Approval code required for amounts over 100")
            .Require(() => user.AccountAge >= 1, "account_age", "Account too new")
            .When(() => request.RequiresApproval && string.IsNullOrEmpty(request.ApprovalCode),
                  "approval_required", "Approval code is mandatory");

        Assert.Equal(3, errors.Count);
        Assert.Contains("user_status", errors.Summary());
        Assert.Contains("approval", errors.Summary());
        Assert.Contains("approval_required", errors.Summary());
    }
}