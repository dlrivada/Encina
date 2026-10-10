using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Serialization;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// A container with the dead letter queue registered on the in-memory <see cref="FakeDeadLetterStore"/>, so the
/// sources capture through the real <see cref="DeadLetterSourceCapture"/> and <see cref="DeadLetterOrchestrator"/>.
/// </summary>
internal sealed class DeadLetterCaptureHost : IDisposable
{
    private DeadLetterCaptureHost(ServiceProvider provider, FakeTimeProvider clock)
    {
        Provider = provider;
        Clock = clock;
        Store = provider.GetRequiredService<FakeDeadLetterStore>();
        Capture = provider.GetRequiredService<DeadLetterSourceCapture>();
    }

    public ServiceProvider Provider { get; }

    public FakeTimeProvider Clock { get; }

    public FakeDeadLetterStore Store { get; }

    public DeadLetterSourceCapture Capture { get; }

    public IMessageSerializer Serializer { get; } = new JsonMessageSerializer();

    /// <summary>Builds the host; <paramref name="configure"/> sets the <c>IntegrateWith*</c> flags.</summary>
    public static DeadLetterCaptureHost Create(
        Action<DeadLetterOptions>? configure = null,
        IDeadLetterStore? store = null,
        IEncina? encina = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(clock);
        if (encina is not null)
        {
            services.AddSingleton(encina);
        }

        services.AddFakeDeadLetterStore();
        if (store is not null)
        {
            services.AddSingleton(store);
        }

        var options = new DeadLetterOptions();
        configure?.Invoke(options);
        services.AddDeadLetterQueueServices<FakeDeadLetterStore, PassThroughFactory>(true, options);
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        return new DeadLetterCaptureHost(provider, clock);
    }

    /// <summary>The dead letters of <paramref name="sourcePattern"/> in the store.</summary>
    public IReadOnlyList<IDeadLetterMessage> DeadLettersOf(string sourcePattern)
        => [.. Store.GetMessages().Where(m => m.SourcePattern == sourcePattern)];

    public void Dispose() => Provider.Dispose();

    /// <summary>Maps the orchestrator's data one to one onto the fake message.</summary>
    public sealed class PassThroughFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => new FakeDeadLetterMessage
        {
            Id = data.Id,
            RequestType = data.RequestType,
            RequestContent = data.RequestContent,
            ErrorCode = data.ErrorCode,
            SourcePattern = data.SourcePattern,
            SourceMessageId = data.SourceMessageId,
            TotalRetryAttempts = data.TotalRetryAttempts,
            FirstFailedAtUtc = data.FirstFailedAtUtc,
            DeadLetteredAtUtc = data.DeadLetteredAtUtc,
            ExpiresAtUtc = data.ExpiresAtUtc,
            CorrelationId = data.CorrelationId,
            ExceptionType = data.ExceptionType,
            TenantId = data.TenantId
        };
    }

    /// <summary>Creates fake inbox messages.</summary>
    public sealed class InboxFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => new FakeInboxMessage
            {
                MessageId = messageId,
                RequestType = requestType,
                ReceivedAtUtc = receivedAtUtc,
                ExpiresAtUtc = expiresAtUtc
            };
    }

    /// <summary>Creates fake scheduled messages.</summary>
    public sealed class ScheduledFactory : IScheduledMessageFactory
    {
        public IScheduledMessage Create(
            Guid id, string requestType, string content, DateTime scheduledAtUtc, DateTime createdAtUtc, bool isRecurring, string? cronExpression)
            => new FakeScheduledMessage
            {
                Id = id,
                RequestType = requestType,
                Content = content,
                ScheduledAtUtc = scheduledAtUtc,
                CreatedAtUtc = createdAtUtc,
                IsRecurring = isRecurring,
                CronExpression = cronExpression
            };
    }

    /// <summary>Creates fake saga states.</summary>
    public sealed class SagaFactory : ISagaStateFactory
    {
        public ISagaState Create(
            Guid sagaId, string sagaType, string data, string status, int currentStep, DateTime startedAtUtc, DateTime? timeoutAtUtc = null)
            => new FakeSagaState
            {
                SagaId = sagaId,
                SagaType = sagaType,
                Data = data,
                Status = status,
                CurrentStep = currentStep,
                StartedAtUtc = startedAtUtc,
                LastUpdatedAtUtc = startedAtUtc,
                TimeoutAtUtc = timeoutAtUtc
            };
    }
}
