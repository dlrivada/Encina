namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 §B.8 — Standard status code identifiers used in
/// <see cref="DecisionStatus.StatusCode"/>.
/// </summary>
/// <remarks>
/// Status codes indicate the outcome of policy evaluation. They are included in the
/// <see cref="PolicyDecision.Status"/> when additional diagnostic information is
/// needed, typically for Indeterminate results.
/// </remarks>
public static class XACMLStatusCodes
{
    /// <summary>
    /// Evaluation completed successfully.
    /// </summary>
    public const string Ok = "urn:oasis:names:tc:xacml:1.0:status:ok";

    /// <summary>
    /// A required attribute was missing during evaluation.
    /// </summary>
    public const string MissingAttribute = "urn:oasis:names:tc:xacml:1.0:status:missing-attribute";

    /// <summary>
    /// A syntax error was encountered in a policy or condition expression.
    /// </summary>
    public const string SyntaxError = "urn:oasis:names:tc:xacml:1.0:status:syntax-error";

    /// <summary>
    /// A processing error occurred during evaluation (e.g., function error, type mismatch).
    /// </summary>
    public const string ProcessingError = "urn:oasis:names:tc:xacml:1.0:status:processing-error";
}
