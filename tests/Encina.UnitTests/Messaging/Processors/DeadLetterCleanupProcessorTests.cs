using Encina.Messaging.DeadLetter;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Encina.UnitTests.Messaging.Processors;

/// <summary>
/// Unit tests for <see cref="DeadLetterCleanupProcessor"/>.
/// </summary>
public sealed class DeadLetterCleanupProcessorTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithNullScopeFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DeadLetterOptions();
        var logger = NullLogger<DeadLetterCleanupProcessor>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterCleanupProcessor(null!, options, logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var logger = NullLogger<DeadLetterCleanupProcessor>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterCleanupProcessor(scopeFactory, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new DeadLetterOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterCleanupProcessor(scopeFactory, options, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new DeadLetterOptions();
        var logger = NullLogger<DeadLetterCleanupProcessor>.Instance;

        // Act
        var processor = new DeadLetterCleanupProcessor(scopeFactory, options, logger);

        // Assert
        processor.ShouldNotBeNull();
    }

    #endregion

    #region ExecuteAsync - Disabled

    [Fact]
    public async Task ExecuteAsync_WhenCleanupDisabled_ExitsImmediately()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new DeadLetterOptions
        {
            EnableAutomaticCleanup = false
        };
        var logger = NullLogger<DeadLetterCleanupProcessor>.Instance;
        var processor = new DeadLetterCleanupProcessor(scopeFactory, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(50); // Give some time for the background task
            await processor.StopAsync(cts.Token);
        });

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoRetentionPeriod_ExitsImmediately()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new DeadLetterOptions
        {
            EnableAutomaticCleanup = true,
            RetentionPeriod = null
        };
        var logger = NullLogger<DeadLetterCleanupProcessor>.Instance;
        var processor = new DeadLetterCleanupProcessor(scopeFactory, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(50);
            await processor.StopAsync(cts.Token);
        });

        // Assert
        exception.ShouldBeNull();
    }

    #endregion

    #region ExecuteAsync - Enabled

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_StartAndStopWork()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        store.DeleteExpiredAsync(Arg.Any<CancellationToken>()).Returns(5);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDeadLetterStore)).Returns(store);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var options = new DeadLetterOptions
        {
            EnableAutomaticCleanup = true,
            RetentionPeriod = TimeSpan.FromDays(30),
            CleanupInterval = TimeSpan.FromMilliseconds(10)
        };
        var logger = NullLogger<DeadLetterCleanupProcessor>.Instance;
        var processor = new DeadLetterCleanupProcessor(scopeFactory, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(100); // Allow time for cleanup loop
            cts.Cancel();
            await processor.StopAsync(default);
        });

        // Assert - processor should start and stop without throwing
        // OperationCanceledException is acceptable when cancellation is requested
        if (exception is not null and not OperationCanceledException)
        {
            exception.ShouldBeNull();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ExitsGracefully()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new DeadLetterOptions
        {
            EnableAutomaticCleanup = true,
            RetentionPeriod = TimeSpan.FromDays(30),
            CleanupInterval = TimeSpan.FromSeconds(1)
        };
        var logger = NullLogger<DeadLetterCleanupProcessor>.Instance;
        var processor = new DeadLetterCleanupProcessor(scopeFactory, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await processor.StartAsync(cts.Token);
            cts.Cancel();
            await processor.StopAsync(default);
        });

        // Assert - should exit gracefully without throwing
        exception.ShouldBeNull();
    }

    #endregion

    #region ExecuteAsync - Error Handling

    [Fact]
    public async Task ExecuteAsync_WhenStoreThrows_ContinuesProcessing()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        store.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDeadLetterStore)).Returns(store);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var options = new DeadLetterOptions
        {
            EnableAutomaticCleanup = true,
            RetentionPeriod = TimeSpan.FromDays(30),
            CleanupInterval = TimeSpan.FromMilliseconds(10)
        };
        var logger = NullLogger<DeadLetterCleanupProcessor>.Instance;
        var processor = new DeadLetterCleanupProcessor(scopeFactory, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(50);
            cts.Cancel();
            await processor.StopAsync(default);
        });

        // Assert - should not throw despite store errors (errors are logged, not propagated)
        exception.ShouldBeNull();
    }

    #endregion
}
