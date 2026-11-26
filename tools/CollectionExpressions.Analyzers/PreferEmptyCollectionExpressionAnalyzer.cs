using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Luchdom.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferEmptyCollectionExpressionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "LUCH0001",
        "Use an empty collection expression",
        "Use [] in place of System.Array.Empty<T>() when the target type is known",
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
            })
            return;

        if (CanUseCollectionExpression(context, invocation))
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }

    private static bool CanUseCollectionExpression(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var cancellation = context.CancellationToken;
        var type = context.SemanticModel.GetTypeInfo(invocation, cancellation).ConvertedType;
        if (type is null || type.TypeKind == TypeKind.Error)
            return false;

        // A collection expression has no natural type. Rebind it in the original context
        // to distinguish compatible calls from var, type inference and expression trees.
        var annotation = new SyntaxAnnotation();
        var tree = invocation.SyntaxTree;
        var editedRoot = tree.GetRoot(cancellation).ReplaceNode(invocation,
            SyntaxFactory.CollectionExpression(default).WithTriviaFrom(invocation).WithAdditionalAnnotations(annotation));
        var editedTree = tree.WithRootAndOptions(editedRoot, tree.Options);
        var editedCompilation = context.Compilation.ReplaceSyntaxTree(tree, editedTree);
        // The analyzer context supplies a model for the unedited tree only.
#pragma warning disable RS1030
        var editedModel = editedCompilation.GetSemanticModel(editedTree);
#pragma warning restore RS1030
        var replacement = (CollectionExpressionSyntax)editedTree.GetRoot(cancellation).GetAnnotatedNodes(annotation).Single();
        var replacementType = editedModel.GetTypeInfo(replacement, cancellation).ConvertedType;
        if (replacementType is null || replacementType.TypeKind == TypeKind.Error ||
            !string.Equals(TypeName(type), TypeName(replacementType), StringComparison.Ordinal))
            return false;

        var region = replacement.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault() ?? editedRoot;
        return !editedModel.GetDiagnostics(region.Span, cancellation).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    private static string TypeName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
