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
    private readonly Dictionary<string, string> _metadata = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string? Purpose { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    /// <inheritdoc />
    public IReadAuditContext WithPurpose(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        Purpose = purpose;
        return this;
    }

    /// <inheritdoc />
    public IReadAuditContext WithMetadata(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _metadata[key] = value;
        return this;
    }
}
