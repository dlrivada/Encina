using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.DistributedLock.ContractTests;

/// <summary>
/// Contract tests that verify all IDistributedLockProvider implementations
/// follow the expected behavior contract.
/// </summary>
public abstract class IDistributedLockProviderContractTests
{
    protected abstract IDistributedLockProvider CreateProvider();

    [Fact]
    public async Task TryAcquireAsync_WhenResourceFree_ShouldReturnLock()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = $"contract-test-{Guid.NewGuid()}";

        // Act
        var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.Should().NotBeNull();
        await lockHandle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceLocked_ShouldEventuallyReturnNull()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = $"contract-test-{Guid.NewGuid()}";

        await using var firstLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Act
        var secondLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        firstLock.Should().NotBeNull();
        secondLock.Should().BeNull();
    }

    [Fact]
    public async Task AcquireAsync_WhenResourceFree_ShouldReturnLock()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = $"contract-test-{Guid.NewGuid()}";

        // Act
        var lockHandle = await provider.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        // Assert
        lockHandle.Should().NotBeNull();
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceLocked_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = $"contract-test-{Guid.NewGuid()}";

        await using var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Act
        var isLocked = await provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.Should().BeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceNotLocked_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = $"contract-test-{Guid.NewGuid()}";

        // Act
        var isLocked = await provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.Should().BeFalse();
    }

    [Fact]
    public async Task Lock_WhenDisposed_ShouldReleaseResource()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = $"contract-test-{Guid.NewGuid()}";

        var firstLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Act
        await firstLock!.DisposeAsync();

        // Assert - should be able to acquire again
        var secondLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        secondLock.Should().NotBeNull();
        await secondLock!.DisposeAsync();
    }

    [Fact]
    public async Task MultipleDifferentResources_ShouldAllBeAcquirable()
    {
        // Arrange
        var provider = CreateProvider();
        var resources = Enumerable.Range(1, 5)
            .Select(i => $"contract-test-{Guid.NewGuid()}-{i}")
            .ToList();

        // Act
        var locks = new List<IAsyncDisposable>();
        foreach (var resource in resources)
        {
            var lockHandle = await provider.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None);

            locks.Add(lockHandle!);
        }

        // Assert
        locks.Should().AllSatisfy(l => l.Should().NotBeNull());

        foreach (var lockHandle in locks)
        {
            await lockHandle.DisposeAsync();
        }
    }
}

/// <summary>
/// Contract tests for InMemory provider.
/// </summary>
public class InMemoryDistributedLockProviderContractTests : IDistributedLockProviderContractTests
{
    protected override IDistributedLockProvider CreateProvider()
    {
        return new InMemoryDistributedLockProvider(
            Options.Create(new InMemoryLockOptions()),
            NullLogger<InMemoryDistributedLockProvider>.Instance);
    }
}
