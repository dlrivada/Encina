using Encina.EntityFrameworkCore.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReadWriteDbContextFactory{TContext}"/>.
/// </summary>
public sealed class ReadWriteDbContextFactoryTests
{
    private const string WriteConnectionString = "Server=primary;Database=test;";
    private const string ReadConnectionString = "Server=replica;Database=test;";

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test")
            .Options;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ReadWriteDbContextFactory<TestDbContext>(
            null!,
            connectionSelector,
            options));
    }

    [Fact]
    public void Constructor_WithNullConnectionSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test")
            .Options;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            null!,
            options));
    }

    [Fact]
    public void Constructor_WithNullBaseOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            null!));
    }

    [Fact]
    public void CreateWriteContext_CallsGetWriteConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetWriteConnectionString().Returns(WriteConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-write")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        // Act
        var context = factory.CreateWriteContext();

        // Assert
        connectionSelector.Received(1).GetWriteConnectionString();
        context.ShouldNotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateReadContext_CallsGetReadConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetReadConnectionString().Returns(ReadConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-read")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        // Act
        var context = factory.CreateReadContext();

        // Assert
        connectionSelector.Received(1).GetReadConnectionString();
        context.ShouldNotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateContext_CallsGetConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetConnectionString().Returns(WriteConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-context")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        // Act
        var context = factory.CreateContext();

        // Assert
        connectionSelector.Received(1).GetConnectionString();
        context.ShouldNotBeNull();
        context.Dispose();
    }

    [Fact]
    public async Task CreateWriteContextAsync_ReturnsContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetWriteConnectionString().Returns(WriteConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-write-async")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        // Act
        var context = await factory.CreateWriteContextAsync();

        // Assert
        context.ShouldNotBeNull();
        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateReadContextAsync_ReturnsContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetReadConnectionString().Returns(ReadConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-read-async")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        // Act
        var context = await factory.CreateReadContextAsync();

        // Assert
        context.ShouldNotBeNull();
        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateContextAsync_ReturnsContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetConnectionString().Returns(WriteConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-context-async")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        // Act
        var context = await factory.CreateContextAsync();

        // Assert
        context.ShouldNotBeNull();
        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateWriteContextAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetWriteConnectionString().Returns(WriteConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-cancel")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await factory.CreateWriteContextAsync(cts.Token));
    }

    [Fact]
    public async Task CreateReadContextAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetReadConnectionString().Returns(ReadConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-cancel-read")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await factory.CreateReadContextAsync(cts.Token));
    }

    [Fact]
    public async Task CreateContextAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetConnectionString().Returns(WriteConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-cancel-context")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await factory.CreateContextAsync(cts.Token));
    }

    [Fact]
    public void CreateWriteContext_CreatesNewContextEachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.GetWriteConnectionString().Returns(WriteConnectionString);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-multiple")
            .Options;

        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        // Act
        var context1 = factory.CreateWriteContext();
        var context2 = factory.CreateWriteContext();

        // Assert
        context1.ShouldNotBeSameAs(context2);
        context1.Dispose();
        context2.Dispose();
    }

    [Fact]
    public void ImplementsIReadWriteDbContextFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-interface")
            .Options;

        // Act
        var factory = new ReadWriteDbContextFactory<TestDbContext>(
            serviceProvider,
            connectionSelector,
            options);

        // Assert
        (factory is IReadWriteDbContextFactory<TestDbContext>).ShouldBeTrue();
    }

    /// <summary>
    /// Simple test DbContext for testing purposes.
    /// </summary>
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }
    }
}
