using System.Text.Json;
using Encina.Messaging.Outbox;
using Encina.MongoDB;
using Encina.MongoDB.Outbox;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - required for NSubstitute mocking pattern

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Outbox;

/// <summary>
/// Integration tests for the hosted <see cref="OutboxProcessor"/> registered by
/// <c>AddEncinaMongoDB</c> (#1289): a message whose first publish attempt fails must be picked
/// up again and delivered by the background processor on a later cycle, instead of being
/// dispatched only through the request post-processor path.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class OutboxProcessorMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public OutboxProcessorMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseOutbox = true
        });
    }

    public async ValueTask InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var collection = _fixture.Database!.GetCollection<OutboxMessage>(_options.Value.Collections.Outbox);
            await collection.DeleteManyAsync(Builders<OutboxMessage>.Filter.Empty);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task HostedProcessor_FirstPublishFails_RetriesAndProcessesOnLaterCycle()
    {
        // Arrange: a message ready for immediate pickup, and an IEncina whose first Publish call
        // fails (simulating a transient error) and whose second call succeeds.
        var store = new OutboxStoreMongoDB(_fixture.Client!, _options, NullLogger<OutboxStoreMongoDB>.Instance);
        var factory = new OutboxMessageFactory();
        var message = factory.Create(
            Guid.NewGuid(),
            typeof(OutboxProcessorIntegrationNotification).AssemblyQualifiedName!,
            JsonSerializer.Serialize(new OutboxProcessorIntegrationNotification("payload")),
            DateTime.UtcNow.AddMinutes(-1));
        (await store.AddAsync(message)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var publishCalls = 0;
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref publishCalls);
                Either<EncinaError, Unit> result = publishCalls == 1
                    ? Left(EncinaErrors.Create("handler.rejected", "Simulated transient failure"))
                    : Right(Unit.Default);
                return new ValueTask<Either<EncinaError, Unit>>(result);
            });

        var services = new ServiceCollection();
        services.AddScoped<IOutboxStore>(_ =>
            new OutboxStoreMongoDB(_fixture.Client!, _options, NullLogger<OutboxStoreMongoDB>.Instance));
        services.AddSingleton(encina);
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var outboxOptions = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(100),
            BatchSize = 10,
            MaxRetries = 5,
            BaseRetryDelay = TimeSpan.FromMilliseconds(100),
            MaxRetryDelay = TimeSpan.FromMilliseconds(500),
            RetryJitterRatio = 0
        };

        var processor = new global::Encina.MongoDB.Outbox.OutboxProcessor(
            provider,
            NullLogger<global::Encina.MongoDB.Outbox.OutboxProcessor>.Instance,
            outboxOptions);

        using var cts = new CancellationTokenSource();

        // Act: run the hosted processor across multiple cycles until the retried message is processed.
        var collection = _fixture.Database!.GetCollection<OutboxMessage>(_options.Value.Collections.Outbox);
        await processor.StartAsync(cts.Token);
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            OutboxMessage? persisted = null;
            while (DateTime.UtcNow < deadline)
            {
                persisted = await collection.Find(m => m.Id == message.Id).FirstOrDefaultAsync();
                if (persisted?.ProcessedAtUtc is not null)
                {
                    break;
                }

                await Task.Delay(100, CancellationToken.None);
            }

            // Assert: the message was retried at least once (first Publish failed) and eventually
            // processed by the background processor, not by a single inline attempt.
            publishCalls.ShouldBeGreaterThanOrEqualTo(2);
            persisted.ShouldNotBeNull();
            persisted!.ProcessedAtUtc.ShouldNotBeNull();
            persisted.RetryCount.ShouldBeGreaterThanOrEqualTo(1);
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(CancellationToken.None);
        }
    }
}

/// <summary>
/// Notification used by <see cref="OutboxProcessorMongoDBIntegrationTests"/>.
/// </summary>
/// <param name="Value">An arbitrary payload value.</param>
public sealed record OutboxProcessorIntegrationNotification(string Value) : INotification;
