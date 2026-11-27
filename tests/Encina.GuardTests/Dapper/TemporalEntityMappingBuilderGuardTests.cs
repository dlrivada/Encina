using NpgTemporal = Encina.Dapper.PostgreSQL.Temporal;
using SqlTemporal = Encina.Dapper.SqlServer.Temporal;

namespace Encina.GuardTests.Dapper;

/// <summary>
/// Guard tests for the Dapper temporal entity mapping builders (SqlServer, PostgreSQL): argument checks.
/// </summary>
public class TemporalEntityMappingBuilderGuardTests
{
    private sealed class Widget
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ToTable_NullOrWhitespace_Throws(string? tableName)
    {
        Should.Throw<ArgumentException>(() => new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().ToTable(tableName!));
        Should.Throw<ArgumentException>(() => new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().ToTable(tableName!));
    }

    [Fact]
    public void HasId_NullSelector_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().HasId<Guid>(null!, "Id"));
        Should.Throw<ArgumentNullException>(() => new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().HasId<Guid>(null!, "Id"));
    }

    [Fact]
    public void MapProperty_NullSelector_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().MapProperty<string>(null!, "Name"));
        Should.Throw<ArgumentNullException>(() => new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().MapProperty<string>(null!, "Name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void MapProperty_NullOrWhitespaceColumn_Throws(string? columnName)
    {
        Should.Throw<ArgumentException>(() => new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().MapProperty(w => w.Name, columnName!));
        Should.Throw<ArgumentException>(() => new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().MapProperty(w => w.Name, columnName!));
    }

    [Fact]
    public void ExcludeFromInsertAndUpdate_NullSelector_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().ExcludeFromInsert<string>(null!));
        Should.Throw<ArgumentNullException>(() => new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().ExcludeFromUpdate<string>(null!));
        Should.Throw<ArgumentNullException>(() => new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().ExcludeFromInsert<string>(null!));
        Should.Throw<ArgumentNullException>(() => new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().ExcludeFromUpdate<string>(null!));
    }

    [Theory]
    [InlineData(null, "end")]
    [InlineData("start", null)]
    [InlineData(" ", "end")]
    public void WithPeriodColumns_NullOrWhitespace_Throws(string? start, string? end)
    {
        Should.Throw<ArgumentException>(() => new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().WithPeriodColumns(start!, end!));
        Should.Throw<ArgumentException>(() => new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().WithPeriodColumns(start!, end!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void WithHistoryTable_NullOrWhitespace_Throws(string? historyTable)
    {
        Should.Throw<ArgumentException>(() => new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().WithHistoryTable(historyTable!));
        Should.Throw<ArgumentException>(() => new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().WithHistoryTable(historyTable!));
    }
}
