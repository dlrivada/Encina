using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.MongoDB.Transactions;
using Encina.MongoDB.UnitOfWork;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Outbox;

/// <summary>
/// Requests, notification and handlers of the MongoDB outbox atomicity scenarios (#718). They live in a
/// non-generic container so assembly scanning of this test assembly can never trip on them.
/// </summary>
public static class OutboxAtomicityMongoDB
{
    /// <summary>The collection the handlers write their business document to.</summary>
    public const string BusinessCollection = "atomicity_orders";

    /// <summary>What the handler does with the business change.</summary>
    public enum Mode
    {
        /// <summary>Writes the business document and returns a response.</summary>
        Commit,

        /// <summary>Writes the business document and returns a business Left.</summary>
        ReturnLeft,

        /// <summary>Writes the business document and throws.</summary>
        Throw,

        /// <summary>Writes the business document inside a unit of work that is rolled back, then returns a response.</summary>
        UnitOfWorkRollback,

        /// <summary>Writes the business document through the default (non-session) collection, then returns a response.</summary>
        OutsideSession
    }

    /// <summary>A notification the handler emits.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Sequence">The position of the notification.</param>
    public sealed record OrderPlaced(Guid OrderId, int Sequence) : INotification;

    /// <summary>A request that carries notifications.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Notifications">How many notifications it emits.</param>
    /// <param name="Mode">What the handler does.</param>
    public sealed record PlaceOrder(Guid OrderId, int Notifications, Mode Mode) : IRequest<int>, IHasNotifications
    {
        /// <inheritdoc />
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new OrderPlaced(OrderId, i));
    }

    /// <summary>A request deduplicated by the inbox that also carries notifications.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Notifications">How many notifications it emits.</param>
    /// <param name="Mode">What the handler does.</param>
    public sealed record PlaceIdempotentOrder(Guid OrderId, int Notifications, Mode Mode)
        : IRequest<int>, IHasNotifications, IIdempotentRequest
    {
        /// <inheritdoc />
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new OrderPlaced(OrderId, i));
    }

    /// <summary>Handler of <see cref="PlaceOrder"/>.</summary>
    public sealed class PlaceOrderHandler(IMongoClient client, IMongoSessionAccessor accessor, IServiceProvider services)
        : IRequestHandler<PlaceOrder, int>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, int>> Handle(PlaceOrder request, CancellationToken cancellationToken) =>
            Run(client, accessor, services, request.OrderId, request.Mode, cancellationToken);
    }

    /// <summary>Handler of <see cref="PlaceIdempotentOrder"/>.</summary>
    public sealed class PlaceIdempotentOrderHandler(IMongoClient client, IMongoSessionAccessor accessor, IServiceProvider services)
        : IRequestHandler<PlaceIdempotentOrder, int>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, int>> Handle(PlaceIdempotentOrder request, CancellationToken cancellationToken) =>
            Run(client, accessor, services, request.OrderId, request.Mode, cancellationToken);
    }

    private static async Task<Either<EncinaError, int>> Run(
        IMongoClient client,
        IMongoSessionAccessor accessor,
        IServiceProvider services,
        Guid orderId,
        Mode mode,
        CancellationToken cancellationToken)
    {
        var collection = client.GetDatabase(MongoDbReplicaSetFixture.DatabaseName).GetCollection<BsonDocument>(BusinessCollection);
        var document = new BsonDocument { { "_id", new BsonBinaryData(orderId, GuidRepresentation.Standard) }, { "name", "order-" + orderId } };

        if (mode == Mode.UnitOfWorkRollback)
        {
            // Built by hand and disposed asynchronously: the unit of work implements only IAsyncDisposable (#2556).
            var options = Options.Create(new global::Encina.MongoDB.EncinaMongoDbOptions { DatabaseName = MongoDbReplicaSetFixture.DatabaseName });
            await using var unitOfWork = new UnitOfWorkMongoDB(client, options, accessor, services);
            var begun = await unitOfWork.BeginTransactionAsync(cancellationToken);
            if (begun.IsLeft)
            {
                return Left<EncinaError, int>(EncinaErrors.Create("test.begin_failed", "unit of work could not begin"));
            }

            await collection.InsertOneAsync(accessor.CurrentSession!, document, cancellationToken: cancellationToken);
            await unitOfWork.RollbackAsync(cancellationToken);
            return 1;
        }

        if (mode == Mode.OutsideSession)
        {
            await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        }
        else
        {
            await collection.InsertOneAsync(accessor.CurrentSession!, document, cancellationToken: cancellationToken);
        }

        return mode switch
        {
            Mode.Throw => throw new InvalidOperationException("boom"),
            Mode.ReturnLeft => Left<EncinaError, int>(EncinaErrors.Create("biz.rule", "business rule")),
            _ => Right<EncinaError, int>(1)
        };
    }

    /// <summary>Gives every outbox message the same id, so the second one violates the key.</summary>
    public sealed class FixedIdOutboxMessageFactory(IOutboxMessageFactory inner, Guid fixedId) : IOutboxMessageFactory
    {
        /// <inheritdoc />
        public IOutboxMessage Create(Guid id, string notificationType, string content, DateTime createdAtUtc) =>
            inner.Create(fixedId, notificationType, content, createdAtUtc);
    }
}
