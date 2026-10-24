using Encina.DomainModeling;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.UnitOfWork;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Outbox;

/// <summary>
/// Requests, notification and handlers of the Entity Framework Core outbox atomicity scenarios (#718). They live
/// in a non-generic container because assembly scanning of this test assembly (other integration tests register
/// Encina that way) cannot register handlers nested in a generic class.
/// </summary>
public static class OutboxAtomicityEF
{
    /// <summary>What the handler does with the business change.</summary>
    public enum Mode
    {
        /// <summary>Saves the business row and returns a response.</summary>
        Commit,

        /// <summary>Saves the business row and returns a business Left.</summary>
        ReturnLeft,

        /// <summary>Saves the business row and throws.</summary>
        Throw,

        /// <summary>Saves the business row inside a unit of work that is rolled back, then returns a response.</summary>
        UnitOfWorkRollback,

        /// <summary>Saves the business row, then rolls the ambient context transaction back itself.</summary>
        RollBackAmbientTransaction
    }

    /// <summary>A notification the handler emits.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Sequence">The position of the notification.</param>
    public sealed record OrderPlaced(Guid OrderId, int Sequence) : INotification;

    /// <summary>A request without the transaction pattern.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Notifications">How many notifications it emits.</param>
    /// <param name="Mode">What the handler does.</param>
    public sealed record PlaceOrder(Guid OrderId, int Notifications, Mode Mode) : IRequest<int>, IHasNotifications
    {
        /// <inheritdoc />
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new OrderPlaced(OrderId, i));
    }

    /// <summary>A request that opts into the transaction behavior.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Notifications">How many notifications it emits.</param>
    /// <param name="Mode">What the handler does.</param>
    [Transaction]
    public sealed record PlaceTransactionalOrder(Guid OrderId, int Notifications, Mode Mode) : IRequest<int>, IHasNotifications
    {
        /// <inheritdoc />
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new OrderPlaced(OrderId, i));
    }

    /// <summary>A transactional request deduplicated by the inbox.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Notifications">How many notifications it emits.</param>
    /// <param name="Mode">What the handler does.</param>
    [Transaction]
    public sealed record PlaceIdempotentOrder(Guid OrderId, int Notifications, Mode Mode)
        : IRequest<int>, IHasNotifications, IIdempotentRequest
    {
        /// <inheritdoc />
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new OrderPlaced(OrderId, i));
    }

    /// <summary>Handler of <see cref="PlaceOrder"/>.</summary>
    public sealed class PlaceOrderHandler(DbContext context, IDbTransactionAccessor accessor, IServiceProvider services)
        : IRequestHandler<PlaceOrder, int>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, int>> Handle(PlaceOrder request, CancellationToken cancellationToken) =>
            Run(context, accessor, services, request.OrderId, request.Mode, cancellationToken);
    }

    /// <summary>Handler of <see cref="PlaceTransactionalOrder"/>.</summary>
    public sealed class PlaceTransactionalOrderHandler(DbContext context, IDbTransactionAccessor accessor, IServiceProvider services)
        : IRequestHandler<PlaceTransactionalOrder, int>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, int>> Handle(PlaceTransactionalOrder request, CancellationToken cancellationToken) =>
            Run(context, accessor, services, request.OrderId, request.Mode, cancellationToken);
    }

    /// <summary>Handler of <see cref="PlaceIdempotentOrder"/>.</summary>
    public sealed class PlaceIdempotentOrderHandler(DbContext context, IDbTransactionAccessor accessor, IServiceProvider services)
        : IRequestHandler<PlaceIdempotentOrder, int>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, int>> Handle(PlaceIdempotentOrder request, CancellationToken cancellationToken) =>
            Run(context, accessor, services, request.OrderId, request.Mode, cancellationToken);
    }

    /// <summary>Switches the application's own transaction on and records what happened to it.</summary>
    public sealed class ApplicationTransactionState
    {
        /// <summary>Gets or sets whether the transaction was still current when the request came back.</summary>
        public bool StillCurrentAfterRequest { get; set; }
    }

    /// <summary>
    /// Application code that begins its own transaction on the request scope's context (Encina dispatches every
    /// request in its own scope, so application code can only share a scope with Encina from inside the pipeline)
    /// and commits it when the request succeeded and the transaction is still current. It does nothing unless an
    /// <see cref="ApplicationTransactionState"/> is registered. Nested, so assembly scanning never registers it.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public sealed class ApplicationTransactionBehavior<TRequest, TResponse>(DbContext dbContext, IServiceProvider services)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <inheritdoc />
        public async ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
        {
            var state = services.GetService<ApplicationTransactionState>();
            if (state is null)
            {
                return await nextStep();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await nextStep();
            state.StillCurrentAfterRequest = dbContext.Database.CurrentTransaction is not null;
            if (state.StillCurrentAfterRequest)
            {
                if (result.IsRight)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
            }

            return result;
        }
    }

    private static async Task<Either<EncinaError, int>> Run(
        DbContext context,
        IDbTransactionAccessor accessor,
        IServiceProvider services,
        Guid orderId,
        Mode mode,
        CancellationToken cancellationToken)
    {
        if (mode == Mode.UnitOfWorkRollback)
        {
            // Built by hand and disposed asynchronously: the unit of work implements only IAsyncDisposable.
            await using var unitOfWork = new UnitOfWorkEF(context, accessor, services);
            (await unitOfWork.BeginTransactionAsync(cancellationToken)).ShouldBeRight();
            await SaveBusinessRowAsync(context, orderId, cancellationToken);
            await unitOfWork.RollbackAsync(cancellationToken);
            return 1;
        }

        await SaveBusinessRowAsync(context, orderId, cancellationToken);

        if (mode == Mode.RollBackAmbientTransaction)
        {
            await context.Database.CurrentTransaction!.RollbackAsync(cancellationToken);
        }

        return mode switch
        {
            Mode.Throw => throw new InvalidOperationException("boom"),
            Mode.ReturnLeft => Left<EncinaError, int>(EncinaErrors.Create("biz.rule", "business rule")),
            _ => Right<EncinaError, int>(1)
        };
    }

    private static async Task SaveBusinessRowAsync(DbContext context, Guid orderId, CancellationToken cancellationToken)
    {
        context.Set<TestRepositoryEntity>().Add(new TestRepositoryEntity
        {
            Id = orderId,
            Name = "order-" + orderId,
            Amount = 1m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
