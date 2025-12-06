using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace StandardResults.Analyzers;

/// <summary>
/// Analyzer that detects implicit conversions from primitive types to Result error type,
/// which may reduce code clarity.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimitiveErrorImplicitConversionAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Clarity";

    private static readonly LocalizableString Title =
        "Implicit error conversion with primitive type";

    private static readonly LocalizableString MessageFormat =
        "Implicit error conversion with primitive type '{0}' may reduce code clarity. Consider using 'Error' type or explicit 'Result<T, {0}>.Failure()'.";

    private static readonly LocalizableString Description =
        "Using implicit conversion with primitive error types like 'string' can make code less clear. " +
        "Consider using strongly-typed error types (Error, ValidationErrors) or explicit Failure() calls for better readability.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PrimitiveErrorImplicitConversion,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/vperim/standard-results#sr0001");

    /// <summary>
    /// Primitive types that trigger the warning when used as TError with implicit conversion.
    /// </summary>
    private static readonly ImmutableHashSet<SpecialType> PrimitiveTypes = ImmutableHashSet.Create(
        SpecialType.System_String,
        SpecialType.System_Int32,
        SpecialType.System_Int64,
        SpecialType.System_Int16,
        SpecialType.System_Byte,
        SpecialType.System_Boolean,
        SpecialType.System_Double,
        SpecialType.System_Single,
        SpecialType.System_Decimal,
        SpecialType.System_Char,
        SpecialType.System_Object
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
    }

    private static void AnalyzeConversion(OperationAnalysisContext context)
    {
        var conversion = (IConversionOperation)context.Operation;

        // Only analyze implicit conversions
        if (!conversion.IsImplicit)
            return;

        // Check if the target type is Result<T, TError>
        if (!IsResultType(conversion.Type, out var errorType))
            return;

        // Check if TError is a primitive type
        if (errorType is null || !IsPrimitiveType(errorType))
            return;

        // Check if the source type matches the error type (i.e., this is an error implicit conversion)
        var sourceType = conversion.Operand.Type;
        if (sourceType is null || !SymbolEqualityComparer.Default.Equals(sourceType, errorType))
            return;

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            Rule,
            conversion.Syntax.GetLocation(),
            errorType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsResultType(ITypeSymbol? type, out ITypeSymbol? errorType)
    {
        errorType = null;

        if (type is not INamedTypeSymbol namedType)
            return false;

        // Check if this is Result<T, TError>
        if (namedType.Name != "Result" || namedType.TypeArguments.Length != 2)
            return false;

        // Verify it's from StandardResults namespace
        var containingNamespace = namedType.ContainingNamespace?.ToDisplayString();
        if (containingNamespace != "StandardResults")
            return false;

        errorType = namedType.TypeArguments[1];
        return true;
    }

    private static bool IsPrimitiveType(ITypeSymbol type)
    {
        return PrimitiveTypes.Contains(type.SpecialType);
    }
}
