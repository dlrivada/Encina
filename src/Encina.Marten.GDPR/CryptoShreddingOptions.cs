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
    /// Gets or sets whether a hosted service validates crypto-shredding at startup and stops the host when
    /// anything is wrong.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the validator checks that the store serializer is wrapped and the contract modifier is still
    /// installed (resolver identity and a nested canary), checks the infrastructure (the async daemon does not skip
    /// serialization errors, the erasure router and the Marten locator are not bypassed), and classifies every
    /// type of <see cref="AssembliesToScan"/> that declares or reaches a <c>[CryptoShredded]</c> property, at any
    /// depth, through the System.Text.Json contract. All issues are reported at once in one
    /// <see cref="CryptoShreddingConfigurationException"/>, with a reason per property.
    /// </para>
    /// <para>
    /// Turning it off is an explicit, logged opt-out (event 8462); misconfigured types then fail on first use.
    /// </para>
    /// </remarks>
    /// <value>Defaults to <c>true</c>.</value>
    public bool ValidateOnStartup { get; set; } = true;

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
    /// Gets the list of assemblies the startup validation scans for types with <see cref="CryptoShreddedAttribute"/>.
    /// </summary>
    /// <remarks>
    /// If empty and <see cref="ValidateOnStartup"/> is <c>true</c>, the entry assembly (or calling assembly) is
    /// scanned. Types outside the scanned assemblies are validated by the contract modifier on first use.
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];
}
