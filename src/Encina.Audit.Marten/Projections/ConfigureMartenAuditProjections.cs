using Marten;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Audit.Marten.Projections;

/// <summary>
/// Configures Marten <see cref="StoreOptions"/> to register audit trail async projections
/// and document indexes for efficient querying.
/// </summary>
/// <remarks>
/// <para>
/// This class is registered as <see cref="IConfigureOptions{StoreOptions}"/> via DI
/// and is invoked during Marten store initialization. It registers:
/// <list type="bullet">
/// <item><see cref="AuditEntryProjection"/> — async projection for write audit entries</item>
/// <item><see cref="ReadAuditEntryProjection"/> — async projection for read audit entries</item>
/// <item>JSONB indexes on commonly queried fields for both read model types</item>
/// </list>
/// </para>
/// <para>
/// Both projections are registered as <b>async</b>, meaning they are processed by Marten's
/// <c>AsyncProjectionDaemon</c> independently of the write path. This provides:
/// <list type="bullet">
/// <item>Better write performance (no projection overhead on <c>SaveChangesAsync</c>)</item>
/// <item>Fault isolation (projection failure doesn't block event appends)</item>
/// <item>Independent rebuild capability</item>
/// <item>Eventual consistency (typically milliseconds to low seconds)</item>
/// </list>
/// </para>
/// <para>
/// Projection instances are constructed here (not by Marten) so that configured options
/// (<see cref="MartenAuditOptions.ShreddedPlaceholder"/>) and loggers can be injected via
/// their constructors. This avoids Marten's projection validator rejecting
/// <see cref="IServiceProvider"/> parameters on <c>Create</c> methods.
/// </para>
/// </remarks>
internal sealed class ConfigureMartenAuditProjections : IConfigureOptions<StoreOptions>
{
    private readonly IOptions<MartenAuditOptions> _auditOptions;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureMartenAuditProjections"/> class.
    /// </summary>
    /// <param name="auditOptions">Configured Marten audit options.</param>
    /// <param name="loggerFactory">Logger factory for creating projection loggers.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="auditOptions"/> or <paramref name="loggerFactory"/> is <c>null</c>.
    /// </exception>
    public ConfigureMartenAuditProjections(
        IOptions<MartenAuditOptions> auditOptions,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(auditOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _auditOptions = auditOptions;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public void Configure(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var placeholder = _auditOptions.Value.ShreddedPlaceholder;

        // Register async projections processed by Marten's AsyncProjectionDaemon.
        // Projections are constructed with explicit dependencies so Marten's validator
        // only sees parameters it supports (IDocumentOperations, CancellationToken, event types).
        options.Projections.Add(
            new AuditEntryProjection(placeholder, _loggerFactory.CreateLogger<AuditEntryProjection>()),
            JasperFx.Events.Projections.ProjectionLifecycle.Async);

        options.Projections.Add(
            new ReadAuditEntryProjection(placeholder, _loggerFactory.CreateLogger<ReadAuditEntryProjection>()),
            JasperFx.Events.Projections.ProjectionLifecycle.Async);

        // Configure indexes for AuditEntryReadModel
        options.Schema.For<AuditEntryReadModel>()
            .Index(x => x.TimestampUtc)
            .Index(x => x.EntityType)
            .Index(x => x.EntityId)
            .Index(x => x.UserId)
            .Index(x => x.TenantId)
            .Index(x => x.CorrelationId)
            .Index(x => x.Action)
            .Index(x => x.Outcome)
            .Index(x => x.TemporalKeyPeriod);

        // Configure indexes for ReadAuditEntryReadModel
        options.Schema.For<ReadAuditEntryReadModel>()
            .Index(x => x.AccessedAtUtc)
            .Index(x => x.EntityType)
            .Index(x => x.EntityId)
            .Index(x => x.UserId)
            .Index(x => x.TenantId)
            .Index(x => x.AccessMethod)
            .Index(x => x.TemporalKeyPeriod);
    }
}
