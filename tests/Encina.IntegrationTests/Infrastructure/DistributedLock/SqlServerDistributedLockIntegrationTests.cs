using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;

namespace Encina.IntegrationTests.Infrastructure.DistributedLock;

/// <summary>
/// Integration tests for SqlServerDistributedLockProvider using Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class SqlServerDistributedLockIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private SqlServerDistributedLockProvider? _provider;

    public SqlServerDistributedLockIntegrationTests()
    {
        _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var connectionString = _sqlContainer.GetConnectionString();
        _provider = new SqlServerDistributedLockProvider(
            Options.Create(new SqlServerLockOptions { ConnectionString = connectionString }),
            NullLogger<SqlServerDistributedLockProvider>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldAcquireLock()
    {
        // Arrange
        var resource = $"test-{Guid.NewGuid()}";

        // Act
        var lockHandle = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceLocked_ShouldReturnNull()
    {
        // Arrange
        var resource = $"test-{Guid.NewGuid()}";

        await using var firstLock = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Act
        var secondLock = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        firstLock.ShouldNotBeNull();
        secondLock.ShouldBeNull();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceLocked_ShouldReturnTrue()
    {
        // Arrange
        var resource = $"test-{Guid.NewGuid()}";

        await using var lockHandle = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Act
        var isLocked = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task Lock_WhenReleased_ShouldBeAcquirableAgain()
    {
        // Arrange
        var resource = $"test-{Guid.NewGuid()}";

        var firstLock = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        await firstLock!.DisposeAsync();

        // Act
        var secondLock = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        secondLock.ShouldNotBeNull();
        await secondLock!.DisposeAsync();
    }

    [Fact]
    public async Task ConcurrentAcquisitions_OnlyOneSucceeds()
    {
        // Arrange
        var resource = $"test-concurrent-{Guid.NewGuid()}";
        const int numAttempts = 5; // Less than Redis to avoid SQL connection pool issues

        // Act
        var tasks = Enumerable.Range(0, numAttempts)
            .Select(_ => _provider!.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(20),
                CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r is not null);
        successCount.ShouldBe(1);

        // Clean up
        foreach (var result in results.Where(r => r is not null))
        {
            await result!.DisposeAsync();
        }
    }
}
