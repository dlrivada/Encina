namespace Encina.Caching.GuardTests;

/// <summary>
/// Guard tests for <see cref="MemoryDistributedLockProvider"/> to verify null parameter handling.
/// </summary>
public class MemoryDistributedLockProviderGuardTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MemoryDistributedLockProvider(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that AcquireAsync throws ArgumentNullException when resource is null.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_NullResource_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string resource = null!;

        // Act & Assert
        Func<Task> act = () => provider.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resource");
    }

    /// <summary>
    /// Verifies that TryAcquireAsync throws ArgumentNullException when resource is null.
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_NullResource_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string resource = null!;

        // Act & Assert
        Func<Task> act = () => provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resource");
    }

    /// <summary>
    /// Verifies that IsLockedAsync throws ArgumentNullException when resource is null.
    /// </summary>
    [Fact]
    public async Task IsLockedAsync_NullResource_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string resource = null!;

        // Act & Assert
        Func<Task> act = () => provider.IsLockedAsync(resource, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resource");
    }

    /// <summary>
    /// Verifies that ExtendAsync throws ArgumentNullException when resource is null.
    /// </summary>
    [Fact]
    public async Task ExtendAsync_NullResource_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string resource = null!;

        // Act & Assert
        Func<Task> act = () => provider.ExtendAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resource");
    }

    private static MemoryDistributedLockProvider CreateProvider()
    {
        return new MemoryDistributedLockProvider(NullLogger<MemoryDistributedLockProvider>.Instance);
    }
}
