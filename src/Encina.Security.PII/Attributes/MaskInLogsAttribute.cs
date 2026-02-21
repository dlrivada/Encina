namespace Encina.Security.PII.Attributes;

/// <summary>
/// Marks a property as requiring masking only in logging contexts.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="PIIAttribute"/> and <see cref="SensitiveDataAttribute"/>, this attribute
/// only triggers masking when the object is being serialized for logging purposes.
/// The value remains unmasked in API responses and database storage.
/// </para>
/// <para>
/// This is useful for data that is not considered PII but should not appear in plain text
/// in log files for operational security reasons (e.g., internal IDs, correlation tokens,
/// or system-level identifiers).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record ProcessOrderCommand(
///     Guid OrderId,
///     [property: MaskInLogs] string InternalTrackingId,
///     [property: MaskInLogs(MaskingMode.Hash)] string CorrelationKey
/// ) : ICommand&lt;OrderResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MaskInLogsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaskInLogsAttribute"/> class
    /// with the default masking mode (<see cref="MaskingMode.Partial"/>).
    /// </summary>
    public MaskInLogsAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaskInLogsAttribute"/> class
    /// with the specified masking mode.
    /// </summary>
    /// <param name="mode">The masking mode to apply in logging contexts.</param>
    public MaskInLogsAttribute(MaskingMode mode)
    {
        Mode = mode;
    }

    /// <summary>
    /// Gets or sets the masking mode to apply in logging contexts.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="MaskingMode.Partial"/>. Override to use a different
    /// mode for log output.
    /// </remarks>
    public MaskingMode Mode { get; set; } = MaskingMode.Partial;
}
