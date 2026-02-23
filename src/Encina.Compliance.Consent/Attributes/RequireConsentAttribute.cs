namespace Encina.Compliance.Consent;

/// <summary>
/// Declares that a request type requires consent for one or more processing purposes
/// before it can be executed.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used by <see cref="ConsentRequiredPipelineBehavior{TRequest, TResponse}"/>
/// to enforce consent validation in the Encina pipeline. When a request decorated with this
/// attribute is dispatched, the pipeline verifies that the data subject has given valid consent
/// for all specified purposes before the handler executes.
/// </para>
/// <para>
/// The data subject is identified either by the property specified in <see cref="SubjectIdProperty"/>
/// (using cached reflection) or by falling back to <c>IRequestContext.UserId</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Single purpose
/// [RequireConsent(ConsentPurposes.Marketing)]
/// public sealed record SendMarketingEmailCommand : ICommand&lt;Unit&gt;;
///
/// // Multiple purposes
/// [RequireConsent(ConsentPurposes.Analytics, ConsentPurposes.Personalization)]
/// public sealed record TrackUserBehaviorCommand : ICommand&lt;Unit&gt;;
///
/// // With custom subject ID property and error message
/// [RequireConsent(ConsentPurposes.Profiling,
///     SubjectIdProperty = "CustomerId",
///     ErrorMessage = "Customer has not consented to profiling")]
/// public sealed record GenerateRecommendationsQuery(string CustomerId) : IQuery&lt;Recommendations&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireConsentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireConsentAttribute"/> class
    /// with one or more required consent purposes.
    /// </summary>
    /// <param name="purposes">
    /// One or more processing purposes that require consent.
    /// Use constants from <see cref="ConsentPurposes"/> for standard purposes.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when no purposes are specified.</exception>
    public RequireConsentAttribute(params string[] purposes)
    {
        ArgumentNullException.ThrowIfNull(purposes);

        if (purposes.Length == 0)
        {
            throw new ArgumentException("At least one consent purpose must be specified.", nameof(purposes));
        }

        Purposes = purposes;
    }

    /// <summary>
    /// Gets the processing purposes that require consent.
    /// </summary>
    public string[] Purposes { get; }

    /// <summary>
    /// Gets or sets the name of the request property that contains the data subject identifier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When specified, the pipeline behavior uses cached reflection to extract the subject ID
    /// from the request instance. When <c>null</c>, the behavior falls back to
    /// <c>IRequestContext.UserId</c>.
    /// </para>
    /// <para>
    /// The property must be a public readable property that returns a <see cref="string"/>.
    /// </para>
    /// </remarks>
    /// <example>"CustomerId", "UserId", "SubjectId"</example>
    public string? SubjectIdProperty { get; set; }

    /// <summary>
    /// Gets or sets a custom error message to use when consent validation fails.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, a default error message is generated that includes the subject ID
    /// and missing purposes. Custom messages are useful for providing domain-specific
    /// guidance to the caller.
    /// </remarks>
    public string? ErrorMessage { get; set; }
}
