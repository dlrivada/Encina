namespace Encina.Messaging.SoftDelete;

/// <summary>
/// Configuration options for automatic soft delete handling.
/// </summary>
/// <remarks>
/// <para>
/// These options control how soft-deletable entities are handled when delete operations
/// are performed across all data access providers (EF Core, Dapper, ADO.NET).
/// </para>
/// <para>
/// <b>Design Philosophy</b>: All options are enabled by default to provide comprehensive
/// soft delete behavior out of the box. Disable specific options if you need manual control
/// over certain soft delete fields.
/// </para>
/// <para>
/// <b>Supported Interfaces</b>: The soft delete mechanism processes entities implementing
/// <see cref="Encina.DomainModeling.ISoftDeletableEntity"/> (with public setters).
/// Entities implementing only <see cref="Encina.DomainModeling.ISoftDeletable"/> (getter-only)
/// should handle soft delete via domain methods.
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
public sealed class SoftDeleteOptions
{
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
    /// When <see langword="true"/>, the soft delete mechanism logs information about soft delete
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

    /// <summary>
    /// Gets or sets whether to automatically filter soft-deleted entities in queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, all queries on soft-deletable entities automatically
    /// include a filter to exclude deleted entities (<c>WHERE IsDeleted = 0</c> or equivalent).
    /// </para>
    /// <para>
    /// Use <c>IncludeDeleted()</c> method on repositories to temporarily bypass this filter
    /// when you need to access soft-deleted entities.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool AutoFilterSoftDeletedQueries { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw an exception when a soft-deleted entity is accessed.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, accessing a soft-deleted entity through <c>GetByIdAsync</c>
    /// will throw a <see cref="System.InvalidOperationException"/> instead of returning
    /// <c>None</c>. This is useful for debugging to catch accidental access to deleted entities.
    /// </remarks>
    /// <value>Default: <see langword="false"/></value>
    public bool ThrowOnDeletedAccess { get; set; }

    #region Column Name Configuration (for Dapper/ADO.NET)

    /// <summary>
    /// Gets or sets the default column name for the <c>IsDeleted</c> flag.
    /// </summary>
    /// <remarks>
    /// Used by Dapper and ADO.NET providers for SQL generation.
    /// EF Core uses property-based mapping instead.
    /// </remarks>
    /// <value>Default: <c>"IsDeleted"</c></value>
    public string DefaultIsDeletedColumnName { get; set; } = "IsDeleted";

    /// <summary>
    /// Gets or sets the default column name for the <c>DeletedAtUtc</c> timestamp.
    /// </summary>
    /// <remarks>
    /// Used by Dapper and ADO.NET providers for SQL generation.
    /// EF Core uses property-based mapping instead.
    /// </remarks>
    /// <value>Default: <c>"DeletedAtUtc"</c></value>
    public string DefaultDeletedAtColumnName { get; set; } = "DeletedAtUtc";

    /// <summary>
    /// Gets or sets the default column name for the <c>DeletedBy</c> user identifier.
    /// </summary>
    /// <remarks>
    /// Used by Dapper and ADO.NET providers for SQL generation.
    /// EF Core uses property-based mapping instead.
    /// </remarks>
    /// <value>Default: <c>"DeletedBy"</c></value>
    public string DefaultDeletedByColumnName { get; set; } = "DeletedBy";

    #endregion
}
