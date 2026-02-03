using Encina;
using Encina.EntityFrameworkCore.SoftDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.UnitTests.EntityFrameworkCore.SoftDelete;

public sealed class SoftDeleteInterceptorTests : IDisposable
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly SoftDeleteTestDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public SoftDeleteInterceptorTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero));

        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        var options = CreateDbContextOptions();
        _context = new SoftDeleteTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    private DbContextOptions<SoftDeleteTestDbContext> CreateDbContextOptions(
        bool enabled = true,
        bool trackDeletedAt = true,
        bool trackDeletedBy = true,
        bool logSoftDeletes = false,
        IRequestContext? requestContext = null)
    {
        var interceptorOptions = new SoftDeleteInterceptorOptions
        {
            Enabled = enabled,
            TrackDeletedAt = trackDeletedAt,
            TrackDeletedBy = trackDeletedBy,
            LogSoftDeletes = logSoftDeletes
        };

        var services = new ServiceCollection();
        if (requestContext is not null)
        {
            services.AddSingleton(requestContext);
        }
        var sp = services.BuildServiceProvider();

        var interceptor = new SoftDeleteInterceptor(
            sp,
            interceptorOptions,
            _timeProvider,
            NullLogger<SoftDeleteInterceptor>.Instance);

        return new DbContextOptionsBuilder<SoftDeleteTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityDeleted_ShouldConvertToSoftDelete()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using var context = new SoftDeleteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var orderId = Guid.NewGuid();
        var order = new TestSoftDeletableOrder
        {
            Id = orderId,
            CustomerName = "Test Customer",
            Total = 100m
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        // Assert - Entity should still exist but be soft deleted
        var deletedOrder = await context.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        deletedOrder.ShouldNotBeNull();
        deletedOrder.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityDeleted_ShouldSetDeletedAtUtc()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);

        var interceptorOptions = new SoftDeleteInterceptorOptions
        {
            Enabled = true,
            TrackDeletedAt = true
        };

        var sp = new ServiceCollection().BuildServiceProvider();
        var interceptor = new SoftDeleteInterceptor(
            sp,
            interceptorOptions,
            timeProvider,
            NullLogger<SoftDeleteInterceptor>.Instance);

        var options = new DbContextOptionsBuilder<SoftDeleteTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new SoftDeleteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var orderId = Guid.NewGuid();
        var order = new TestSoftDeletableOrder
        {
            Id = orderId,
            CustomerName = "Test Customer",
            Total = 100m
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        // Assert
        var deletedOrder = await context.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        deletedOrder.ShouldNotBeNull();
        deletedOrder.DeletedAtUtc.ShouldBe(fixedTime.UtcDateTime);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityDeleted_WithRequestContext_ShouldSetDeletedBy()
    {
        // Arrange
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.UserId.Returns("user-123");

        var options = CreateDbContextOptions(requestContext: requestContext);
        await using var context = new SoftDeleteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var orderId = Guid.NewGuid();
        var order = new TestSoftDeletableOrder
        {
            Id = orderId,
            CustomerName = "Test Customer",
            Total = 100m
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        // Assert
        var deletedOrder = await context.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        deletedOrder.ShouldNotBeNull();
        deletedOrder.DeletedBy.ShouldBe("user-123");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenInterceptorDisabled_ShouldPerformHardDelete()
    {
        // Arrange
        var options = CreateDbContextOptions(enabled: false);
        await using var context = new SoftDeleteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var orderId = Guid.NewGuid();
        var order = new TestSoftDeletableOrder
        {
            Id = orderId,
            CustomerName = "Test Customer",
            Total = 100m
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        // Assert - Entity should be completely removed
        var deletedOrder = await context.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        deletedOrder.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTrackDeletedAtDisabled_ShouldNotSetDeletedAtUtc()
    {
        // Arrange
        var options = CreateDbContextOptions(trackDeletedAt: false);
        await using var context = new SoftDeleteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var orderId = Guid.NewGuid();
        var order = new TestSoftDeletableOrder
        {
            Id = orderId,
            CustomerName = "Test Customer",
            Total = 100m
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        // Assert
        var deletedOrder = await context.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        deletedOrder.ShouldNotBeNull();
        deletedOrder.IsDeleted.ShouldBeTrue();
        deletedOrder.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTrackDeletedByDisabled_ShouldNotSetDeletedBy()
    {
        // Arrange
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.UserId.Returns("user-123");

        var options = CreateDbContextOptions(trackDeletedBy: false, requestContext: requestContext);
        await using var context = new SoftDeleteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var orderId = Guid.NewGuid();
        var order = new TestSoftDeletableOrder
        {
            Id = orderId,
            CustomerName = "Test Customer",
            Total = 100m
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        // Assert
        var deletedOrder = await context.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        deletedOrder.ShouldNotBeNull();
        deletedOrder.IsDeleted.ShouldBeTrue();
        deletedOrder.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_QueryFilter_ShouldExcludeSoftDeletedEntities()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using var context = new SoftDeleteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var activeOrderId = Guid.NewGuid();
        var deletedOrderId = Guid.NewGuid();

        context.Orders.Add(new TestSoftDeletableOrder
        {
            Id = activeOrderId,
            CustomerName = "Active Customer",
            Total = 100m
        });
        context.Orders.Add(new TestSoftDeletableOrder
        {
            Id = deletedOrderId,
            CustomerName = "Deleted Customer",
            Total = 200m
        });
        await context.SaveChangesAsync();

        // Soft delete one order
        var orderToDelete = await context.Orders.FindAsync(deletedOrderId);
        context.Orders.Remove(orderToDelete!);
        await context.SaveChangesAsync();

        // Act - Query without IgnoreQueryFilters
        var orders = await context.Orders.ToListAsync();

        // Assert
        orders.Count.ShouldBe(1);
        orders.First().Id.ShouldBe(activeOrderId);
    }

    [Fact]
    public async Task SaveChangesAsync_IgnoreQueryFilters_ShouldIncludeSoftDeletedEntities()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using var context = new SoftDeleteTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var activeOrderId = Guid.NewGuid();
        var deletedOrderId = Guid.NewGuid();

        context.Orders.Add(new TestSoftDeletableOrder
        {
            Id = activeOrderId,
            CustomerName = "Active Customer",
            Total = 100m
        });
        context.Orders.Add(new TestSoftDeletableOrder
        {
            Id = deletedOrderId,
            CustomerName = "Deleted Customer",
            Total = 200m
        });
        await context.SaveChangesAsync();

        // Soft delete one order
        var orderToDelete = await context.Orders.FindAsync(deletedOrderId);
        context.Orders.Remove(orderToDelete!);
        await context.SaveChangesAsync();

        // Act - Query with IgnoreQueryFilters
        var orders = await context.Orders.IgnoreQueryFilters().ToListAsync();

        // Assert
        orders.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new SoftDeleteInterceptorOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SoftDeleteInterceptor(
            null!,
            options,
            _timeProvider,
            NullLogger<SoftDeleteInterceptor>.Instance));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SoftDeleteInterceptor(
            _serviceProvider,
            null!,
            _timeProvider,
            NullLogger<SoftDeleteInterceptor>.Instance));
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new SoftDeleteInterceptorOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SoftDeleteInterceptor(
            _serviceProvider,
            options,
            null!,
            NullLogger<SoftDeleteInterceptor>.Instance));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new SoftDeleteInterceptorOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SoftDeleteInterceptor(
            _serviceProvider,
            options,
            _timeProvider,
            null!));
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
