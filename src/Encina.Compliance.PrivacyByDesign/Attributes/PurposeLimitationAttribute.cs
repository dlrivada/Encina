namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Declares the processing purpose for which a property is collected and may be used.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a property of a request type, the pipeline behavior validates that the
/// field is only used within the scope of its declared purpose. If the request's overall
/// purpose (declared via <see cref="EnforceDataMinimizationAttribute.Purpose"/>) does not
/// match this field's purpose, a <see cref="Model.PrivacyViolationType.PurposeLimitation"/>
/// violation is reported.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(b), personal data shall be "collected for specified, explicit and
/// legitimate purposes and not further processed in a manner that is incompatible with those
/// purposes." This attribute enforces purpose limitation at the field level.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EnforceDataMinimization(Purpose = "Order Processing")]
/// public sealed record CreateOrderCommand(
///     string ProductId,
///     int Quantity,
///     [property: PurposeLimitation("Order Processing")]
///     string ShippingAddress,
///     [property: PurposeLimitation("Marketing Analytics")]
///     string? CampaignCode) : ICommand&lt;OrderId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PurposeLimitationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PurposeLimitationAttribute"/> class
    /// with the specified processing purpose.
    /// </summary>
    /// <param name="purpose">The identifier of the processing purpose for which this field is collected.</param>
    public PurposeLimitationAttribute(string purpose)
    {
        Purpose = purpose;
    }

    /// <summary>
    /// Gets the processing purpose for which this field is collected.
    /// </summary>
    /// <remarks>
    /// Must match a <see cref="Model.PurposeDefinition.PurposeId"/> registered in the system.
    /// When this purpose does not match the request's overall purpose, a purpose limitation
    /// violation is reported.
    /// </remarks>
    public string Purpose { get; }
}
