using System.Data;

namespace Encina.EntityFrameworkCore;

/// <summary>
/// Attribute to mark a command as transactional with optional isolation level configuration.
/// </summary>
/// <remarks>
/// <para>
/// This attribute provides a declarative way to specify transaction behavior for commands.
/// It can be used as an alternative to implementing <see cref="ITransactionalCommand"/>.
/// </para>
/// <para>
/// <b>Isolation Levels</b>: The isolation level determines how transaction locks are handled.
/// If not specified, the database's default isolation level is used.
/// </para>
/// <para>
/// Common isolation levels:
/// <list type="bullet">
/// <item><description><b>ReadUncommitted</b>: Lowest isolation, highest concurrency, dirty reads possible</description></item>
/// <item><description><b>ReadCommitted</b>: Default for most databases, prevents dirty reads</description></item>
/// <item><description><b>RepeatableRead</b>: Prevents non-repeatable reads</description></item>
/// <item><description><b>Serializable</b>: Highest isolation, lowest concurrency, prevents phantom reads</description></item>
/// <item><description><b>Snapshot</b>: Uses row versioning (SQL Server, PostgreSQL)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default isolation level
/// [Transaction]
/// public record CreateOrderCommand(int CustomerId, decimal Amount) : ICommand&lt;Order&gt;;
///
/// // Explicit isolation level
/// [Transaction(IsolationLevel = IsolationLevel.Serializable)]
/// public record UpdateInventoryCommand(int ProductId, int Quantity) : ICommand;
///
/// // Read uncommitted for reports (non-critical data)
/// [Transaction(IsolationLevel = IsolationLevel.ReadUncommitted)]
/// public record GenerateReportQuery(DateTime StartDate, DateTime EndDate) : IQuery&lt;Report&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class TransactionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the transaction isolation level.
    /// </summary>
    /// <value>
    /// The isolation level for the transaction. If <c>null</c>, the database's default isolation level is used.
    /// </value>
    public IsolationLevel? IsolationLevel { get; init; }
}
