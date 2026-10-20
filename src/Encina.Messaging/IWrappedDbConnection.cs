using System.Data;

namespace Encina.Messaging;

/// <summary>
/// Implemented by a connection that decorates another one (for example the module-isolation
/// <c>SchemaValidatingConnection</c>), so that <see cref="DbLease"/> can find the connection that really owns
/// the business transaction and can clone it for writes that must survive the transaction's rollback.
/// </summary>
/// <remarks>
/// A decorator typically hands out the inner connection's transaction, so <c>transaction.Connection</c> is the
/// inner connection, not the decorator the stores hold.
/// </remarks>
public interface IWrappedDbConnection
{
    /// <summary>
    /// Gets the connection this one decorates.
    /// </summary>
    IDbConnection InnerConnection { get; }
}
