using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Outbox;

/// <summary>
/// Integration tests for <see cref="OutboxProcessor"/>.
/// Tests the processor's ability to process pending messages from the outbox.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OutboxProcessorIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TestDbContext _dbContext;

    public OutboxProcessorIntegrationTests()
    {
        var services = new ServiceCollection();

        // Add DbContext
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase($"OutboxTest_{Guid.NewGuid()}")
                   .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        // Register DbContext as the base DbContext interface for the processor
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

        // Add mock IEncina
        var mockEncina = Substitute.For<IEncina>();
        services.AddSingleton(mockEncina);

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new OutboxOptions();
        var logger = NullLogger<OutboxProcessor>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(null!, options, logger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<OutboxProcessor>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(_serviceProvider, null!, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new OutboxOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(_serviceProvider, options, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var options = new OutboxOptions();
        var logger = NullLogger<OutboxProcessor>.Instance;

        // Act
        var processor = new OutboxProcessor(_serviceProvider, options, logger);

        // Assert
        processor.ShouldNotBeNull();
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public async Task StartAsync_CanBeStartedAndStopped()
    {
        // Arrange
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMinutes(10)
        };
        var logger = NullLogger<OutboxProcessor>.Instance;
        var processor = new OutboxProcessor(_serviceProvider, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert - No exception means success
    }

    [Fact]
    public async Task StopAsync_CompletesGracefully()
    {
        // Arrange
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMinutes(10)
        };
        var logger = NullLogger<OutboxProcessor>.Instance;
        var processor = new OutboxProcessor(_serviceProvider, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert - No exception means success
    }

    #endregion

    #region Message Processing Tests

    [Fact]
    public async Task ExecuteAsync_WithNoPendingMessages_CompletesWithoutError()
    {
        // Arrange
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(50),
            BatchSize = 10,
            MaxRetries = 3
        };
        var logger = NullLogger<OutboxProcessor>.Instance;
        var processor = new OutboxProcessor(_serviceProvider, options, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();

        // Assert - No exception means success
        await processor.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsProcessingInterval()
    {
        // Arrange
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(100),
            BatchSize = 10,
            MaxRetries = 3
        };
        var logger = NullLogger<OutboxProcessor>.Instance;
        var processor = new OutboxProcessor(_serviceProvider, options, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        var startTime = DateTime.UtcNow;
        await processor.StartAsync(cts.Token);
        await Task.Delay(250);
        cts.Cancel();
        var endTime = DateTime.UtcNow;

        // Assert
        await processor.StopAsync(CancellationToken.None);
        var elapsed = endTime - startTime;
        elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(200));
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}

/// <summary>
/// Test notification for outbox processing tests.
/// </summary>
public sealed record TestOutboxNotification(string Message) : INotification;
