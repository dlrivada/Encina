using Encina.Messaging.Transactions;
using MongoDB.Driver;

namespace Encina.MongoDB.Transactions;

/// <summary>
/// The scope's MongoDB transaction: owns the client session and its transaction, and lets every participant of
/// the scope (unit of work, transactional outbox, messaging stores) share them.
/// </summary>
/// <remarks>
/// <para>
/// It is the MongoDB form of <see cref="ITransactionLeaseSource"/>: the first <c>BeginOrJoinAsync</c> owns the
/// transaction, later calls join it. A handler that writes with its own collection handles takes
/// <see cref="CurrentSession"/> and passes it to the driver so its writes commit or abort with the request.
/// </para>
/// <para>
/// MongoDB transactions need a replica set or a sharded cluster; on a standalone server
/// <c>BeginOrJoinAsync</c> returns <c>transaction.not_supported</c>.
/// </para>
/// </remarks>
public interface IMongoSessionAccessor : ITransactionLeaseSource
{
    /// <summary>
    /// Gets the client session of the scope's active transaction, or <see langword="null"/> when no transaction
    /// is active.
    /// </summary>
    IClientSessionHandle? CurrentSession { get; }
}
