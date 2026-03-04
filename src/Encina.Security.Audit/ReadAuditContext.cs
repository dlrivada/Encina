namespace Encina.Security.Audit;

/// <summary>
/// Default scoped implementation of <see cref="IReadAuditContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a scoped service via <c>AddEncinaReadAuditing()</c>.
/// Each HTTP request or DI scope gets its own instance, so purposes
/// set in one request do not leak into another.
/// </para>
/// </remarks>
public sealed class ReadAuditContext : IReadAuditContext
{
    /// <inheritdoc />
    public string? Purpose { get; private set; }

    /// <inheritdoc />
    public IReadAuditContext WithPurpose(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        Purpose = purpose;
        return this;
    }
}
