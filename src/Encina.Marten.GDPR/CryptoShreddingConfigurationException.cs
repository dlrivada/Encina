namespace Encina.Marten.GDPR;

/// <summary>
/// What is wrong with the crypto-shredding model or its wiring.
/// </summary>
public enum CryptoShreddingConfigurationProblem
{
    /// <summary>One or more <c>[CryptoShredded]</c> properties, or types on the crypto graph, are misconfigured; see <see cref="CryptoShreddingConfigurationException.Issues"/>.</summary>
    MisconfiguredProperties = 0,

    /// <summary>The Marten serializer is not Marten's System.Text.Json serializer (Newtonsoft is not supported).</summary>
    SerializerNotSupported = 1,

    /// <summary>The store serializer is not a <see cref="CryptoShredderSerializer"/>.</summary>
    SerializerNotWrapped = 2,

    /// <summary>The contract modifier is not installed, was replaced, or a resolver placed ahead of it produced a contract.</summary>
    ContractModifierMissing = 3,

    /// <summary>Marten's async daemon skips serialization errors, so a decryption failure would be dead-lettered instead of pausing the shard.</summary>
    ProjectionSkipsSerializationErrors = 4,

    /// <summary>The resolved <c>IDataErasureStrategy</c> is not the crypto-shredding router: a strategy was registered after <c>AddEncinaMartenGdpr</c>.</summary>
    ErasureStrategyBypassed = 5,

    /// <summary>The resolved <c>IPersonalDataLocator</c> leaves the Marten locator out of data subject requests.</summary>
    PersonalDataLocatorBypassed = 6,
}

/// <summary>
/// Thrown when the crypto-shredding model or its wiring is wrong: a misconfigured <c>[CryptoShredded]</c>
/// property, an unsupported serializer, a missing or bypassed contract modifier, or an infrastructure setting
/// that would let personal data escape encryption or erasure.
/// </summary>
/// <remarks>
/// <para>
/// The startup validator throws it so the host does not start; the contract modifier throws it on the first
/// use of a misconfigured type, before any byte is written, and System.Text.Json caches that failure for the
/// type. Fix the code or the registration: this is not a transient error.
/// </para>
/// <para>
/// The message lists type names, property names and problem flags only. It never carries a value, a subject
/// id or the message of another exception.
/// </para>
/// </remarks>
public sealed class CryptoShreddingConfigurationException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoShreddingConfigurationException"/> class.
    /// </summary>
    /// <param name="problem">What is wrong.</param>
    /// <param name="issues">The per-property issues; empty for an infrastructure problem.</param>
    /// <param name="componentType">The serializer, strategy or locator type involved, for an infrastructure problem.</param>
    public CryptoShreddingConfigurationException(
        CryptoShreddingConfigurationProblem problem,
        IReadOnlyList<CryptoShreddedPropertyIssue> issues,
        string? componentType = null)
        : base(BuildMessage(problem, issues, componentType))
    {
        Problem = problem;
        Issues = issues;
        ComponentType = componentType;
    }

    /// <summary>Gets what is wrong.</summary>
    public CryptoShreddingConfigurationProblem Problem { get; }

    /// <summary>Gets every misconfigured property with its reasons.</summary>
    public IReadOnlyList<CryptoShreddedPropertyIssue> Issues { get; }

    /// <summary>Gets the component type name involved in an infrastructure problem, or <c>null</c>.</summary>
    public string? ComponentType { get; }

    private static string BuildMessage(
        CryptoShreddingConfigurationProblem problem, IReadOnlyList<CryptoShreddedPropertyIssue> issues, string? componentType)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var component = componentType is null ? string.Empty : $" ({componentType})";
        var details = issues.Count == 0
            ? string.Empty
            : " " + string.Join(" | ", issues.Select(issue => issue.Describe()));
        return $"Crypto-shredding is misconfigured: {problem}{component}.{details}";
    }
}
