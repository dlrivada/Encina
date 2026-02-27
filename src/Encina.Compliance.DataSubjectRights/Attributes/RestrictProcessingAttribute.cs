namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Marker attribute indicating that a request should be subject to processing restriction checks (Article 18).
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request type, the <c>RestrictionCheckPipelineBehavior</c> will verify whether
/// the data subject has an active processing restriction before allowing the request to proceed.
/// If a restriction is active, the pipeline will return a <see cref="DSRErrors.RestrictionActive"/> error.
/// </para>
/// <para>
/// Per Article 18(2), while restriction is active, personal data may only be stored — not processed —
/// except with the data subject's consent, for legal claims, for protecting rights of another person,
/// or for reasons of important public interest.
/// </para>
/// <para>
/// The <see cref="SubjectIdProperty"/> indicates which property of the request contains the data
/// subject identifier. If not specified, the pipeline behavior will use the registered
/// <see cref="IDataSubjectIdExtractor"/> to determine the subject ID.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Request that should check for processing restrictions
/// [RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
/// public record UpdateCustomerProfileCommand(string CustomerId, string NewEmail) : ICommand;
///
/// // Using the default subject ID extraction (via IDataSubjectIdExtractor)
/// [RestrictProcessing]
/// public record SendMarketingEmailCommand(string SubjectId) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RestrictProcessingAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the property on the request that contains the data subject identifier.
    /// </summary>
    /// <remarks>
    /// When specified, the pipeline behavior uses reflection to read this property value as the
    /// subject ID. When <c>null</c>, the behavior falls back to the registered
    /// <see cref="IDataSubjectIdExtractor"/> for subject ID resolution.
    /// </remarks>
    public string? SubjectIdProperty { get; set; }
}
