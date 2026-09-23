using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.EntityFrameworkCore.Outbox;

/// <summary>
/// Spike reproduction for finding C2 (verification spike, branch spike/verify-request-context) -
/// EF Core variant, driven end-to-end through the real <see cref="OutboxProcessor"/>
/// (src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs, ~line 169) against an EF Core
/// InMemory database (no SQLite - ADR-024 removed SQLite from the supported provider matrix).
/// See <c>Encina.UnitTests.ADO.SqlServer.Outbox.OutboxProcessorLeftResultSpikeTests</c> for the full
/// root-cause narrative shared by all seven processors.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OutboxProcessorLeftResultSpikeTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TestDbContext _dbContext;
    private readonly IEncina _mockEncina;

    public OutboxProcessorLeftResultSpikeTests()
    {
        var services = new ServiceCollection();

        // The database name is captured once and reused by every DbContext instance the DI
        // container creates (root scope and every processor-owned inner scope alike). Evaluating
        // Guid.NewGuid() inside the AddDbContext configuration delegate would hand each scope a
        // different InMemory database, silently hiding the seeded message from the processor.
        var databaseName = $"OutboxLeftResultSpike_{Guid.NewGuid()}";
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName)
                   .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

        _mockEncina = Substitute.For<IEncina>();
        services.AddSingleton(_mockEncina);

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
    }

    /// <summary>
    /// CONFIRMED (C2, EF Core): identical to the ADO.SqlServer reproduction, but exercised through
    /// the EF Core processor, which marks messages processed by setting <c>message.ProcessedAtUtc</c>
    /// directly (src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs, ~lines 169-175) rather than
    /// through a store method - same discarded <see cref="Either{EncinaError, Unit}"/>, same result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenPublishReturnsLeft_ShouldNotMarkAsProcessed_AndShouldScheduleRetry()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = typeof(TestOutboxNotification).AssemblyQualifiedName!,
            Content = "{\"Message\":\"test\"}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        var handlerError = EncinaErrors.Create("handler.rejected", "Handler rejected the notification");
        _mockEncina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Left(handlerError)));

        var options = new OutboxOptions
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            BatchSize = 10,
            MaxRetries = 3
        };
        var logger = NullLogger<OutboxProcessor>.Instance;
        var processor = new OutboxProcessor(_serviceProvider, options, logger);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Sanity check: the handler failure did reach Publish.
        await _mockEncina.Received().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());

        // Re-read from a fresh scope/DbContext instance backed by the same InMemory database name,
        // to observe exactly what the processor persisted (not a cached tracked entity).
        await using var verificationScope = _serviceProvider.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var persisted = await verificationContext.OutboxMessages.SingleAsync(m => m.Id == messageId);

        // Expected (correct) behavior: a Left from Publish should NOT be recorded as processed, and
        // should increment RetryCount / set NextRetryAtUtc for a retry.
        // Actual (current, buggy) behavior: ProcessedAtUtc is set and RetryCount stays 0, because the
        // Either returned by Publish is discarded and no exception was thrown.
        persisted.ProcessedAtUtc.ShouldBeNull();
        persisted.RetryCount.ShouldBeGreaterThan(0);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
