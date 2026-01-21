using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.Tenancy.AspNetCore;

/// <summary>
/// Unit tests for TenantResolverChain.
/// </summary>
public class TenantResolverChainTests
{
    [Fact]
    public void Constructor_NullResolvers_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantResolverChain(null!));
    }

    [Fact]
    public void Constructor_EmptyResolvers_CreatesEmptyChain()
    {
        // Act
        var chain = new TenantResolverChain([]);

        // Assert
        chain.Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_OrdersByPriority()
    {
        // Arrange
        var lowPriority = new TestResolver(100, "low");
        var highPriority = new TestResolver(10, "high");
        var mediumPriority = new TestResolver(50, "medium");

        // Act
        var chain = new TenantResolverChain([lowPriority, highPriority, mediumPriority]);

        // Assert
        chain.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ResolveAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var chain = new TenantResolverChain([]);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await chain.ResolveAsync(null!));
    }

    [Fact]
    public async Task ResolveAsync_EmptyChain_ReturnsNull()
    {
        // Arrange
        var chain = new TenantResolverChain([]);
        var context = new DefaultHttpContext();

        // Act
        var result = await chain.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_FirstResolverReturnsValue_ReturnsValue()
    {
        // Arrange
        var resolver1 = new TestResolver(10, "tenant-from-first");
        var resolver2 = new TestResolver(20, "tenant-from-second");

        var chain = new TenantResolverChain([resolver1, resolver2]);
        var context = new DefaultHttpContext();

        // Act
        var result = await chain.ResolveAsync(context);

        // Assert
        result.ShouldBe("tenant-from-first");
    }

    [Fact]
    public async Task ResolveAsync_FirstResolverReturnsNull_TrysSecond()
    {
        // Arrange
        var resolver1 = new TestResolver(10, null);
        var resolver2 = new TestResolver(20, "tenant-from-second");

        var chain = new TenantResolverChain([resolver1, resolver2]);
        var context = new DefaultHttpContext();

        // Act
        var result = await chain.ResolveAsync(context);

        // Assert
        result.ShouldBe("tenant-from-second");
    }

    [Fact]
    public async Task ResolveAsync_AllResolversReturnNull_ReturnsNull()
    {
        // Arrange
        var resolver1 = new TestResolver(10, null);
        var resolver2 = new TestResolver(20, null);

        var chain = new TenantResolverChain([resolver1, resolver2]);
        var context = new DefaultHttpContext();

        // Act
        var result = await chain.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_ResolverReturnsEmpty_TrysNext()
    {
        // Arrange
        var resolver1 = new TestResolver(10, "");
        var resolver2 = new TestResolver(20, "tenant-from-second");

        var chain = new TenantResolverChain([resolver1, resolver2]);
        var context = new DefaultHttpContext();

        // Act
        var result = await chain.ResolveAsync(context);

        // Assert
        result.ShouldBe("tenant-from-second");
    }

    [Fact]
    public async Task ResolveAsync_ResolverReturnsWhitespace_TrysNext()
    {
        // Arrange
        var resolver1 = new TestResolver(10, "   ");
        var resolver2 = new TestResolver(20, "tenant-from-second");

        var chain = new TenantResolverChain([resolver1, resolver2]);
        var context = new DefaultHttpContext();

        // Act
        var result = await chain.ResolveAsync(context);

        // Assert
        result.ShouldBe("tenant-from-second");
    }

    [Fact]
    public async Task ResolveAsync_ExecutesInPriorityOrder()
    {
        // Arrange - all resolvers return null so the chain continues to the end
        var executionOrder = new List<int>();
        var resolver1 = new TrackingResolver(100, null, executionOrder);
        var resolver2 = new TrackingResolver(10, null, executionOrder);
        var resolver3 = new TrackingResolver(50, null, executionOrder);

        var chain = new TenantResolverChain([resolver1, resolver2, resolver3]);
        var context = new DefaultHttpContext();

        // Act
        await chain.ResolveAsync(context);

        // Assert - executed in priority order (low number first)
        executionOrder.ShouldBe([10, 50, 100]);
    }

    [Fact]
    public async Task ResolveAsync_StopsOnFirstNonNullResult()
    {
        // Arrange
        var executionOrder = new List<int>();
        var resolver1 = new TrackingResolver(10, null, executionOrder);
        var resolver2 = new TrackingResolver(20, "found", executionOrder);
        var resolver3 = new TrackingResolver(30, "not-reached", executionOrder);

        var chain = new TenantResolverChain([resolver1, resolver2, resolver3]);
        var context = new DefaultHttpContext();

        // Act
        var result = await chain.ResolveAsync(context);

        // Assert
        result.ShouldBe("found");
        executionOrder.ShouldBe([10, 20]);
        executionOrder.ShouldNotContain(30);
    }

    private sealed class TestResolver : ITenantResolver
    {
        private readonly string? _tenantId;

        public TestResolver(int priority, string? tenantId)
        {
            Priority = priority;
            _tenantId = tenantId;
        }

        public int Priority { get; }

        public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_tenantId);
        }
    }

    private sealed class TrackingResolver : ITenantResolver
    {
        private readonly string? _tenantId;
        private readonly List<int> _executionOrder;

        public TrackingResolver(int priority, string? tenantId, List<int> executionOrder)
        {
            Priority = priority;
            _tenantId = tenantId;
            _executionOrder = executionOrder;
        }

        public int Priority { get; }

        public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            _executionOrder.Add(Priority);
            return ValueTask.FromResult(_tenantId);
        }
    }
}
