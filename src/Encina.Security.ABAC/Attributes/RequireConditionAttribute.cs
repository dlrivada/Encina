using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

namespace Encina.Security.ABAC;

/// <summary>
/// Requires the request to satisfy an inline ABAC condition expression written
/// in EEL (Encina Expression Language).
/// </summary>
/// <remarks>
/// <para>
/// EEL expressions are compiled at startup using Roslyn scripting and cached.
/// They provide a concise way to define ABAC conditions directly on request classes
/// without creating separate policy definitions.
/// </para>
/// <para>
/// Expressions have access to subject, resource, action, and environment attributes
/// through a predefined context. The expression must evaluate to a boolean value.
/// </para>
/// <para>
/// The <c>expression</c> parameter is annotated with <see cref="StringSyntaxAttribute"/>
/// and <see cref="LanguageInjectionAttribute"/> to provide IDE support (syntax highlighting,
/// IntelliSense) in Visual Studio and JetBrains Rider respectively.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple role-based condition
/// [RequireCondition("subject.department == 'engineering'")]
/// public sealed record GetCodeReviewQuery(Guid ReviewId) : IQuery&lt;ReviewDto&gt;;
///
/// // Time-based condition
/// [RequireCondition("environment.isBusinessHours == true")]
/// public sealed record ProcessPayrollCommand(Guid PayrollId) : ICommand;
///
/// // Complex condition with multiple attributes
/// [RequireCondition("subject.clearanceLevel >= resource.classification")]
/// public sealed record GetClassifiedDocumentQuery(Guid DocumentId) : IQuery&lt;DocumentDto&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequireConditionAttribute : SecurityAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireConditionAttribute"/> class.
    /// </summary>
    /// <param name="expression">The EEL (Encina Expression Language) condition expression.</param>
    public RequireConditionAttribute(
        [StringSyntax("csharp")]
        [LanguageInjection("csharp")]
        string expression)
    {
        Expression = expression;
    }

    /// <summary>
    /// Gets the EEL condition expression to evaluate.
    /// </summary>
    /// <value>
    /// A string expression that must evaluate to <c>true</c> for the request to be authorized.
    /// </value>
    public string Expression { get; }
}
