using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.DistributedLock.GuardTests;

/// <summary>
/// Guard clause tests for InMemoryDistributedLockProvider.
/// </summary>
public class InMemoryDistributedLockProviderGuardTests
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        IOptions<InMemoryLockOptions>? options = null;
        var logger = NullLogger<InMemoryDistributedLockProvider>.Instance;

        // Act
        var act = () => new InMemoryDistributedLockProvider(options!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new InMemoryLockOptions());
        ILogger<InMemoryDistributedLockProvider>? logger = null;

        // Act
        var act = () => new InMemoryDistributedLockProvider(options, logger!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task TryAcquireAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.TryAcquireAsync(
            null!,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }

    [Fact]
    public async Task AcquireAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.AcquireAsync(
            null!,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }

    [Fact]
    public async Task IsLockedAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.IsLockedAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }

    [Fact]
    public async Task ExtendAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.ExtendAsync(
            null!,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resource");
    }

    private static InMemoryDistributedLockProvider CreateProvider()
    {
        return new InMemoryDistributedLockProvider(
            Options.Create(new InMemoryLockOptions()),
            NullLogger<InMemoryDistributedLockProvider>.Instance);
    }
}
