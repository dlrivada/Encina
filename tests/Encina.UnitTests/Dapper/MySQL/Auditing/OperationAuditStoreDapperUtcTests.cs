using System.Data;
using System.Data.Common;
using Encina.Dapper.MySQL.Auditing;
using Encina.Security.Audit;
using Encina.UnitTests.Security.Audit.Relational;

namespace Encina.UnitTests.Dapper.MySQL.Auditing;

/// <summary>
/// Proves <see cref="OperationAuditStoreDapper"/> binds only UTC instants to the driver, and reads MySQL
/// <c>DATETIME</c> values (which carry no zone) back as UTC, whatever the machine's time zone.
/// </summary>
/// <remarks>
/// On a UTC runner a Local value equals its UTC value, so the read test alone cannot tell
/// <c>SpecifyKind(..., Utc)</c> from the local-offset default; that difference shows only on a non-UTC machine.
/// </remarks>
public sealed class OperationAuditStoreDapperUtcTests : OperationAuditStoreUtcTestsBase
{
    /// <inheritdoc/>
    protected override IOperationAuditStore CreateStore(DbConnection connection) => new OperationAuditStoreDapper(connection);

    [Fact]
    public async Task GetByCorrelationIdAsync_ZonelessDateTimeColumns_ReadsStartAndCompletionAsUtcOffsetZero()
    {
        // Arrange: MySQL returns DATETIME as an Unspecified DateTime holding the stored UTC wall clock.
        var stored = DateTime.SpecifyKind(new DateTime(2026, 3, 1, 10, 0, 0), DateTimeKind.Unspecified);
        var connection = new RecordingDbConnection { ReaderRows = SingleRow(stored, stored.AddMinutes(5)) };
        var store = new OperationAuditStoreDapper(connection);

        // Act
        var result = await store.GetByCorrelationIdAsync("correlation-1", TestContext.Current.CancellationToken);

        // Assert
        var entry = result.Match(
            Right: entries => entries.ShouldHaveSingleItem(),
            Left: error => throw new InvalidOperationException(error.Message));
        var expectedUtc = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        entry.StartedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        entry.StartedAtUtc.UtcTicks.ShouldBe(expectedUtc.UtcTicks);
        entry.CompletedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        entry.CompletedAtUtc.UtcTicks.ShouldBe(expectedUtc.AddMinutes(5).UtcTicks);
    }

    private static DataTable SingleRow(DateTime stored, DateTime completed)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("CorrelationId", typeof(string));
        table.Columns.Add("UserId", typeof(string));
        table.Columns.Add("TenantId", typeof(string));
        table.Columns.Add("Action", typeof(string));
        table.Columns.Add("EntityType", typeof(string));
        table.Columns.Add("EntityId", typeof(string));
        table.Columns.Add("Outcome", typeof(int));
        table.Columns.Add("ErrorMessage", typeof(string));
        table.Columns.Add("TimestampUtc", typeof(DateTime));
        table.Columns.Add("StartedAtUtc", typeof(DateTime));
        table.Columns.Add("CompletedAtUtc", typeof(DateTime));
        table.Columns.Add("IpAddress", typeof(string));
        table.Columns.Add("UserAgent", typeof(string));
        table.Columns.Add("RequestPayloadHash", typeof(string));
        table.Columns.Add("RequestPayload", typeof(string));
        table.Columns.Add("ResponsePayload", typeof(string));
        table.Columns.Add("Metadata", typeof(string));
        table.Rows.Add(
            Guid.NewGuid(), "correlation-1", DBNull.Value, DBNull.Value, "Create", "Order", DBNull.Value,
            (int)AuditOutcome.Success, DBNull.Value, stored, stored, completed,
            DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        return table;
    }
}
