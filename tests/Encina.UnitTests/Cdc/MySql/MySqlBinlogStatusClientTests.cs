using System.Data;
using Encina.Cdc.MySql;
using Shouldly;

namespace Encina.UnitTests.Cdc.MySql;

/// <summary>
/// Unit tests for <see cref="MySqlBinlogStatusClient"/> that run without a server: the result
/// reading goes through a real <see cref="System.Data.Common.DbDataReader"/> built from a
/// <see cref="DataTable"/>; the connection and command lines need MySQL 8.4/9.x (#3052).
/// </summary>
public sealed class MySqlBinlogStatusClientTests
{
    private static DataTable CreateTable()
    {
        var table = new DataTable();
        table.Columns.Add("File", typeof(string));
        table.Columns.Add("Position", typeof(long));
        return table;
    }

    [Fact]
    public async Task ReadPositionAsync_RowReturned_ReturnsFileAndPosition()
    {
        var table = CreateTable();
        table.Rows.Add("binlog.000042", 1234L);
        using var reader = table.CreateDataReader();

        var result = await MySqlBinlogStatusClient.ReadPositionAsync(reader, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Value.File.ShouldBe("binlog.000042");
        result.Value.Position.ShouldBe(1234L);
    }

    [Fact]
    public async Task ReadPositionAsync_EmptyResult_ReturnsNull()
    {
        using var reader = CreateTable().CreateDataReader();

        var result = await MySqlBinlogStatusClient.ReadPositionAsync(reader, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ReadPositionAsync_MultipleRows_ReadsOnlyTheFirst()
    {
        var table = CreateTable();
        table.Rows.Add("binlog.000001", 10L);
        table.Rows.Add("binlog.000002", 20L);
        using var reader = table.CreateDataReader();

        var result = await MySqlBinlogStatusClient.ReadPositionAsync(reader, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Value.File.ShouldBe("binlog.000001");
        result.Value.Position.ShouldBe(10L);
    }

    [Fact]
    public async Task ReadPositionAsync_MissingPositionColumn_Throws()
    {
        var table = new DataTable();
        table.Columns.Add("File", typeof(string));
        table.Rows.Add("binlog.000001");
        using var reader = table.CreateDataReader();

        await Should.ThrowAsync<ArgumentException>(
            () => MySqlBinlogStatusClient.ReadPositionAsync(reader, CancellationToken.None));
    }

    [Fact]
    public async Task ReadPositionAsync_NullFileColumn_Throws()
    {
        var table = CreateTable();
        table.Rows.Add(DBNull.Value, 5L);
        using var reader = table.CreateDataReader();

        await Should.ThrowAsync<InvalidCastException>(
            () => MySqlBinlogStatusClient.ReadPositionAsync(reader, CancellationToken.None));
    }

    [Fact]
    public async Task ReadPositionAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var table = CreateTable();
        table.Rows.Add("binlog.000001", 1L);
        using var reader = table.CreateDataReader();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => MySqlBinlogStatusClient.ReadPositionAsync(reader, cts.Token));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new MySqlBinlogStatusClient(null!));
    }

    [Fact]
    public async Task GetBinlogStatusAsync_CancelledBeforeConnect_ThrowsOperationCanceled()
    {
        var options = new MySqlCdcOptions { ConnectionString = "Server=127.0.0.1;Port=1;User ID=u;Password=p" };
        var client = new MySqlBinlogStatusClient(options);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => client.GetBinlogStatusAsync(cts.Token));
    }
}
