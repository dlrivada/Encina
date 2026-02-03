namespace Encina.EntityFrameworkCore.SoftDelete;

/// <summary>
/// Configuration options for the soft delete interceptor.
/// </summary>
/// <remarks>
/// <para>
/// These options control how soft-deletable entities are handled when delete operations
/// are performed via <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/>.
/// </para>
/// <para>
/// <b>Design Philosophy</b>: All options are enabled by default to provide comprehensive
/// soft delete behavior out of the box. Disable specific options if you need manual control
/// over certain aspects of soft delete handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseSoftDelete = true;
///     config.SoftDeleteOptions.TrackDeletedBy = true;
///     config.SoftDeleteOptions.LogSoftDeletes = true;
/// });
/// </code>
/// </example>
public sealed class SoftDeleteInterceptorOptions
{
    /// <summary>
    /// Gets or sets whether the soft delete interceptor is enabled.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, delete operations are not converted to soft deletes.
    /// Entities will be physically deleted from the database.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track the deletion timestamp.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, entities implementing <see cref="Encina.DomainModeling.ISoftDeletableEntity"/>
    /// will have their <c>DeletedAtUtc</c> property automatically set to the current UTC time
    /// when the entity is soft-deleted.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool TrackDeletedAt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track the user who deleted the entity.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, entities implementing <see cref="Encina.DomainModeling.ISoftDeletableEntity"/>
    /// will have their <c>DeletedBy</c> property automatically set to the current user ID
    /// from <see cref="IRequestContext.UserId"/> when the entity is soft-deleted.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool TrackDeletedBy { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log soft delete operations for debugging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, the interceptor logs information about soft delete
    /// operations at <see cref="Microsoft.Extensions.Logging.LogLevel.Debug"/> level.
    /// This is useful for troubleshooting soft delete behavior during development.
    /// </para>
    /// <para>
    /// <b>Performance Note</b>: Enabling this in production may impact performance
    /// due to additional logging overhead.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="false"/></value>
    public bool LogSoftDeletes { get; set; }
}
