using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Luchdom.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferEmptyCollectionExpressionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "LUCH0001",
        "Use an empty collection expression",
        "Use [] for the empty array, with ({0})[] to preserve its type and overload selection",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var name = invocation.Expression is MemberAccessExpressionSyntax member ? member.Name : invocation.Expression as SimpleNameSyntax;
        if (name?.Identifier.ValueText != "Empty" ||
            invocation.SyntaxTree.Options is not CSharpParseOptions { LanguageVersion: >= LanguageVersion.CSharp12 } ||
            context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol
            {
                IsStatic: true,
                Arity: 1,
                ContainingType.SpecialType: SpecialType.System_Array
            } method)
            return;

        if (!CanUseCollectionExpression(context, invocation, "[]") &&
            !CanUseCollectionExpression(context, invocation, "([])"))
            return;

        var arrayType = method.ReturnType.ToMinimalDisplayString(context.SemanticModel, invocation.SpanStart);
        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), arrayType));
    }

    private static bool CanUseCollectionExpression(
        SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, string expression)
    {
        var cancellation = context.CancellationToken;
        // Parse the actual replacement in context. Parentheses can disambiguate
        // a collection expression from an index initializer.
        var tree = invocation.SyntaxTree;
        var editedTree = tree.WithChangedText(tree.GetText(cancellation)
            .WithChanges(new TextChange(invocation.Span, expression)));
        var editedRoot = editedTree.GetRoot(cancellation);
        var replacement = editedRoot.FindNode(
            new TextSpan(invocation.SpanStart, expression.Length), getInnermostNodeForTie: true);
        if (replacement is not (CollectionExpressionSyntax or
            ParenthesizedExpressionSyntax { Expression: CollectionExpressionSyntax }))
            return false;

        // The analyzer context supplies a model for the unedited tree only.
        var editedCompilation = context.Compilation.ReplaceSyntaxTree(tree, editedTree);
#pragma warning disable RS1030
        var editedModel = editedCompilation.GetSemanticModel(editedTree);
#pragma warning restore RS1030
        var region = replacement.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault() ?? editedRoot;
        return !editedModel.GetDiagnostics(region.Span, cancellation)
            .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
