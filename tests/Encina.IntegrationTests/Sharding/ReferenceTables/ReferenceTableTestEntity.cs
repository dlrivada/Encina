using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Encina.IntegrationTests.Sharding.ReferenceTables;

/// <summary>
/// Test entity for reference table integration tests.
/// Maps to the "Country" table created by <see cref="TestInfrastructure.Fixtures.Sharding.ShardedSqliteFixture"/>.
/// </summary>
[Table("Country")]
internal sealed class CountryRef
{
    [Key]
    public string Id { get; set; } = "";

    public string Code { get; set; } = "";

    public string Name { get; set; } = "";
}
