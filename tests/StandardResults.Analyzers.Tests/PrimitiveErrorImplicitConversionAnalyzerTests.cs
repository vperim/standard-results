using StandardResults.Analyzers.Tests.Verifiers;

namespace StandardResults.Analyzers.Tests;

using Verify = CSharpAnalyzerVerifier<PrimitiveErrorImplicitConversionAnalyzer>;

public class PrimitiveErrorImplicitConversionAnalyzerTests
{
    [Fact]
    public async Task ImplicitConversion_WithStringError_ReportsDiagnostic()
    {
        const string source = """
            using StandardResults;

            class TestClass
            {
                Result<int, string> GetValue(int id)
                {
                    if (id <= 0)
                        return "Invalid ID";

                    return Result<int, string>.Success(42);
                }
            }
            """;

        var expected = Verify.Diagnostic(DiagnosticIds.PrimitiveErrorImplicitConversion)
            .WithSpan(8, 20, 8, 32)
            .WithArguments("string");

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ImplicitConversion_WithIntError_ReportsDiagnostic()
    {
        const string source = """
            using StandardResults;

            class TestClass
            {
                Result<string, int> GetValue(bool valid)
                {
                    if (!valid)
                        return 404;

                    return Result<string, int>.Success("data");
                }
            }
            """;

        var expected = Verify.Diagnostic(DiagnosticIds.PrimitiveErrorImplicitConversion)
            .WithSpan(8, 20, 8, 23)
            .WithArguments("int");

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ImplicitConversion_WithErrorType_NoDiagnostic()
    {
        const string source = """
            using StandardResults;

            class TestClass
            {
                Result<int, Error> GetValue(int id)
                {
                    if (id <= 0)
                        return Error.Permanent("invalid_id", "Invalid ID");

                    return Result<int, Error>.Success(42);
                }
            }
            """;

        await Verify.VerifyNoDiagnosticsAsync(source);
    }

    [Fact]
    public async Task ImplicitConversion_WithValidationErrors_NoDiagnostic()
    {
        const string source = """
            using StandardResults;

            class TestClass
            {
                Result<int, ValidationErrors> GetValue(int id)
                {
                    var validation = ValidationErrors.Empty
                        .Require(id > 0, "id", "ID must be positive");

                    if (validation.HasErrors)
                        return validation;

                    return Result<int, ValidationErrors>.Success(42);
                }
            }
            """;

        await Verify.VerifyNoDiagnosticsAsync(source);
    }

    [Fact]
    public async Task ExplicitFailure_WithStringError_NoDiagnostic()
    {
        const string source = """
            using StandardResults;

            class TestClass
            {
                Result<int, string> GetValue(int id)
                {
                    if (id <= 0)
                        return Result<int, string>.Failure("Invalid ID");

                    return Result<int, string>.Success(42);
                }
            }
            """;

        await Verify.VerifyNoDiagnosticsAsync(source);
    }

    [Fact]
    public async Task MultipleImplicitConversions_WithStringError_ReportsMultipleDiagnostics()
    {
        const string source = """
            using StandardResults;

            class TestClass
            {
                Result<int, string> GetValue(int id)
                {
                    if (id < 0)
                        return "ID cannot be negative";

                    if (id == 0)
                        return "ID cannot be zero";

                    return Result<int, string>.Success(id);
                }
            }
            """;

        var expected1 = Verify.Diagnostic(DiagnosticIds.PrimitiveErrorImplicitConversion)
            .WithSpan(8, 20, 8, 43)
            .WithArguments("string");

        var expected2 = Verify.Diagnostic(DiagnosticIds.PrimitiveErrorImplicitConversion)
            .WithSpan(11, 20, 11, 39)
            .WithArguments("string");

        await Verify.VerifyAnalyzerAsync(source, expected1, expected2);
    }

    [Fact]
    public async Task ImplicitConversion_WithCustomErrorClass_NoDiagnostic()
    {
        const string source = """
            using StandardResults;

            class CustomError
            {
                public string Message { get; }
                public CustomError(string message) => Message = message;
            }

            class TestClass
            {
                Result<int, CustomError> GetValue(int id)
                {
                    if (id <= 0)
                        return new CustomError("Invalid ID");

                    return Result<int, CustomError>.Success(42);
                }
            }
            """;

        await Verify.VerifyNoDiagnosticsAsync(source);
    }

    [Fact]
    public async Task NoConversion_SuccessOnly_NoDiagnostic()
    {
        const string source = """
            using StandardResults;

            class TestClass
            {
                Result<int, string> GetValue()
                {
                    return Result<int, string>.Success(42);
                }
            }
            """;

        await Verify.VerifyNoDiagnosticsAsync(source);
    }
}
