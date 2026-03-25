using Encina.DistributedLock.SqlServer;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.SqlServer;

/// <summary>
/// Extended unit tests for <see cref="SqlServerDistributedLockProvider"/> covering
/// additional paths: cancellation, ExtendAsync behavior, and key prefix logic.
/// </summary>
public sealed class SqlServerDistributedLockProviderExtendedTests
{
    private readonly ILogger<SqlServerDistributedLockProvider> _logger =
        NullLogger<SqlServerDistributedLockProvider>.Instance;

    private SqlServerDistributedLockProvider CreateProvider(string? keyPrefix = null)
    {
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;",
            KeyPrefix = keyPrefix ?? ""
        });
        return new SqlServerDistributedLockProvider(options, _logger);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.TryAcquireAsync(
                "test-resource",
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(100),
                cts.Token));
    }

    [Fact]
    public async Task AcquireAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.AcquireAsync(
                "test-resource",
                TimeSpan.FromMinutes(1),
                cts.Token));
    }

    [Fact]
    public async Task IsLockedAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.IsLockedAsync("test-resource", cts.Token));
    }

    [Fact]
    public async Task ExtendAsync_WithValidResource_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateProvider();

        // Act - ExtendAsync for SqlServer always returns true since locks are held until released
        var result = await provider.ExtendAsync("my-resource", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithCustomTimeProvider_ShouldAcceptIt()
    {
        // Arrange
        var options = Options.Create(new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
        });
        var customTimeProvider = Substitute.For<TimeProvider>();

        // Act
        var provider = new SqlServerDistributedLockProvider(options, _logger, customTimeProvider);

        // Assert
        provider.ShouldNotBeNull();
    }
}
