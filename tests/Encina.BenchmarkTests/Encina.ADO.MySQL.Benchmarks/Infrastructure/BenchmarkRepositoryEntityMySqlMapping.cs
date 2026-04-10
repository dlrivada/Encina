using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.MySQL.Repository;

namespace Encina.ADO.MySQL.Benchmarks.Infrastructure;

/// <summary>
/// MySQL-specific <see cref="IEntityMapping{TEntity, TId}"/> for
/// <see cref="BenchmarkRepositoryEntity"/>. The shared umbrella project ships a SqlServer-typed
/// mapping; each provider must supply its own because each ADO provider package defines its
/// own <c>IEntityMapping</c> interface in a provider-specific namespace.
/// </summary>
public sealed class BenchmarkRepositoryEntityMySqlMapping : IEntityMapping<BenchmarkRepositoryEntity, Guid>
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
