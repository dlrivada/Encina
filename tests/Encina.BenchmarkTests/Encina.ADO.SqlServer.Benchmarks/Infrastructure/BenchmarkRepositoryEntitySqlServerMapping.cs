using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.SqlServer.Repository;

namespace Encina.ADO.SqlServer.Benchmarks.Infrastructure;

/// <summary>
/// SQL Server-specific <see cref="IEntityMapping{TEntity, TId}"/> for
/// <see cref="BenchmarkRepositoryEntity"/>. The shared umbrella project ships a mapping that
/// also happens to target this interface, but we keep a local copy here so the SQL Server
/// benchmark project is self-contained and aligned with its MySQL / PostgreSQL siblings.
/// </summary>
public sealed class BenchmarkRepositoryEntitySqlServerMapping : IEntityMapping<BenchmarkRepositoryEntity, Guid>
{
    /// <inheritdoc />
    public string TableName => "BenchmarkEntities";

    /// <inheritdoc />
    public string IdColumnName => "Id";

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["Name"] = "Name",
        ["Description"] = "Description",
        ["Price"] = "Price",
        ["Quantity"] = "Quantity",
        ["IsActive"] = "IsActive",
        ["Category"] = "Category",
        ["CreatedAtUtc"] = "CreatedAtUtc",
        ["UpdatedAtUtc"] = "UpdatedAtUtc"
    };

    /// <inheritdoc />
    public IReadOnlySet<string> InsertExcludedProperties { get; } = new HashSet<string>();

    /// <inheritdoc />
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new HashSet<string> { "Id", "CreatedAtUtc" };

    /// <inheritdoc />
    public Guid GetId(BenchmarkRepositoryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return entity.Id;
    }
}
