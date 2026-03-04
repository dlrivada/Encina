using Encina.Compliance.BreachNotification.Model;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Marks a request class for evaluation by the breach detection pipeline behavior.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request class (typically a command), the
/// <c>BreachDetectionPipelineBehavior&lt;TRequest, TResponse&gt;</c> automatically generates
/// a <see cref="SecurityEvent"/> from the request execution context and submits it to
/// the registered <see cref="IBreachDetectionRule"/> implementations for evaluation.
/// </para>
/// <para>
/// The <see cref="EventType"/> property controls which <see cref="SecurityEventType"/> is
/// associated with the generated security event. This allows detection rules to filter
/// and prioritize events based on their type.
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must become "aware" of a breach to trigger
/// the 72-hour notification obligation. By instrumenting request handlers with this
/// attribute, applications can systematically monitor security-relevant operations
/// and detect breaches promptly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Monitor a command for unauthorized data access
/// [BreachMonitored(EventType = SecurityEventType.UnauthorizedAccess)]
/// public sealed record ExportCustomerDataCommand(string CustomerId) : IRequest&lt;ExportResult&gt;;
///
/// // Monitor with default event type (Custom)
/// [BreachMonitored]
/// public sealed record BulkDataTransferCommand(IEnumerable&lt;string&gt; EntityIds) : IRequest&lt;TransferResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BreachMonitoredAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the type of security event to generate from this request.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="SecurityEventType.Custom"/>. Detection rules can use this
    /// value to apply type-specific evaluation logic (e.g., stricter thresholds for
    /// <see cref="SecurityEventType.UnauthorizedAccess"/> events).
    /// </remarks>
    public SecurityEventType EventType { get; set; } = SecurityEventType.Custom;
}
