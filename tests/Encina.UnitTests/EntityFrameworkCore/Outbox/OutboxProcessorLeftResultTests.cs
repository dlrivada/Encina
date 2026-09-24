using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.EntityFrameworkCore.Outbox;

/// <summary>
/// Regression tests for #1151 and #1150, driven end-to-end through the real EF Core
/// <see cref="OutboxProcessor"/> against an EF Core InMemory database: a <c>Left</c> returned by
/// <c>IEncina.Publish</c> must schedule a retry instead of marking the message processed, and the
/// failure that uses up the retries must leave the message unprocessed with no next retry.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OutboxProcessorLeftResultTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TestDbContext _dbContext;
    private readonly IEncina _mockEncina;

    public OutboxProcessorLeftResultTests()
    {
        var services = new ServiceCollection();

        // The database name is captured once and reused by every DbContext instance the container
        // creates (root scope and every processor-owned scope alike). Evaluating Guid.NewGuid()
        // inside the configuration delegate would hand each scope a different InMemory database.
        var databaseName = $"OutboxLeftResult_{Guid.NewGuid()}";
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName)
                   .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

        _mockEncina = Substitute.For<IEncina>();
        services.AddSingleton(_mockEncina);

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPublishReturnsLeft_ShouldNotMarkAsProcessed_AndShouldScheduleRetry()
    {
        // Arrange
        var messageId = await SeedMessageAsync(retryCount: 0);
        ReturnLeftFromPublish();
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            BatchSize = 10,
            MaxRetries = 3,
            BaseRetryDelay = TimeSpan.FromHours(1),
            MaxRetryDelay = TimeSpan.FromHours(1)
        };

        // Act
        await RunUntilAsync(options, async context =>
            (await context.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == messageId)).RetryCount > 0);

        // Assert
        await _mockEncina.Received().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        var persisted = await ReadAsync(messageId);
        persisted.ProcessedAtUtc.ShouldBeNull();
        persisted.RetryCount.ShouldBe(1);
        persisted.ErrorMessage.ShouldBe("handler.rejected");
        persisted.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailureUsesUpRetries_ShouldLeaveMessageUnprocessedWithoutNextRetry()
    {
        // Arrange
        var messageId = await SeedMessageAsync(retryCount: 2);
        ReturnLeftFromPublish();
        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            BatchSize = 10,
            MaxRetries = 3
        };

        // Act
        await RunUntilAsync(options, async context =>
            (await context.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == messageId)).RetryCount >= 3);

        // Assert
        var persisted = await ReadAsync(messageId);
        persisted.ProcessedAtUtc.ShouldBeNull();
        persisted.RetryCount.ShouldBe(3);
        persisted.NextRetryAtUtc.ShouldBeNull();
    }

    private async Task<Guid> SeedMessageAsync(int retryCount)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = typeof(TestOutboxNotification).AssemblyQualifiedName!,
            Content = "{\"Message\":\"test\"}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = retryCount
        };
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();
        return message.Id;
    }

    private void ReturnLeftFromPublish()
    {
        var handlerError = EncinaErrors.Create("handler.rejected", "Handler rejected the notification");
        _mockEncina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Left(handlerError)));
    }

    private async Task RunUntilAsync(OutboxOptions options, Func<TestDbContext, Task<bool>> condition)
    {
        var processor = new OutboxProcessor(_serviceProvider, options, NullLogger<OutboxProcessor>.Instance);
        using var cts = new CancellationTokenSource();

        await processor.StartAsync(cts.Token);
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                if (await condition(context))
                {
                    return;
                }

                await Task.Delay(20);
            }
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(CancellationToken.None);
        }
    }

    private async Task<OutboxMessage> ReadAsync(Guid messageId)
    {
        // Re-read from a fresh scope to observe exactly what the processor persisted.
        await using var verificationScope = _serviceProvider.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<TestDbContext>();
        return await verificationContext.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == messageId);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
