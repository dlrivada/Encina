using Encina.Messaging.Diagnostics;
using Encina.Messaging.Serialization;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Outbox;

/// <summary>
/// Pipeline behavior that writes the notifications of an <see cref="IHasNotifications"/> request to the outbox
/// inside the request's database transaction, so the business change and its events commit or roll back
/// together.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// <para>
/// The behavior joins the scope's transaction when one is active (opened by the transaction behavior or a unit
/// of work) and begins one around the handler otherwise, so atomicity does not depend on registration order or on
/// <c>UseTransactions</c>. The flow is: begin or join, run the handler, read
/// <see cref="IHasNotifications.GetNotifications"/> after the handler, write them with the outbox store, commit.
/// A <see langword="Left"/> from the handler leaves no outbox row; a <see langword="Left"/> from the outbox write
/// or from the commit fails the request and rolls the business change back. Errors are never swallowed.
/// </para>
/// <para>
/// Requests that cannot carry notifications skip the behavior at no cost: a per-<typeparamref name="TRequest"/>
/// static check, then a runtime check, both before any transaction work and without allocating.
/// </para>
/// </remarks>
public sealed class TransactionalOutboxPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // A closed, sealed request type that does not implement IHasNotifications can never carry notifications;
    // an interface or abstract TRequest may still be implemented by a request that does.
    private static readonly bool CanHaveNotifications =
        typeof(IHasNotifications).IsAssignableFrom(typeof(TRequest))
        || typeof(TRequest).IsInterface
        || typeof(TRequest).IsAbstract;

    private static readonly string RequestName = typeof(TRequest).Name;

    private readonly ITransactionLeaseSource _transactionSource;
    private readonly OutboxWriter _writer;
    private readonly ILogger<TransactionalOutboxPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionalOutboxPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="transactionSource">The scope's transaction source, joined or begun around the handler.</param>
    /// <param name="outboxStore">The outbox store notifications are written to.</param>
    /// <param name="messageFactory">The factory for outbox messages.</param>
    /// <param name="messageSerializer">The serializer for the persisted payload (so message encryption applies).</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Optional time provider for the creation timestamp.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
    public TransactionalOutboxPipelineBehavior(
        ITransactionLeaseSource transactionSource,
        IOutboxStore outboxStore,
        IOutboxMessageFactory messageFactory,
        IMessageSerializer messageSerializer,
        ILogger<TransactionalOutboxPipelineBehavior<TRequest, TResponse>> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(transactionSource);
        ArgumentNullException.ThrowIfNull(outboxStore);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(messageSerializer);
        ArgumentNullException.ThrowIfNull(logger);

        _transactionSource = transactionSource;
        _logger = logger;
        _writer = new OutboxWriter(outboxStore, messageFactory, messageSerializer, timeProvider ?? TimeProvider.System);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        if (!CanHaveNotifications || request is not IHasNotifications hasNotifications)
        {
            MessagingLog.OutboxBehaviorSkipped(_logger, RequestName);
            return nextStep();
        }

        return HandleInTransactionAsync(hasNotifications, nextStep, cancellationToken);
    }

    private async ValueTask<Either<EncinaError, TResponse>> HandleInTransactionAsync(
        IHasNotifications request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var begun = await _transactionSource.BeginOrJoinAsync(cancellationToken).ConfigureAwait(false);
        if (begun.IsLeft)
        {
            return (EncinaError)begun;
        }

        await using var lease = begun.RequireLease();

        try
        {
            var result = await nextStep().ConfigureAwait(false);
            if (result.IsLeft)
            {
                await lease.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return result;
            }

            var written = await WriteNotificationsAsync(request, cancellationToken).ConfigureAwait(false);
            if (written.IsLeft)
            {
                await lease.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return (EncinaError)written;
            }

            // The commit is cleanup: it runs to completion even when the caller cancels.
            var committed = await lease.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            if (committed.IsLeft)
            {
                return (EncinaError)committed;
            }

            return result;
        }
        catch (Exception)
        {
            // Cancellation, a throwing handler or a serializer failure: nothing was committed, and the
            // exception reaches the caller unchanged.
            await lease.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async ValueTask<Either<EncinaError, int>> WriteNotificationsAsync(
        IHasNotifications request,
        CancellationToken cancellationToken)
    {
        var notifications = request.GetNotifications().ToList();
        if (notifications.Count == 0)
        {
            return 0;
        }

        using var activity = OutboxActivitySource.StartEnqueue(notifications.Count);
        var written = await _writer.WriteAsync(notifications, cancellationToken).ConfigureAwait(false);
        var dbSystem = DbSystemName.From(_transactionSource);

        if (written.IsLeft)
        {
            var error = (EncinaError)written;
            OutboxProcessorMetrics.Instance.RecordEnqueued(OutboxProcessorMetrics.EnqueueFailed, dbSystem, notifications.Count);
            MessagingLog.OutboxWriteFailed(_logger, RequestName, error.GetCode().IfNone("unknown"));
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
            return error;
        }

        OutboxProcessorMetrics.Instance.RecordEnqueued(OutboxProcessorMetrics.EnqueueWritten, dbSystem, notifications.Count);
        MessagingLog.OutboxNotificationsWritten(_logger, notifications.Count, RequestName);
        activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
        return written;
    }
}
