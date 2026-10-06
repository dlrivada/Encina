using System.Data.Common;
using Encina.EntityFrameworkCore.Auditing;
using Encina.Security.Audit;
using Encina.UnitTests.Security.Audit.Relational;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Encina.UnitTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Proves <see cref="OperationAuditStoreEF"/> hands only UTC instants to the EF Core provider.
/// </summary>
/// <remarks>
/// The store runs over a real provider <see cref="DbContext"/> whose connection is never opened: interceptors
/// capture the value of every <see cref="DbParameter"/> EF Core builds for the command, then abort it. The
/// assertion therefore sits on the exact values the provider would send, produced by the store's real code path.
/// </remarks>
public abstract class OperationAuditStoreEFUtcTestsBase : OperationAuditStoreUtcTestsBase
{
    /// <inheritdoc/>
    protected override bool ExpectsSuccess => false;

    /// <inheritdoc/>
    protected override bool BindsNamedParameters => false;

    /// <summary>Configures the provider under test on <paramref name="options"/>.</summary>
    protected abstract void UseProvider(DbContextOptionsBuilder<OperationAuditTestContext> options);

    /// <inheritdoc/>
    protected override IOperationAuditStore CreateStore(DbConnection connection)
    {
        var recorder = (RecordingDbConnection)connection;
        var options = new DbContextOptionsBuilder<OperationAuditTestContext>();
        UseProvider(options);
        options.AddInterceptors(new SuppressOpenInterceptor(), new CaptureCommandInterceptor(recorder));
        return new OperationAuditStoreEF(new OperationAuditTestContext(options.Options));
    }

    [Fact]
    public void MapToRecord_UnspecifiedTimestamp_IsReadBackAsUtc()
    {
        // Arrange: SQL Server and MySQL return datetime columns with Kind Unspecified.
        var entity = OperationAuditStoreEF.MapToEntity(new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "correlation-1",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
            StartedAtUtc = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero),
            CompletedAtUtc = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero)
        });
        entity.TimestampUtc = DateTime.SpecifyKind(new DateTime(2026, 3, 1, 10, 0, 0), DateTimeKind.Unspecified);

        // Act
        var record = OperationAuditStoreEF.MapToRecord(entity);

        // Assert
        record.TimestampUtc.Kind.ShouldBe(DateTimeKind.Utc);
        record.TimestampUtc.Ticks.ShouldBe(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc).Ticks);
    }
}

/// <summary>Test <see cref="DbContext"/> that maps the operation audit entity like the production configuration.</summary>
public sealed class OperationAuditTestContext(DbContextOptions<OperationAuditTestContext> options) : DbContext(options)
{
    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new OperationAuditEntryEntityConfiguration());
}

/// <summary>Thrown by <see cref="CaptureCommandInterceptor"/> once a command's parameters are captured.</summary>
internal sealed class CommandCapturedException : Exception;

internal sealed class SuppressOpenInterceptor : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
        InterceptionResult.Suppress();

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InterceptionResult.Suppress());
}

internal sealed class CaptureCommandInterceptor(RecordingDbConnection recorder) : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result) =>
        Capture(command);

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) =>
        throw Captured(command);

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result) =>
        throw Captured(command);

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
        throw Captured(command);

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result) =>
        throw Captured(command);

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default) =>
        throw Captured(command);

    private InterceptionResult<DbDataReader> Capture(DbCommand command) => throw Captured(command);

    private CommandCapturedException Captured(DbCommand command)
    {
        var parameters = command.Parameters.Cast<DbParameter>()
            .Select(p => new RecordedParameter(p.ParameterName.TrimStart('@', ':', '?'), p.Value))
            .ToList();
        recorder.Record(new RecordedCommand(command.CommandText, parameters));
        return new CommandCapturedException();
    }
}
