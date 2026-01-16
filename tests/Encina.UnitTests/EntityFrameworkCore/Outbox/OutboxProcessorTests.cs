using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxProcessor"/>.
/// </summary>
public sealed class OutboxProcessorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(null!, options, logger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<OutboxProcessor>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(serviceProvider, null!, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new OutboxOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(serviceProvider, options, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions();

        // Act
        var processor = new OutboxProcessor(serviceProvider, options, logger);

        // Assert
        processor.ShouldNotBeNull();
    }

    #endregion

    #region StartAsync/StopAsync Tests

    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMinutes(10) // Long interval to prevent processing
        };
        var processor = new OutboxProcessor(serviceProvider, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(10);
            cts.Cancel();
            await processor.StopAsync(CancellationToken.None);
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMinutes(10)
        };
        var processor = new OutboxProcessor(serviceProvider, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await processor.StartAsync(cts.Token);
            cts.Cancel();
            await processor.StopAsync(CancellationToken.None);
        });

        // Assert
        Assert.Null(exception);
    }

    #endregion
}
