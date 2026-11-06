namespace Encina.Audit.Marten.Resolution;

/// <summary>
/// The field of an audit event a resolution applies to. The names follow the read-model properties.
/// </summary>
/// <remarks>
/// Operation trail: <see cref="UserId"/>, <see cref="IpAddress"/>, <see cref="UserAgent"/>,
/// <see cref="RequestPayload"/>, <see cref="ResponsePayload"/>, <see cref="MetadataJson"/>.
/// Read trail: <see cref="UserId"/>, <see cref="Purpose"/>, <see cref="MetadataJson"/>.
/// <see cref="All"/> applies to every field of either trail.
/// </remarks>
public enum AuditResolutionField
{
    /// <summary>Every encrypted field of the event.</summary>
    All = 0,

    /// <summary>The user identifier (both trails).</summary>
    UserId = 1,

    /// <summary>The client IP address (operation trail).</summary>
    IpAddress = 2,

    /// <summary>The client user agent (operation trail).</summary>
    UserAgent = 3,

    /// <summary>The request payload (operation trail).</summary>
    RequestPayload = 4,

    /// <summary>The response payload (operation trail).</summary>
    ResponsePayload = 5,

    /// <summary>The metadata JSON (both trails).</summary>
    MetadataJson = 6,

    /// <summary>The access purpose (read trail).</summary>
    Purpose = 7,
}
