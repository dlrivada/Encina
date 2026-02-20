using System.Linq.Expressions;
using Encina.Dapper.SqlServer.Repository;

namespace Encina.Dapper.SqlServer.Temporal;

/// <summary>
/// Fluent builder for configuring temporal entity mappings for SQL Server temporal tables.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This builder extends <see cref="EntityMappingBuilder{TEntity, TId}"/> with temporal table-specific
/// configuration for SQL Server system-versioned temporal tables.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaTemporalRepository&lt;Order, Guid&gt;(mapping =&gt;
/// {
///     mapping.ToTable("Orders")
///         .HasId(o =&gt; o.Id)
///         .MapProperty(o =&gt; o.CustomerId, "CustomerId")
///         .MapProperty(o =&gt; o.Total, "Total")
///         .WithPeriodColumns("PeriodStart", "PeriodEnd")
///         .WithHistoryTable("dbo.OrdersHistory");
/// });
/// </code>
/// </example>
public sealed class TemporalEntityMappingBuilder<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private string _tableName = typeof(TEntity).Name + "s";
    private string _idColumnName = "Id";
    private readonly Dictionary<string, string> _columnMappings = new();
    private readonly HashSet<string> _insertExcludedProperties = new();
    private readonly HashSet<string> _updateExcludedProperties = new();
    private Func<TEntity, TId>? _idExtractor;
    private string _periodStartColumnName = "PeriodStart";
    private string _periodEndColumnName = "PeriodEnd";
    private string? _historyTableName;

    /// <summary>
    /// Configures the database table name.
    /// </summary>
    /// <param name="tableName">The table name (can include schema, e.g., "dbo.Orders").</param>
    /// <returns>The builder for method chaining.</returns>
    public TemporalEntityMappingBuilder<TEntity, TId> ToTable(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        _tableName = tableName;
        return this;
    }

    /// <summary>
    /// Configures the primary key mapping.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="idSelector">Expression selecting the ID property.</param>
    /// <param name="columnName">Optional column name (defaults to "Id").</param>
    /// <returns>The builder for method chaining.</returns>
    public TemporalEntityMappingBuilder<TEntity, TId> HasId<TProperty>(
        Expression<Func<TEntity, TProperty>> idSelector,
        string columnName = "Id")
    {
        ArgumentNullException.ThrowIfNull(idSelector);

        var propertyName = GetPropertyName(idSelector);
        _idColumnName = columnName;
        _columnMappings[propertyName] = columnName;
        _updateExcludedProperties.Add(propertyName);

        // Create ID extractor from the expression
        var compiled = idSelector.Compile();
        _idExtractor = entity => (TId)(object)compiled(entity)!;

        return this;
    }

    /// <summary>
    /// Maps a property to a database column.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <param name="columnName">The database column name.</param>
    /// <returns>The builder for method chaining.</returns>
    public TemporalEntityMappingBuilder<TEntity, TId> MapProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        string columnName)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

        var propertyName = GetPropertyName(propertySelector);
        _columnMappings[propertyName] = columnName;
        return this;
    }

    /// <summary>
    /// Excludes a property from INSERT operations (e.g., auto-generated columns).
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <returns>The builder for method chaining.</returns>
    public TemporalEntityMappingBuilder<TEntity, TId> ExcludeFromInsert<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _insertExcludedProperties.Add(propertyName);
        return this;
    }

    /// <summary>
    /// Excludes a property from UPDATE operations.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <returns>The builder for method chaining.</returns>
    public TemporalEntityMappingBuilder<TEntity, TId> ExcludeFromUpdate<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var propertyName = GetPropertyName(propertySelector);
        _updateExcludedProperties.Add(propertyName);
        return this;
    }

    /// <summary>
    /// Configures the temporal period column names.
    /// </summary>
    /// <param name="periodStartColumnName">The period start column name (default: "PeriodStart").</param>
    /// <param name="periodEndColumnName">The period end column name (default: "PeriodEnd").</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// These columns must match the PERIOD columns defined in the SQL Server temporal table:
    /// <code>
    /// CREATE TABLE Orders (
    ///     Id UNIQUEIDENTIFIER PRIMARY KEY,
    ///     ...
    ///     PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START,
    ///     PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END,
    ///     PERIOD FOR SYSTEM_TIME (PeriodStart, PeriodEnd)
    /// ) WITH (SYSTEM_VERSIONING = ON);
    /// </code>
    /// </remarks>
    public TemporalEntityMappingBuilder<TEntity, TId> WithPeriodColumns(
        string periodStartColumnName = "PeriodStart",
        string periodEndColumnName = "PeriodEnd")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(periodStartColumnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndColumnName);

        _periodStartColumnName = periodStartColumnName;
        _periodEndColumnName = periodEndColumnName;
        return this;
    }

    /// <summary>
    /// Configures the history table name.
    /// </summary>
    /// <param name="historyTableName">The fully qualified history table name (e.g., "dbo.OrdersHistory").</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// If not specified, SQL Server uses the default naming convention: "[Schema].[TableName_History]".
    /// </remarks>
    public TemporalEntityMappingBuilder<TEntity, TId> WithHistoryTable(string historyTableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(historyTableName);
        _historyTableName = historyTableName;
        return this;
    }

    /// <summary>
    /// Builds the temporal entity mapping.
    /// </summary>
    /// <returns>Either the configured entity mapping or a validation error.</returns>
    public LanguageExt.Either<EncinaError, ITemporalEntityMapping<TEntity, TId>> Build()
    {
        if (_idExtractor is null)
        {
            return EncinaErrors.Create(
                EntityMappingErrorCodes.MissingPrimaryKey,
                $"ID mapping not configured. Call HasId() before Build() for entity {typeof(TEntity).Name}.");
        }

        if (_columnMappings.Count == 0)
        {
            return EncinaErrors.Create(
                EntityMappingErrorCodes.MissingColumnMappings,
                $"No column mappings configured. Call MapProperty() at least once for entity {typeof(TEntity).Name}.");
        }

        return LanguageExt.Either<EncinaError, ITemporalEntityMapping<TEntity, TId>>.Right(
            new TemporalEntityMapping(
                _tableName,
                _idColumnName,
                _columnMappings,
                _idExtractor,
                _insertExcludedProperties,
                _updateExcludedProperties,
                _periodStartColumnName,
                _periodEndColumnName,
                _historyTableName));
    }

    private static string GetPropertyName<TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression { Operand: MemberExpression unaryMember })
        {
            return unaryMember.Member.Name;
        }

        throw new ArgumentException("Expression must be a property access expression.", nameof(expression));
    }

    private sealed class TemporalEntityMapping : ITemporalEntityMapping<TEntity, TId>
    {
        private readonly Func<TEntity, TId> _idExtractor;

        public TemporalEntityMapping(
            string tableName,
            string idColumnName,
            Dictionary<string, string> columnMappings,
            Func<TEntity, TId> idExtractor,
            HashSet<string> insertExcludedProperties,
            HashSet<string> updateExcludedProperties,
            string periodStartColumnName,
            string periodEndColumnName,
            string? historyTableName)
        {
            TableName = tableName;
            IdColumnName = idColumnName;
            ColumnMappings = columnMappings;
            _idExtractor = idExtractor;
            InsertExcludedProperties = insertExcludedProperties;
            UpdateExcludedProperties = updateExcludedProperties;
            PeriodStartColumnName = periodStartColumnName;
            PeriodEndColumnName = periodEndColumnName;
            HistoryTableName = historyTableName;
        }

        public string TableName { get; }
        public string IdColumnName { get; }
        public IReadOnlyDictionary<string, string> ColumnMappings { get; }
        public IReadOnlySet<string> InsertExcludedProperties { get; }
        public IReadOnlySet<string> UpdateExcludedProperties { get; }
        public string PeriodStartColumnName { get; }
        public string PeriodEndColumnName { get; }
        public string? HistoryTableName { get; }

        public TId GetId(TEntity entity) => _idExtractor(entity);
    }
}
