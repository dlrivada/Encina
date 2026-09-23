using LanguageExt;

namespace Encina.Messaging.Inbox;

/// <summary>
/// Pipeline behavior that implements the Inbox Pattern for idempotent message processing.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior ensures that requests marked with <see cref="IIdempotentRequest"/> are
/// processed exactly once, even if received multiple times. It uses the MessageId from
/// the request context to track processed messages.
/// </para>
/// <para>
/// This is a provider-agnostic implementation that delegates to <see cref="InboxOrchestrator"/>
/// for all domain logic and <see cref="IInboxStore"/> for persistence.
/// </para>
/// <para>
/// A request sent from inside another dispatch inherits no idempotency key (see
/// <see cref="RequestContextDispatchExtensions.IsNestedDispatch"/>); without a key of its own it runs
/// without an inbox entry, covered by the entry point's one. To deduplicate it separately, send it
/// with an explicit context that carries its own key.
/// </para>
/// </remarks>
public sealed class InboxPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly InboxOrchestrator _orchestrator;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="orchestrator">The inbox orchestrator.</param>
    public InboxPipelineBehavior(InboxOrchestrator orchestrator)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        _orchestrator = orchestrator;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Only process if request is idempotent
        if (request is not IIdempotentRequest)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // A request sent from inside another dispatch without a key of its own: the key belongs to
        // the entry point, whose inbox entry already covers this nested work.
        if (string.IsNullOrWhiteSpace(context.IdempotencyKey) && context.IsNestedDispatch())
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Validate message ID — return early if validation fails
        var validationError = _orchestrator.ValidateMessageId(
            context.IdempotencyKey,
            typeof(TRequest).Name,
            context.CorrelationId);

        if (validationError.IsSome)
        {
            return (EncinaError)validationError;
        }

        // Process through orchestrator
        var metadata = new InboxMetadata
        {
            CorrelationId = context.CorrelationId,
            UserId = context.UserId,
            TenantId = context.TenantId,
            Timestamp = context.Timestamp
        };

        return await _orchestrator.ProcessAsync<TResponse>(
            context.IdempotencyKey!,
            typeof(TRequest).AssemblyQualifiedName ?? typeof(TRequest).FullName ?? typeof(TRequest).Name,
            context.CorrelationId,
            metadata,
            () => nextStep(),
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Marker interface to indicate that a request should be processed idempotently using the Inbox Pattern.
/// </summary>
/// <remarks>
/// <para>
/// Requests implementing this interface will be tracked in the inbox to ensure exactly-once processing.
/// The MessageId (from <c>IRequestContext.IdempotencyKey</c>) is used as the deduplication key.
/// </para>
/// </remarks>
public interface IIdempotentRequest
{
}
