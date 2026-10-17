using System.Reflection;
using Encina.DistributedLock;
using Encina.DistributedLock.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.SqlServer;

/// <summary>
/// Unit tests for the release path of the SQL Server lock handle: a failed release is logged, never thrown (#531).
/// </summary>
public sealed class SqlServerLockHandleReleaseTests
{
    [Fact]
    public async Task LockHandle_DisposeAsync_WhenReleaseFails_LogsExceptionTypeOnlyAndDoesNotThrow()
    {
        // Arrange - a connection that was never opened makes the release command fail without a server
        var logger = new FakeLogger<SqlServerDistributedLockProvider>();
        var connection = new SqlConnection("Server=.;Database=Test;Trusted_Connection=True;");
        var handleType = typeof(SqlServerDistributedLockProvider).GetNestedType("LockHandle", BindingFlags.NonPublic)!;
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var handle = (ILockHandle)Activator.CreateInstance(
            handleType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [connection, "lock:resource", "lock-id", "resource", now, now.AddMinutes(1), logger, TimeProvider.System],
            culture: null)!;

        // Act
        await Should.NotThrowAsync(async () => await handle.DisposeAsync());

        // Assert
        handle.IsReleased.ShouldBeTrue();
        var entry = logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        entry.Id.Id.ShouldBe(3804);
        entry.Message.ShouldContain("resource: resource ");
        entry.Message.ShouldContain(nameof(InvalidOperationException));
        entry.Exception.ShouldNotBeNull();
    }
}
