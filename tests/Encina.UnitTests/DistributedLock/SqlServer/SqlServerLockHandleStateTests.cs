using System.Reflection;
using Encina.DistributedLock;
using Encina.DistributedLock.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.SqlServer;

/// <summary>
/// Unit tests for the state of the SQL Server lock handle (expiry, extension, repeated disposal) and for the
/// provider paths that fail while opening the connection. The handle is built by reflection around a connection
/// that was never opened, so no server is needed.
/// </summary>
public sealed class SqlServerLockHandleStateTests
{
    private static readonly DateTime AcquiredAt = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ILockHandle CreateHandle(FakeLogger<SqlServerDistributedLockProvider> logger, TimeProvider timeProvider)
    {
        var connection = new SqlConnection("Server=.;Database=Test;Trusted_Connection=True;");
        var handleType = typeof(SqlServerDistributedLockProvider).GetNestedType("LockHandle", BindingFlags.NonPublic)!;
        return (ILockHandle)Activator.CreateInstance(
            handleType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [connection, "lock:resource", "lock-id", "resource", AcquiredAt, AcquiredAt.AddMinutes(1), logger, timeProvider],
            culture: null)!;
    }

    private static SqlServerDistributedLockProvider CreateProviderForUnreachableServer() =>
        new(
            Options.Create(new SqlServerLockOptions
            {
                // Port 1 refuses the connection at once, so opening fails without waiting for a timeout
                ConnectionString = "Server=127.0.0.1,1;Database=Test;User Id=sa;Password=x;Encrypt=false;Connect Timeout=2"
            }),
            new FakeLogger<SqlServerDistributedLockProvider>());

    [Fact]
    public void LockHandle_ExposesTheValuesItWasCreatedWith()
    {
        // Arrange & Act
        var handle = CreateHandle(new FakeLogger<SqlServerDistributedLockProvider>(), TimeProvider.System);

        // Assert
        handle.Resource.ShouldBe("resource");
        handle.LockId.ShouldBe("lock-id");
        handle.AcquiredAtUtc.ShouldBe(AcquiredAt);
        handle.ExpiresAtUtc.ShouldBe(AcquiredAt.AddMinutes(1));
        handle.IsReleased.ShouldBeFalse();
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_WhenHeld_SetsTheExpiryFromTheCurrentTimeAndReturnsTrue()
    {
        // Arrange
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 30, 0, TimeSpan.Zero));
        var handle = CreateHandle(new FakeLogger<SqlServerDistributedLockProvider>(), clock);

        // Act
        var extended = await handle.ExtendAsync(TimeSpan.FromMinutes(10));

        // Assert
        extended.ShouldBeTrue();
        handle.ExpiresAtUtc.ShouldBe(new DateTime(2026, 1, 1, 12, 40, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_WhenDisposed_ReturnsFalseAndKeepsTheExpiry()
    {
        // Arrange
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 30, 0, TimeSpan.Zero));
        var handle = CreateHandle(new FakeLogger<SqlServerDistributedLockProvider>(), clock);
        await handle.DisposeAsync();
        var expiryBefore = handle.ExpiresAtUtc;

        // Act
        var extended = await handle.ExtendAsync(TimeSpan.FromMinutes(10));

        // Assert
        extended.ShouldBeFalse();
        handle.ExpiresAtUtc.ShouldBe(expiryBefore);
    }

    [Fact]
    public async Task LockHandle_DisposeAsync_CalledTwice_AttemptsTheReleaseOnlyOnce()
    {
        // Arrange - the never-opened connection makes the first release fail and log once
        var logger = new FakeLogger<SqlServerDistributedLockProvider>();
        var handle = CreateHandle(logger, TimeProvider.System);
        await handle.DisposeAsync();

        // Act
        await Should.NotThrowAsync(async () => await handle.DisposeAsync());

        // Assert
        logger.Collector.GetSnapshot().Count(r => r.Level == LogLevel.Warning).ShouldBe(1);
        handle.IsReleased.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireAsync_WhenTheServerIsUnreachable_ThrowsTheOpenFailureUnwrapped()
    {
        // Arrange
        var provider = CreateProviderForUnreachableServer();

        // Act & Assert - the connection is opened before the try block, so the failure is not a LockAcquisitionException
        await Should.ThrowAsync<SqlException>(() =>
            provider.AcquireAsync("resource", TimeSpan.FromMinutes(1), CancellationToken.None));
    }

    [Fact]
    public async Task TryAcquireAsync_WhenTheServerIsUnreachable_ThrowsTheOpenFailure()
    {
        // Arrange
        var provider = CreateProviderForUnreachableServer();

        // Act & Assert
        await Should.ThrowAsync<SqlException>(() =>
            provider.TryAcquireAsync(
                "resource",
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None));
    }

    [Fact]
    public async Task IsLockedAsync_WhenTheServerIsUnreachable_ThrowsTheOpenFailure()
    {
        // Arrange
        var provider = CreateProviderForUnreachableServer();

        // Act & Assert
        await Should.ThrowAsync<SqlException>(() => provider.IsLockedAsync("resource", CancellationToken.None));
    }
}
