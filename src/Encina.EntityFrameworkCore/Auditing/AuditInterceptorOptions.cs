namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Configuration options for the audit interceptor.
/// </summary>
/// <remarks>
/// <para>
/// These options control how auditable entities are automatically populated
/// when <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> is called.
/// </para>
/// <para>
/// <b>Design Philosophy</b>: All options are enabled by default to provide comprehensive
/// audit tracking out of the box. Disable specific options if you need manual control
/// over certain audit fields.
/// </para>
/// <para>
/// <b>Supported Interfaces</b>: The interceptor detects entities implementing the granular
/// interfaces from <c>Encina.DomainModeling</c>:
/// <list type="bullet">
/// <item><description><see cref="Encina.DomainModeling.ICreatedAtUtc"/>: Track creation timestamp</description></item>
/// <item><description><see cref="Encina.DomainModeling.ICreatedBy"/>: Track who created the entity</description></item>
/// <item><description><see cref="Encina.DomainModeling.IModifiedAtUtc"/>: Track modification timestamp</description></item>
/// <item><description><see cref="Encina.DomainModeling.IModifiedBy"/>: Track who modified the entity</description></item>
/// <item><description><see cref="Encina.DomainModeling.IAuditableEntity"/>: Combined interface for all four</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseAuditing = true;
///     config.AuditingOptions.TrackCreatedBy = true;
///     config.AuditingOptions.TrackModifiedBy = true;
///     config.AuditingOptions.LogAuditChanges = true;
/// });
/// </code>
/// </example>
public sealed class AuditInterceptorOptions
{
    /// <summary>
    /// Gets or sets whether the audit interceptor is enabled.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, auditable properties are not automatically populated.
    /// You must manually set audit fields in your domain logic.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track creation timestamps.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, entities implementing <see cref="Encina.DomainModeling.ICreatedAtUtc"/>
    /// will have their <c>CreatedAtUtc</c> property automatically set to the current UTC time
    /// when the entity is first added to the database.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool TrackCreatedAt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track the user who created entities.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, entities implementing <see cref="Encina.DomainModeling.ICreatedBy"/>
    /// will have their <c>CreatedBy</c> property automatically set to the current user ID
    /// from <see cref="IRequestContext.UserId"/> when the entity is first added to the database.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool TrackCreatedBy { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track modification timestamps.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, entities implementing <see cref="Encina.DomainModeling.IModifiedAtUtc"/>
    /// will have their <c>ModifiedAtUtc</c> property automatically set to the current UTC time
    /// when the entity is modified in the database.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool TrackModifiedAt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track the user who modified entities.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, entities implementing <see cref="Encina.DomainModeling.IModifiedBy"/>
    /// will have their <c>ModifiedBy</c> property automatically set to the current user ID
    /// from <see cref="IRequestContext.UserId"/> when the entity is modified in the database.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool TrackModifiedBy { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log audit changes for debugging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, the interceptor logs information about audit field
    /// population at <see cref="Microsoft.Extensions.Logging.LogLevel.Debug"/> level.
    /// This is useful for troubleshooting audit tracking issues during development.
    /// </para>
    /// <para>
    /// <b>Performance Note</b>: Enabling this in production may impact performance
    /// due to additional logging overhead.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="false"/></value>
    public bool LogAuditChanges { get; set; }

    /// <summary>
    /// Gets or sets whether to persist detailed audit log entries to an <see cref="Encina.DomainModeling.Auditing.IAuditLogStore"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/> and an <see cref="Encina.DomainModeling.Auditing.IAuditLogStore"/> is registered,
    /// the interceptor will create detailed audit log entries capturing old and new values for each entity change.
    /// These entries are persisted after a successful <c>SaveChanges</c> operation.
    /// </para>
    /// <para>
    /// <b>Requirements</b>: An implementation of <see cref="Encina.DomainModeling.Auditing.IAuditLogStore"/>
    /// must be registered in the DI container for this feature to work.
    /// </para>
    /// <para>
    /// <b>Performance Note</b>: Enabling this adds overhead for serializing entity values and
    /// storing audit entries. Consider the performance impact for high-throughput scenarios.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="false"/></value>
    public bool LogChangesToStore { get; set; }
}
