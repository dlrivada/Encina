namespace Encina.Compliance.NIS2;

/// <summary>
/// Validates a supplier's security posture before processing the decorated request (Art. 21(2)(d)).
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request class, the <c>NIS2CompliancePipelineBehavior</c> invokes
/// <see cref="Abstractions.ISupplyChainSecurityValidator.ValidateSupplierForOperationAsync"/>
/// for the specified <see cref="SupplierId"/> before executing the request handler. If the
/// supplier's risk level exceeds the acceptable threshold, the behavior either blocks the
/// request or logs a warning, depending on the configured
/// <see cref="Model.NIS2EnforcementMode"/>.
/// </para>
/// <para>
/// Per NIS2 Article 21(2)(d), entities must address "supply chain security, including
/// security-related aspects concerning the relationships between each entity and its
/// direct suppliers or service providers."
/// </para>
/// <para>
/// Multiple instances of this attribute can be applied to a single request to validate
/// multiple suppliers involved in the operation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [NIS2SupplyChainCheck("payment-provider")]
/// public sealed record ProcessExternalPaymentCommand(decimal Amount) : ICommand&lt;Unit&gt;;
///
/// [NIS2SupplyChainCheck("cloud-provider")]
/// [NIS2SupplyChainCheck("data-processor", MinimumRiskLevel = SupplierRiskLevel.Medium)]
/// public sealed record MigrateDataCommand(string DataSetId) : ICommand&lt;Unit&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class NIS2SupplyChainCheckAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NIS2SupplyChainCheckAttribute"/> class.
    /// </summary>
    /// <param name="supplierId">
    /// The unique identifier of the supplier to validate, as registered via
    /// <c>NIS2Options.AddSupplier()</c>.
    /// </param>
    public NIS2SupplyChainCheckAttribute(string supplierId)
    {
        ArgumentNullException.ThrowIfNull(supplierId);
        SupplierId = supplierId;
    }

    /// <summary>
    /// Gets the unique identifier of the supplier to validate.
    /// </summary>
    public string SupplierId { get; }

    /// <summary>
    /// Gets or sets the minimum acceptable risk level for this operation.
    /// </summary>
    /// <remarks>
    /// Suppliers with a risk level equal to or higher than this threshold will cause
    /// the pipeline behavior to block or warn. Default is <see cref="Model.SupplierRiskLevel.Medium"/>,
    /// meaning <see cref="Model.SupplierRiskLevel.High"/> and <see cref="Model.SupplierRiskLevel.Critical"/>
    /// suppliers will be flagged.
    /// </remarks>
    public Model.SupplierRiskLevel MinimumRiskLevel { get; set; } = Model.SupplierRiskLevel.Medium;
}
