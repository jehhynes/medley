using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Medley.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypeSafeOkResultAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MEDLEY001";
    private const string Category = "Type Safety";

    private static readonly LocalizableString Title = "Use type-safe Ok<T>() instead of Ok(object)";
    private static readonly LocalizableString MessageFormat = "Method returns ActionResult<{0}> but uses Ok() with incompatible type. Use Ok<{0}>() or change return type to match the actual value. NEVER return 'object' or anonymous types, and never return Entity Framework domain types. Always use DTO types.";
    private static readonly LocalizableString Description = "Using Ok() with a value that doesn't match the declared ActionResult<T> type loses type safety.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a call to Ok()
        if (invocation.Expression is not IdentifierNameSyntax identifierName ||
            identifierName.Identifier.Text != "Ok")
        {
            return;
        }

        // Get the method symbol
        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol == null)
        {
            return;
        }

        // Check if it's from ControllerBase
        if (methodSymbol.ContainingType?.ToString() != "Microsoft.AspNetCore.Mvc.ControllerBase")
        {
            return;
        }

        // Find the containing method
        var containingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod == null)
        {
            return;
        }

        // Get the return type of the containing method
        var containingMethodSymbol = context.SemanticModel.GetDeclaredSymbol(containingMethod);
        if (containingMethodSymbol == null)
        {
            return;
        }

        var returnType = containingMethodSymbol.ReturnType;

        // Check if return type is ActionResult<T> or Task<ActionResult<T>>
        ITypeSymbol? actionResultType = null;
        
        if (returnType is INamedTypeSymbol namedReturnType)
        {
            // Handle Task<ActionResult<T>>
            if (namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 1)
            {
                actionResultType = namedReturnType.TypeArguments[0];
            }
            else
            {
                actionResultType = namedReturnType;
            }
        }

        // Check if it's ActionResult<T>
        if (actionResultType is not INamedTypeSymbol actionResultNamedType ||
            actionResultNamedType.Name != "ActionResult" ||
            actionResultNamedType.TypeArguments.Length != 1)
        {
            return;
        }

        var expectedType = actionResultNamedType.TypeArguments[0];

        // Get the argument passed to Ok()
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return; // Ok() with no arguments is fine (returns 200 OK with no body)
        }

        var argument = invocation.ArgumentList.Arguments[0];
        var argumentType = context.SemanticModel.GetTypeInfo(argument.Expression).Type;

        if (argumentType == null)
        {
            return;
        }

        // Check if the argument type matches the expected type
        var conversion = context.Compilation.ClassifyConversion(argumentType, expectedType);
        
        // Allow if types match or there's an implicit conversion
        if (conversion.IsIdentity || conversion.IsImplicit)
        {
            return;
        }

        // Check for anonymous types - these are problematic
        if (argumentType.IsAnonymousType)
        {
            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), expectedType.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check if the argument is object type (most common issue)
        if (argumentType.SpecialType == SpecialType.System_Object)
        {
            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), expectedType.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Report diagnostic for type mismatch
        var typeMismatchDiagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), expectedType.Name);
        context.ReportDiagnostic(typeMismatchDiagnostic);
    }
}
