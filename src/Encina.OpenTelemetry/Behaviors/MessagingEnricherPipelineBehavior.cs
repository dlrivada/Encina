using System.Diagnostics;
using LanguageExt;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.Enrichers;

namespace Encina.OpenTelemetry.Behaviors;

/// <summary>
/// Pipeline behavior that automatically enriches OpenTelemetry activities with messaging pattern context.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class MessagingEnricherPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            EnrichActivityWithMessagingContext(activity, context);
        }

        return await nextStep().ConfigureAwait(false);
    }

    private static void EnrichActivityWithMessagingContext(Activity activity, IRequestContext context)
    {
        var hasMessagingContext = false;

        // Check for Outbox message in context
        if (TryGetMetadata<IOutboxMessage>(context, "OutboxMessage", out var outboxMessage))
        {
            MessagingActivityEnricher.EnrichWithOutboxMessage(activity, outboxMessage);
            hasMessagingContext = true;
        }

        // Check for Inbox message in context
        if (TryGetMetadata<IInboxMessage>(context, "InboxMessage", out var inboxMessage))
        {
            MessagingActivityEnricher.EnrichWithInboxMessage(activity, inboxMessage);
            hasMessagingContext = true;
        }

        // Check for Saga state in context
        if (TryGetMetadata<ISagaState>(context, "SagaState", out var sagaState))
        {
            MessagingActivityEnricher.EnrichWithSagaState(activity, sagaState);
            hasMessagingContext = true;
        }

        // Check for Scheduled message in context
        if (TryGetMetadata<IScheduledMessage>(context, "ScheduledMessage", out var scheduledMessage))
        {
            MessagingActivityEnricher.EnrichWithScheduledMessage(activity, scheduledMessage);
            hasMessagingContext = true;
        }

        if (hasMessagingContext)
        {
            activity.SetTag("simplemediator.messaging_enabled", true);
        }
    }

    private static bool TryGetMetadata<T>(IRequestContext context, string key, out T value)
        where T : class
    {
        if (context.Metadata.TryGetValue(key, out var obj) && obj is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }
}
