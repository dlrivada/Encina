using System.Reflection;

namespace Encina.Marten.GDPR;

/// <summary>
/// Configuration options for the Marten crypto-shredding subsystem.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the <see cref="CryptoShredderSerializer"/> and
/// associated infrastructure including key provider selection, auto-registration, and
/// health monitoring.
/// </para>
/// <para>
/// Configure via <c>AddEncinaMartenGdpr</c>:
/// <code>
/// services.AddEncinaMartenGdpr(options =>
/// {
///     options.UsePostgreSqlKeyStore = true;
///     options.KeyRotationDays = 90;
///     options.AddHealthCheck = true;
///     options.AssembliesToScan.Add(typeof(Program).Assembly);
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class CryptoShreddingOptions
{
    /// <summary>
    /// Gets or sets the placeholder value substituted for PII of forgotten data subjects.
    /// </summary>
    /// <remarks>
    /// When a data subject has been cryptographically forgotten (keys deleted) and their
    /// encrypted PII is encountered during deserialization, this placeholder replaces the
    /// unrecoverable field value.
    /// </remarks>
    /// <value>Defaults to <c>"[REDACTED]"</c>.</value>
    public string AnonymizedPlaceholder { get; set; } = CryptoShredderSerializerFactory.DefaultAnonymizedPlaceholder;

    /// <summary>
    /// Gets or sets whether to scan assemblies at startup for <see cref="CryptoShreddedAttribute"/>
    /// decorations and validate their configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, a hosted service scans <see cref="AssembliesToScan"/> for event types
    /// with <c>[CryptoShredded]</c> properties and validates that:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Each property also has <c>[PersonalData]</c></description></item>
    /// <item><description>The <c>SubjectIdProperty</c> references a valid property on the declaring type</description></item>
    /// </list>
    /// </remarks>
    /// <value>Defaults to <c>true</c>.</value>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register a health check for the crypto-shredding subsystem.
    /// </summary>
    /// <value>Defaults to <c>false</c>.</value>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether to publish domain events for crypto-shredding operations.
    /// </summary>
    /// <remarks>
    /// When enabled, events such as <see cref="SubjectForgottenEvent"/> and
    /// <see cref="SubjectKeyRotatedEvent"/> are published during key lifecycle operations.
    /// </remarks>
    /// <value>Defaults to <c>true</c>.</value>
    public bool PublishEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the recommended key rotation interval in days.
    /// </summary>
    /// <remarks>
    /// This value is informational and used by health checks and diagnostics to warn
    /// when subject keys exceed this age without rotation. Actual rotation is triggered
    /// by calling <see cref="Abstractions.ISubjectKeyProvider.RotateSubjectKeyAsync"/>.
    /// </remarks>
    /// <value>Defaults to <c>90</c> days.</value>
    public int KeyRotationDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets whether to use PostgreSQL-backed key storage instead of in-memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>false</c> (default), uses <see cref="InMemorySubjectKeyProvider"/> which is
    /// suitable for testing and development. Keys are lost when the process restarts.
    /// </para>
    /// <para>
    /// When <c>true</c>, uses <see cref="PostgreSqlSubjectKeyProvider"/> which persists keys
    /// in Marten's PostgreSQL document store. <b>Required for production use.</b>
    /// </para>
    /// </remarks>
    /// <value>Defaults to <c>false</c>.</value>
    public bool UsePostgreSqlKeyStore { get; set; }

    /// <summary>
    /// Gets the list of assemblies to scan for event types with <see cref="CryptoShreddedAttribute"/>.
    /// </summary>
    /// <remarks>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>, the entry
    /// assembly (or calling assembly) is scanned by default.
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];
}
