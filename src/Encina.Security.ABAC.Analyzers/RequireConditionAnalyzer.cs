using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Encina.Security.ABAC.Analyzers
{
    /// <summary>
    /// Roslyn diagnostic analyzer that validates EEL (Encina Expression Language) expressions
    /// used in <c>[RequireCondition]</c> attribute arguments at compile time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This analyzer performs lightweight syntax-level checks without full Roslyn scripting
    /// compilation (which would be too heavy for IDE use). It catches common errors early:
    /// </para>
    /// <list type="bullet">
    /// <item><description>EEL001: Invalid C# syntax</description></item>
    /// <item><description>EEL002: Unknown top-level identifiers</description></item>
    /// <item><description>EEL003: Non-boolean expressions</description></item>
    /// <item><description>EEL004: Empty expressions</description></item>
    /// <item><description>EEL005: Expensive operations</description></item>
    /// <item><description>EEL006: Statement syntax</description></item>
    /// </list>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RequireConditionAnalyzer : DiagnosticAnalyzer
    {
        private const string RequireConditionAttributeName = "RequireConditionAttribute";
        private const string RequireConditionAttributeShortName = "RequireCondition";
        private const string RequireConditionFullName = "Encina.Security.ABAC.RequireConditionAttribute";

        /// <summary>
        /// Valid top-level identifiers in EEL expressions.
        /// </summary>
        private static readonly HashSet<string> ValidIdentifiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "user", "resource", "environment", "action"
        };

        /// <summary>
        /// Keywords that indicate statement syntax rather than expressions.
        /// </summary>
        private static readonly HashSet<string> StatementKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "if", "else", "for", "foreach", "while", "do", "switch", "try", "catch",
            "finally", "throw", "return", "var", "using", "lock", "yield", "class",
            "struct", "interface", "enum", "namespace", "delegate"
        };

        /// <summary>
        /// Method names that suggest expensive operations.
        /// </summary>
        private static readonly HashSet<string> ExpensiveMethodNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Select", "Where", "OrderBy", "OrderByDescending", "GroupBy",
            "Join", "Aggregate", "ToList", "ToArray", "ToDictionary",
            "FirstOrDefault", "LastOrDefault", "SingleOrDefault",
            "Any", "All", "Count", "Sum", "Average", "Min", "Max"
        };

        /// <summary>
        /// Well-known non-boolean types that can be identified by simple name patterns.
        /// </summary>
        private static readonly HashSet<SyntaxKind> BooleanOperatorKinds = new HashSet<SyntaxKind>
        {
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.GreaterThanExpression,
            SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.LessThanExpression,
            SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.LogicalAndExpression,
            SyntaxKind.LogicalOrExpression,
            SyntaxKind.LogicalNotExpression,
            SyntaxKind.IsExpression,
            SyntaxKind.IsPatternExpression,
        };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DiagnosticDescriptors.EEL001_InvalidSyntax,
                DiagnosticDescriptors.EEL002_UnknownIdentifier,
                DiagnosticDescriptors.EEL003_NonBooleanExpression,
                DiagnosticDescriptors.EEL004_EmptyExpression,
                DiagnosticDescriptors.EEL005_ExpensiveOperation,
                DiagnosticDescriptors.EEL006_StatementSyntax);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var attributeSyntax = (AttributeSyntax)context.Node;

            // Check if this is a RequireCondition attribute
            if (!IsRequireConditionAttribute(attributeSyntax, context.SemanticModel))
            {
                return;
            }

            // Get the expression argument
            var expressionArgument = GetExpressionArgument(attributeSyntax);
            if (expressionArgument == null)
            {
                return;
            }

            // Get the constant string value
            var constantValue = context.SemanticModel.GetConstantValue(expressionArgument);
            if (!constantValue.HasValue || !(constantValue.Value is string expressionText))
            {
                return;
            }

            var location = expressionArgument.GetLocation();

            // EEL004: Empty expression
            if (string.IsNullOrWhiteSpace(expressionText))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EEL004_EmptyExpression,
                    location));
                return;
            }

            // EEL006: Statement syntax (check for semicolons and statement keywords)
            if (ContainsStatementSyntax(expressionText, out var statementDetail))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EEL006_StatementSyntax,
                    location,
                    statementDetail));
                return;
            }

            // Parse as C# expression for further analysis
            var parsed = SyntaxFactory.ParseExpression(expressionText);

            // EEL001: Syntax errors
            var diagnostics = parsed.GetDiagnostics().ToList();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                var errors = string.Join("; ", diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage()));

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EEL001_InvalidSyntax,
                    location,
                    errors));
                return;
            }

            // EEL002: Unknown identifiers
            CheckForUnknownIdentifiers(context, parsed, location);

            // EEL003: Non-boolean expression (heuristic)
            CheckForNonBooleanExpression(context, parsed, location);

            // EEL005: Expensive operations
            CheckForExpensiveOperations(context, parsed, location);
        }

        private static bool IsRequireConditionAttribute(
            AttributeSyntax attributeSyntax,
            SemanticModel semanticModel)
        {
            var name = attributeSyntax.Name;
            var nameText = name.ToString();

            // Quick name check before expensive semantic model lookup
            if (nameText != RequireConditionAttributeShortName &&
                nameText != RequireConditionAttributeName &&
                !nameText.EndsWith("." + RequireConditionAttributeShortName, StringComparison.Ordinal) &&
                !nameText.EndsWith("." + RequireConditionAttributeName, StringComparison.Ordinal))
            {
                return false;
            }

            // Use semantic model for definitive check
            var symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            if (symbol is IMethodSymbol methodSymbol)
            {
                return methodSymbol.ContainingType.ToDisplayString() == RequireConditionFullName;
            }

            // Fallback: name-based matching when semantic model can't resolve
            return true;
        }

        private static ExpressionSyntax? GetExpressionArgument(AttributeSyntax attributeSyntax)
        {
            var argumentList = attributeSyntax.ArgumentList;
            if (argumentList == null || argumentList.Arguments.Count == 0)
            {
                return null;
            }

            // First positional argument is the expression
            return argumentList.Arguments[0].Expression;
        }

        private static bool ContainsStatementSyntax(string expression, out string detail)
        {
            // Check for semicolons (statement separator)
            if (expression.Contains(";"))
            {
                detail = "semicolon (;)";
                return true;
            }

            // Check for statement keywords at word boundaries
            foreach (var keyword in StatementKeywords)
            {
                var index = expression.IndexOf(keyword, StringComparison.Ordinal);
                if (index < 0)
                {
                    continue;
                }

                // Ensure it's a word boundary (not part of a larger identifier)
                var beforeOk = index == 0 || !char.IsLetterOrDigit(expression[index - 1]);
                var afterIndex = index + keyword.Length;
                var afterOk = afterIndex >= expression.Length ||
                              !char.IsLetterOrDigit(expression[afterIndex]);

                if (beforeOk && afterOk)
                {
                    detail = $"'{keyword}' keyword";
                    return true;
                }
            }

            detail = string.Empty;
            return false;
        }

        private static void CheckForUnknownIdentifiers(
            SyntaxNodeAnalysisContext context,
            ExpressionSyntax parsed,
            Location location)
        {
            // Find top-level identifier names (left-most in member access chains)
            foreach (var node in parsed.DescendantNodesAndSelf())
            {
                if (node is MemberAccessExpressionSyntax memberAccess)
                {
                    // Get the root of the member access chain
                    var root = GetMemberAccessRoot(memberAccess);
                    if (root is IdentifierNameSyntax identifier &&
                        !ValidIdentifiers.Contains(identifier.Identifier.Text))
                    {
                        // Skip well-known type names and common expressions
                        var text = identifier.Identifier.Text;
                        if (!IsWellKnownTypeName(text))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.EEL002_UnknownIdentifier,
                                location,
                                text));
                        }
                    }
                }
            }
        }

        private static ExpressionSyntax GetMemberAccessRoot(MemberAccessExpressionSyntax memberAccess)
        {
            var current = memberAccess.Expression;
            while (current is MemberAccessExpressionSyntax nested)
            {
                current = nested.Expression;
            }

            return current;
        }

        private static bool IsWellKnownTypeName(string name)
        {
            // Common type names that might appear in expressions
            return name == "true" || name == "false" || name == "null" ||
                   name == "string" || name == "int" || name == "bool" ||
                   name == "DateTime" || name == "TimeSpan" || name == "Guid" ||
                   name == "Math" || name == "Convert" || name == "Enumerable" ||
                   name == "String" || name == "Int32" || name == "Boolean";
        }

        private static void CheckForNonBooleanExpression(
            SyntaxNodeAnalysisContext context,
            ExpressionSyntax parsed,
            Location location)
        {
            // Heuristic: if the top-level expression is not a boolean operator,
            // boolean literal, or invocation (which could return bool), warn.
            if (IsProbablyBoolean(parsed))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.EEL003_NonBooleanExpression,
                location));
        }

        private static bool IsProbablyBoolean(ExpressionSyntax expression)
        {
            // Boolean literals
            if (expression is LiteralExpressionSyntax literal)
            {
                return literal.Kind() == SyntaxKind.TrueLiteralExpression ||
                       literal.Kind() == SyntaxKind.FalseLiteralExpression;
            }

            // Binary expressions with boolean operators
            if (expression is BinaryExpressionSyntax binary)
            {
                return BooleanOperatorKinds.Contains(binary.Kind());
            }

            // Prefix unary (! operator)
            if (expression is PrefixUnaryExpressionSyntax prefix)
            {
                return prefix.Kind() == SyntaxKind.LogicalNotExpression;
            }

            // Parenthesized expression — recurse
            if (expression is ParenthesizedExpressionSyntax paren)
            {
                return IsProbablyBoolean(paren.Expression);
            }

            // Invocations could return bool
            if (expression is InvocationExpressionSyntax)
            {
                return true;
            }

            // Conditional expression (ternary) — check both branches
            if (expression is ConditionalExpressionSyntax conditional)
            {
                return IsProbablyBoolean(conditional.WhenTrue) ||
                       IsProbablyBoolean(conditional.WhenFalse);
            }

            // is pattern
            if (expression is IsPatternExpressionSyntax)
            {
                return true;
            }

            // Member access could be a bool property (user.isAdmin)
            if (expression is MemberAccessExpressionSyntax)
            {
                return true;
            }

            // Standalone identifier could be a bool variable
            if (expression is IdentifierNameSyntax)
            {
                return true;
            }

            return false;
        }

        private static void CheckForExpensiveOperations(
            SyntaxNodeAnalysisContext context,
            ExpressionSyntax parsed,
            Location location)
        {
            foreach (var node in parsed.DescendantNodesAndSelf())
            {
                // Check for expensive LINQ methods
                if (node is MemberAccessExpressionSyntax memberAccess &&
                    ExpensiveMethodNames.Contains(memberAccess.Name.Identifier.Text))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.EEL005_ExpensiveOperation,
                        location,
                        memberAccess.Name.Identifier.Text));
                    return;
                }

                // Check for lambda expressions
                if (node is LambdaExpressionSyntax)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.EEL005_ExpensiveOperation,
                        location,
                        "lambda expression"));
                    return;
                }

                // Check for object creation
                if (node is ObjectCreationExpressionSyntax)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.EEL005_ExpensiveOperation,
                        location,
                        "object creation (new)"));
                    return;
                }
            }
        }
    }
}
