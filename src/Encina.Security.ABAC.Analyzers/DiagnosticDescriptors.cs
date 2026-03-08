using Microsoft.CodeAnalysis;

namespace Encina.Security.ABAC.Analyzers
{
    /// <summary>
    /// Diagnostic descriptors for EEL (Encina Expression Language) analyzers.
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        private const string Category = "Encina.ABAC";

        /// <summary>
        /// EEL001: Expression is not valid C# syntax.
        /// </summary>
        public static readonly DiagnosticDescriptor EEL001_InvalidSyntax = new DiagnosticDescriptor(
            id: "EEL001",
            title: "Expression is not valid C# syntax",
            messageFormat: "EEL expression contains syntax errors: {0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "EEL expressions must be valid C# boolean expressions. Fix the syntax errors to ensure the expression can be compiled at runtime.");

        /// <summary>
        /// EEL002: Unknown top-level identifier (not user/resource/environment/action).
        /// </summary>
        public static readonly DiagnosticDescriptor EEL002_UnknownIdentifier = new DiagnosticDescriptor(
            id: "EEL002",
            title: "Unknown top-level identifier in EEL expression",
            messageFormat: "Unknown identifier '{0}'. EEL expressions can only access: user, resource, environment, action",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "EEL expressions have access to four predefined globals: user, resource, environment, and action. Other top-level identifiers will cause runtime errors.");

        /// <summary>
        /// EEL003: Expression may not return bool.
        /// </summary>
        public static readonly DiagnosticDescriptor EEL003_NonBooleanExpression = new DiagnosticDescriptor(
            id: "EEL003",
            title: "Expression may not return a boolean value",
            messageFormat: "EEL expression may not evaluate to a boolean. Consider adding a comparison operator",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "EEL expressions must evaluate to a boolean value (true/false). Expressions that return other types will fail at runtime.");

        /// <summary>
        /// EEL004: Expression is empty or whitespace.
        /// </summary>
        public static readonly DiagnosticDescriptor EEL004_EmptyExpression = new DiagnosticDescriptor(
            id: "EEL004",
            title: "Expression is empty or whitespace",
            messageFormat: "EEL expression cannot be empty or whitespace",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "EEL expressions must contain a valid C# boolean expression. Empty expressions will always fail at runtime.");

        /// <summary>
        /// EEL005: Expensive operation detected (LINQ, loops).
        /// </summary>
        public static readonly DiagnosticDescriptor EEL005_ExpensiveOperation = new DiagnosticDescriptor(
            id: "EEL005",
            title: "Expensive operation detected in EEL expression",
            messageFormat: "EEL expression contains potentially expensive operation: {0}. Consider simplifying",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "EEL expressions are evaluated on every request. Complex operations like LINQ queries, loops, or method chains may impact performance.");

        /// <summary>
        /// EEL006: Statement syntax detected (semicolons, if/for/while).
        /// </summary>
        public static readonly DiagnosticDescriptor EEL006_StatementSyntax = new DiagnosticDescriptor(
            id: "EEL006",
            title: "Statement syntax detected in EEL expression",
            messageFormat: "EEL expressions must be single expressions, not statements. Found: {0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "EEL expressions must be single C# boolean expressions. Statements (if, for, while, semicolons) are not allowed.");
    }
}
