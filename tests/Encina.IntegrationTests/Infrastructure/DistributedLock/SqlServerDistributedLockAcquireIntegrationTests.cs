using Encina.DistributedLock;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Infrastructure.DistributedLock;

/// <summary>
/// Integration tests of <see cref="SqlServerDistributedLockProvider"/> paths the five original tests never reach:
/// <c>AcquireAsync</c>, the free branch of <c>IsLockedAsync</c>, cancellation while <c>sp_getapplock</c> runs
/// and the lock handle. Waits above 30 seconds are not pinned here: the command timeout cuts them (see the follow-up
/// issue of #2732). They share the <c>ADO-SqlServer</c> container; every test uses its own
/// resource name because application locks are scoped to the database, not to the test.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class SqlServerDistributedLockAcquireIntegrationTests
{
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Retry = TimeSpan.FromMilliseconds(50);

    private readonly SqlServerDistributedLockProvider _provider;

    public SqlServerDistributedLockAcquireIntegrationTests(SqlServerFixture fixture)
    {
        _provider = new SqlServerDistributedLockProvider(
            Options.Create(new SqlServerLockOptions { ConnectionString = fixture.ConnectionString }),
            NullLogger<SqlServerDistributedLockProvider>.Instance);
    }

    private static string NewResource() => $"acquire-{Guid.NewGuid()}";

    [Fact]
    public async Task AcquireAsync_OnFreeResource_HoldsTheLockUntilTheHandleIsDisposed()
    {
        // Arrange
        var resource = NewResource();

        // Act
        var handle = await _provider.AcquireAsync(resource, Expiry, CancellationToken.None);
        var lockedWhileHeld = await _provider.IsLockedAsync(resource, CancellationToken.None);
        await handle.DisposeAsync();
        var lockedAfterRelease = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        lockedWhileHeld.ShouldBeTrue();
        lockedAfterRelease.ShouldBeFalse();
        var again = await _provider.AcquireAsync(resource, Expiry, CancellationToken.None);
        await again.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_ReturnsAHandleCarryingTheResourceAndTheExpiry()
    {
        // Arrange
        var resource = NewResource();

        // Act
        var handle = (ILockHandle)await _provider.AcquireAsync(resource, Expiry, CancellationToken.None);

        // Assert
        try
        {
            handle.Resource.ShouldBe(resource);
            handle.LockId.ShouldNotBeNullOrWhiteSpace();
            handle.IsReleased.ShouldBeFalse();
            (handle.ExpiresAtUtc - handle.AcquiredAtUtc).ShouldBe(Expiry);
        }
        finally
        {
            await handle.DisposeAsync();
        }
    }

    [Fact]
    public async Task AcquireAsync_WhenHeldElsewhere_CompletesOnlyAfterTheHolderDisposes()
    {
        // Arrange
        var resource = NewResource();
        var holder = await _provider.AcquireAsync(resource, Expiry, CancellationToken.None);
        var blocked = _provider.AcquireAsync(resource, Expiry, CancellationToken.None);

        // Act
        var completedWhileHeld = await Task.WhenAny(blocked, Task.Delay(TimeSpan.FromMilliseconds(500))) == blocked;
        await holder.DisposeAsync();
        var handle = await blocked.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        completedWhileHeld.ShouldBeFalse();
        handle.ShouldNotBeNull();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task IsLockedAsync_OnFreeResource_ReturnsFalseAndLeavesItAcquirable()
    {
        // Arrange
        var resource = NewResource();

        // Act
        var locked = await _provider.IsLockedAsync(resource, CancellationToken.None);
        var handle = await _provider.TryAcquireAsync(resource, Expiry, TimeSpan.FromSeconds(1), Retry, CancellationToken.None);

        // Assert
        locked.ShouldBeFalse();
        handle.ShouldNotBeNull();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WhenCancelledWhileWaiting_ThrowsLockAcquisitionExceptionAndReleasesTheConnection()
    {
        // Arrange
        var resource = NewResource();
        var holder = await _provider.AcquireAsync(resource, Expiry, CancellationToken.None);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        var ex = await Should.ThrowAsync<LockAcquisitionException>(
            () => _provider.AcquireAsync(resource, Expiry, cts.Token));

        // Assert
        ex.Resource.ShouldBe(resource);
        ex.InnerException.ShouldNotBeNull();
        await holder.DisposeAsync();
        var afterwards = await _provider.TryAcquireAsync(resource, Expiry, TimeSpan.FromSeconds(5), Retry, CancellationToken.None);
        afterwards.ShouldNotBeNull();
        await afterwards.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenCancelledWhileWaiting_RethrowsAndReleasesTheConnection()
    {
        // Arrange
        var resource = NewResource();
        var holder = await _provider.AcquireAsync(resource, Expiry, CancellationToken.None);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        var thrown = await Should.ThrowAsync<Exception>(
            () => _provider.TryAcquireAsync(resource, Expiry, TimeSpan.FromSeconds(30), Retry, cts.Token));

        // Assert
        thrown.ShouldNotBeOfType<LockAcquisitionException>();
        await holder.DisposeAsync();
        var afterwards = await _provider.TryAcquireAsync(resource, Expiry, TimeSpan.FromSeconds(5), Retry, CancellationToken.None);
        afterwards.ShouldNotBeNull();
        await afterwards.DisposeAsync();
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_MovesTheExpiryWhileHeldAndReturnsFalseOnceReleased()
    {
        // Arrange
        var resource = NewResource();
        var handle = (ILockHandle)await _provider.AcquireAsync(resource, TimeSpan.FromSeconds(10), CancellationToken.None);
        var before = handle.ExpiresAtUtc;

        // Act
        var extendedWhileHeld = await handle.ExtendAsync(TimeSpan.FromHours(1), CancellationToken.None);
        var after = handle.ExpiresAtUtc;
        await handle.DisposeAsync();
        var extendedAfterRelease = await handle.ExtendAsync(TimeSpan.FromHours(1), CancellationToken.None);

        // Assert
        extendedWhileHeld.ShouldBeTrue();
        after.ShouldBeGreaterThan(before);
        extendedAfterRelease.ShouldBeFalse();
        handle.IsReleased.ShouldBeTrue();
    }

    [Fact]
    public async Task LockHandle_DisposedTwice_ReleasesOnceAndKeepsTheResourceFreeForTheNextHolder()
    {
        // Arrange
        var resource = NewResource();
        var handle = await _provider.AcquireAsync(resource, Expiry, CancellationToken.None);
        await handle.DisposeAsync();
        var next = await _provider.AcquireAsync(resource, Expiry, CancellationToken.None);

        // Act - the second dispose of the first handle must not release the lock the next holder now owns
        await handle.DisposeAsync();
        var lockedByNext = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        lockedByNext.ShouldBeTrue();
        await next.DisposeAsync();
    }
}
