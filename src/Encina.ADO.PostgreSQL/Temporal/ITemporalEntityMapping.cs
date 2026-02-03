using Encina.ADO.PostgreSQL.Repository;

namespace Encina.ADO.PostgreSQL.Temporal;

/// <summary>
/// Defines the mapping configuration for a temporal entity type to its database table.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IEntityMapping{TEntity, TId}"/> with temporal table-specific
/// configuration for PostgreSQL temporal tables using the <c>temporal_tables</c> extension.
/// </para>
/// <para>
/// <b>PostgreSQL Temporal Tables (Non-Native)</b>: Unlike SQL Server, PostgreSQL does not have
/// native temporal table support. This implementation uses the <c>temporal_tables</c> extension
/// which must be installed separately:
/// <code>
/// CREATE EXTENSION IF NOT EXISTS temporal_tables;
/// </code>
/// </para>
/// <para>
/// <b>Important</b>: The <c>temporal_tables</c> extension is a third-party extension and may not
/// be available in all PostgreSQL deployments (e.g., some managed cloud instances). Verify
/// extension availability before using this feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderTemporalMapping : ITemporalEntityMapping&lt;Order, Guid&gt;
/// {
///     public string TableName =&gt; "orders";
///     public string IdColumnName =&gt; "id";
///     public string PeriodStartColumnName =&gt; "sys_period_start";
///     public string PeriodEndColumnName =&gt; "sys_period_end";
///     public string HistoryTableName =&gt; "orders_history";
///     public IReadOnlyDictionary&lt;string, string&gt; ColumnMappings =&gt; new Dictionary&lt;string, string&gt;
///     {
///         ["Id"] = "id",
///         ["CustomerId"] = "customer_id",
///         ["Total"] = "total"
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
    /// Gets the name of the period start column (typically "sys_period_start" or "valid_from").
    /// </summary>
    /// <remarks>
    /// This column stores the timestamp indicating when the row version became valid.
    /// The <c>temporal_tables</c> extension uses this column with the <c>tstzrange</c> type.
    /// </remarks>
    string PeriodStartColumnName { get; }

    /// <summary>
    /// Gets the name of the period end column (typically "sys_period_end" or "valid_to").
    /// </summary>
    /// <remarks>
    /// This column stores the timestamp indicating when the row version ceased to be valid.
    /// For current rows, this is typically NULL or set to 'infinity'.
    /// </remarks>
    string PeriodEndColumnName { get; }

    /// <summary>
    /// Gets the name of the history table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PostgreSQL temporal_tables extension requires an explicit history table.
    /// The history table must have the same structure as the main table plus
    /// the period columns.
    /// </para>
    /// <para>
    /// Common naming convention: <c>"{tablename}_history"</c>
    /// </para>
    /// </remarks>
    string HistoryTableName { get; }
}
