using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Encina.Security.ABAC.Analyzers
{
    /// <summary>
    /// Code fix provider for EEL004 (empty expression) diagnostic.
    /// Offers to replace empty expressions with <c>"true"</c> or <c>"false"</c>.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequireConditionCodeFixProvider))]
    [Shared]
    public sealed class RequireConditionCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("EEL004");

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc />
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the string literal containing the empty expression
            var node = root.FindNode(diagnosticSpan);
            if (node == null)
            {
                return;
            }

            var literalExpression = node as LiteralExpressionSyntax
                ?? node.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();

            if (literalExpression == null ||
                literalExpression.Kind() != SyntaxKind.StringLiteralExpression)
            {
                return;
            }

            // Register fix: replace with "true"
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace with \"true\"",
                    createChangedDocument: ct => ReplaceExpressionAsync(
                        context.Document, literalExpression, "true", ct),
                    equivalenceKey: "EEL004_ReplaceWithTrue"),
                diagnostic);

            // Register fix: replace with "false"
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace with \"false\"",
                    createChangedDocument: ct => ReplaceExpressionAsync(
                        context.Document, literalExpression, "false", ct),
                    equivalenceKey: "EEL004_ReplaceWithFalse"),
                diagnostic);
        }

        private static async Task<Document> ReplaceExpressionAsync(
            Document document,
            LiteralExpressionSyntax oldLiteral,
            string newExpression,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var newLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(newExpression))
                .WithTriviaFrom(oldLiteral);

            var newRoot = root.ReplaceNode(oldLiteral, newLiteral);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
