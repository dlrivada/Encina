using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

namespace Encina.Security.ABAC;

/// <summary>
/// Requires the request to satisfy an inline ABAC condition expression written
/// in EEL (Encina Expression Language).
/// </summary>
/// <remarks>
/// <para>
/// EEL expressions are compiled with Roslyn scripting and cached: at startup when
/// <see cref="ABACOptions.ValidateExpressionsAtStartup"/> scans the request's assembly, otherwise
/// on first use. They provide a concise way to define ABAC conditions directly on request classes
/// without creating separate policy definitions.
/// </para>
/// <para>
/// The ABAC pipeline behavior evaluates every expression for each request, against the attributes
/// collected for it, exposed as <c>user</c> (subject), <c>resource</c>, <c>environment</c> and
/// <c>action</c> (with <c>action.name</c> set to the request type name). The expression must
/// evaluate to a boolean value. All expressions on a request must be <c>true</c> (AND), checked in
/// declaration order: <c>false</c> denies the request with <see cref="ABACErrors.ConditionNotMetCode"/>;
/// an expression that does not compile or throws (for example, a missing attribute) is
/// Indeterminate and denies the request.
/// </para>
/// <para>
/// With only <see cref="RequireConditionAttribute"/> on a request, the conditions alone decide it
/// and no policy is evaluated. With <see cref="RequirePolicyAttribute"/> as well, the policies and
/// the conditions combine with AND: the request proceeds only when every requirement passes.
/// </para>
/// <para>
/// The <c>expression</c> parameter is annotated with <see cref="StringSyntaxAttribute"/>
/// and <see cref="LanguageInjectionAttribute"/> to provide IDE support (syntax highlighting,
/// IntelliSense) in Visual Studio and JetBrains Rider respectively.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple subject-based condition
/// [RequireCondition("user.department == \"engineering\"")]
/// public sealed record GetCodeReviewQuery(Guid ReviewId) : IQuery&lt;ReviewDto&gt;;
///
/// // Time-based condition
/// [RequireCondition("environment.isBusinessHours == true")]
/// public sealed record ProcessPayrollCommand(Guid PayrollId) : ICommand;
///
/// // Complex condition with multiple attributes
/// [RequireCondition("user.clearanceLevel >= resource.classification")]
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
