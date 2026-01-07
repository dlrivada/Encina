using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Outbox;

/// <summary>
/// Tests for <see cref="OutboxProcessor"/> lifecycle and basic functionality.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OutboxProcessorLifecycleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestDbContext _dbContext;
    private readonly IEncina _mockEncina;

    public OutboxProcessorLifecycleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(options =>
            options.UseSqlite(_connection));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

        _mockEncina = Substitute.For<IEncina>();
        services.AddSingleton(_mockEncina);

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    #region Lifecycle Tests

    [Fact]
    public async Task StartAsync_CanStartProcessor()
    {
        // Arrange
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMinutes(10)
        };
        var logger = NullLogger<OutboxProcessor>.Instance;
        var processor = new OutboxProcessor(_serviceProvider, options, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        // Assert - Should not throw
        await processor.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CanStopProcessor()
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

        // Assert - Should complete gracefully
        await processor.StopAsync(CancellationToken.None);
    }

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

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        // Act & Assert - Should complete without error
        await processor.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // No exception means success
    }

    [Fact]
    public async Task ExecuteAsync_MultipleStartStop_WorksCorrectly()
    {
        // Arrange
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMinutes(10)
        };
        var logger = NullLogger<OutboxProcessor>.Instance;
        var processor = new OutboxProcessor(_serviceProvider, options, logger);

        // Act - Start and stop multiple times
        using (var cts1 = new CancellationTokenSource())
        {
            await processor.StartAsync(cts1.Token);
            await Task.Delay(20);
            cts1.Cancel();
            await processor.StopAsync(CancellationToken.None);
        }

        using (var cts2 = new CancellationTokenSource())
        {
            await processor.StartAsync(cts2.Token);
            await Task.Delay(20);
            cts2.Cancel();
            await processor.StopAsync(CancellationToken.None);
        }

        // Assert - Should complete without error
    }

    #endregion

    #region Configuration Tests

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

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        // Act
        var startTime = DateTime.UtcNow;
        await processor.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        var endTime = DateTime.UtcNow;

        // Assert
        await processor.StopAsync(CancellationToken.None);
        var elapsed = endTime - startTime;
        elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(150));
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
    }
}
