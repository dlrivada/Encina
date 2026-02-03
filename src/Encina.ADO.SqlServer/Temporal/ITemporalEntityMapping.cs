using Encina.ADO.SqlServer.Repository;

namespace Encina.ADO.SqlServer.Temporal;

/// <summary>
/// Defines the mapping configuration for a temporal entity type to its database table.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IEntityMapping{TEntity, TId}"/> with temporal table-specific
/// configuration for SQL Server system-versioned temporal tables.
/// </para>
/// <para>
/// <b>SQL Server Temporal Tables</b>: Temporal tables use two datetime2 columns to track
/// the validity period of each row. These columns are automatically maintained by SQL Server.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderTemporalMapping : ITemporalEntityMapping&lt;Order, Guid&gt;
/// {
///     public string TableName =&gt; "Orders";
///     public string IdColumnName =&gt; "Id";
///     public string PeriodStartColumnName =&gt; "PeriodStart";
///     public string PeriodEndColumnName =&gt; "PeriodEnd";
///     public IReadOnlyDictionary&lt;string, string&gt; ColumnMappings =&gt; new Dictionary&lt;string, string&gt;
///     {
///         ["Id"] = "Id",
///         ["CustomerId"] = "CustomerId",
///         ["Total"] = "Total"
///     };
///     public Guid GetId(Order entity) =&gt; entity.Id;
/// }
/// </code>
/// </example>
public interface ITemporalEntityMapping<TEntity, TId> : IEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets the name of the period start column (typically "PeriodStart" or "ValidFrom").
    /// </summary>
    /// <remarks>
    /// This column stores the datetime2 value indicating when the row version became valid.
    /// SQL Server automatically sets this value when rows are inserted or updated.
    /// </remarks>
    string PeriodStartColumnName { get; }

    /// <summary>
    /// Gets the name of the period end column (typically "PeriodEnd" or "ValidTo").
    /// </summary>
    /// <remarks>
    /// This column stores the datetime2 value indicating when the row version ceased to be valid.
    /// For current rows, this is set to the maximum datetime2 value (9999-12-31 23:59:59.9999999).
    /// </remarks>
    string PeriodEndColumnName { get; }

    /// <summary>
    /// Gets the name of the history table (optional).
    /// </summary>
    /// <remarks>
    /// If null, SQL Server uses the default history table name pattern: "[SchemaName].[TableName_History]".
    /// Set this to query a custom history table name.
    /// </remarks>
    string? HistoryTableName { get; }
}
