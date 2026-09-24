using System.Text.Json;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.Messaging.Outbox;

/// <summary>
/// Runs the delivery rules of #1151 and #1150 through the <c>OutboxProcessor</c> of every ADO.NET,
/// Dapper and MongoDB provider, so that a provider that stops deriving from
/// <see cref="OutboxProcessorBase"/> or overrides its store resolution incorrectly is caught. The
/// EF Core processor is covered end to end by
/// <c>Encina.UnitTests.EntityFrameworkCore.Outbox.OutboxProcessorLeftResultTests</c>.
/// </summary>
public sealed class OutboxProcessorProvidersTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    public static TheoryData<string> Providers =>
    [
        "ADO.SqlServer",
        "ADO.PostgreSQL",
        "ADO.MySQL",
        "Dapper.SqlServer",
        "Dapper.PostgreSQL",
        "Dapper.MySQL",
        "MongoDB"
    ];

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Processor_PublishReturnsLeft_SchedulesRetryInsteadOfMarkingProcessed(string provider)
    {
        var message = CreateMessage(retryCount: 1);
        var harness = new Harness(message, Left<EncinaError, Unit>(EncinaErrors.Create("handler.rejected", "Rejected")));

        await harness.RunOneFailureAsync(provider, Options(maxRetries: 5));

        await harness.Store.Received(1).MarkAsFailedAsync(
            message.Id,
            "Rejected",
            Now.UtcDateTime.AddSeconds(10),
            Arg.Any<CancellationToken>());
        await harness.Store.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Processor_FailureUsingUpRetries_RecordsNoNextRetry(string provider)
    {
        var message = CreateMessage(retryCount: 2);
        var harness = new Harness(message, Left<EncinaError, Unit>(EncinaErrors.Create("handler.rejected", "Rejected")));

        await harness.RunOneFailureAsync(provider, Options(maxRetries: 3));

        await harness.Store.Received(1).MarkAsFailedAsync(message.Id, "Rejected", null, Arg.Any<CancellationToken>());
        await harness.Store.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default);
    }

    private static BackgroundService CreateProcessor(string provider, IServiceProvider services, OutboxOptions options, TimeProvider timeProvider) => provider switch
    {
        "ADO.SqlServer" => new global::Encina.ADO.SqlServer.Outbox.OutboxProcessor(services, NullLogger<global::Encina.ADO.SqlServer.Outbox.OutboxProcessor>.Instance, options, timeProvider),
        "ADO.PostgreSQL" => new global::Encina.ADO.PostgreSQL.Outbox.OutboxProcessor(services, NullLogger<global::Encina.ADO.PostgreSQL.Outbox.OutboxProcessor>.Instance, options, timeProvider),
        "ADO.MySQL" => new global::Encina.ADO.MySQL.Outbox.OutboxProcessor(services, NullLogger<global::Encina.ADO.MySQL.Outbox.OutboxProcessor>.Instance, options, timeProvider),
        "Dapper.SqlServer" => new global::Encina.Dapper.SqlServer.Outbox.OutboxProcessor(services, NullLogger<global::Encina.Dapper.SqlServer.Outbox.OutboxProcessor>.Instance, options, timeProvider),
        "Dapper.PostgreSQL" => new global::Encina.Dapper.PostgreSQL.Outbox.OutboxProcessor(services, NullLogger<global::Encina.Dapper.PostgreSQL.Outbox.OutboxProcessor>.Instance, options, timeProvider),
        "Dapper.MySQL" => new global::Encina.Dapper.MySQL.Outbox.OutboxProcessor(services, NullLogger<global::Encina.Dapper.MySQL.Outbox.OutboxProcessor>.Instance, options, timeProvider),
        "MongoDB" => new global::Encina.MongoDB.Outbox.OutboxProcessor(services, NullLogger<global::Encina.MongoDB.Outbox.OutboxProcessor>.Instance, options, timeProvider),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private static OutboxOptions Options(int maxRetries) => new()
    {
        EnableProcessor = true,
        ProcessingInterval = TimeSpan.FromMilliseconds(20),
        MaxRetries = maxRetries,
        BaseRetryDelay = TimeSpan.FromSeconds(5),
        RetryJitterRatio = 0
    };

    private static TestOutboxMessage CreateMessage(int retryCount) => new()
    {
        Id = Guid.NewGuid(),
        NotificationType = typeof(ProviderOutboxNotification).AssemblyQualifiedName!,
        Content = JsonSerializer.Serialize(new ProviderOutboxNotification("value")),
        CreatedAtUtc = Now.UtcDateTime,
        RetryCount = retryCount
    };

    private sealed class Harness
    {
        private readonly TaskCompletionSource _failed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IServiceProvider _services;

        public Harness(IOutboxMessage message, Either<EncinaError, Unit> publishResult)
        {
            var returned = false;
            Store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    IEnumerable<IOutboxMessage> batch = returned ? [] : [message];
                    returned = true;
                    return Right<EncinaError, IEnumerable<IOutboxMessage>>(batch);
                });
            Store.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(Unit.Default));
            Store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(Unit.Default))
                .AndDoes(_ => _failed.TrySetResult());
            Store.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(Unit.Default));

            var encina = Substitute.For<IEncina>();
            encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
                .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(publishResult));

            var scopedServices = Substitute.For<IServiceProvider>();
            scopedServices.GetService(typeof(IOutboxStore)).Returns(Store);
            scopedServices.GetService(typeof(IEncina)).Returns(encina);
            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(scopedServices);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);
            var services = Substitute.For<IServiceProvider>();
            services.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
            _services = services;
        }

        public IOutboxStore Store { get; } = Substitute.For<IOutboxStore>();

        public async Task RunOneFailureAsync(string provider, OutboxOptions options)
        {
            var processor = CreateProcessor(provider, _services, options, new FakeTimeProvider(Now));
            using var cts = new CancellationTokenSource();

            await processor.StartAsync(cts.Token);
            try
            {
                await _failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            finally
            {
                await cts.CancelAsync();
                await processor.StopAsync(CancellationToken.None);
            }
        }
    }
}

/// <summary>
/// Notification used by <see cref="OutboxProcessorProvidersTests"/>.
/// </summary>
/// <param name="Value">An arbitrary payload value.</param>
public sealed record ProviderOutboxNotification(string Value) : INotification;
