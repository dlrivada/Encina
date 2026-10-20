using BenchmarkDotNet.Attributes;
using Encina.ADO.SqlServer.Outbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ADO.SqlServer.Benchmarks.Benchmarks;

/// <summary>
/// Measures what <see cref="TransactionalOutboxPipelineBehavior{TRequest, TResponse}"/> costs a request that cannot
/// carry notifications (does not implement <see cref="IHasNotifications"/>): the skip path must add no transaction,
/// no database call and no allocation compared with calling the next step directly (#718, pay for what you use).
/// No database is needed: the skip path never touches the connection.
/// </summary>
[MemoryDiagnoser]
public class TransactionalOutboxSkipPathBenchmarks
{
    private sealed record PlainRequest : IRequest<int>;

    private SqlConnection _connection = null!;
    private TransactionalOutboxPipelineBehavior<PlainRequest, int> _behavior = null!;
    private PlainRequest _request = null!;
    private IRequestContext _context = null!;
    private RequestHandlerCallback<int> _next = null!;

    /// <summary>
    /// Builds the behavior over the real accessor and store with a connection that is never opened.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = new SqlConnection();
        _behavior = new TransactionalOutboxPipelineBehavior<PlainRequest, int>(
            new DbTransactionAccessor(_connection, NullLogger<DbTransactionAccessor>.Instance),
            new OutboxStoreADO(_connection),
            new OutboxMessageFactory(),
            new JsonMessageSerializer(),
            NullLogger<TransactionalOutboxPipelineBehavior<PlainRequest, int>>.Instance);
        _request = new PlainRequest();
        _context = RequestContext.CreateForTest();
        var response = ValueTask.FromResult<Either<EncinaError, int>>(1);
        _next = () => response;
    }

    /// <summary>
    /// Drops the unopened connection.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup() => _connection.Dispose();

    /// <summary>
    /// The next step called directly: the baseline.
    /// </summary>
    /// <returns>The response.</returns>
    [Benchmark(Baseline = true)]
    public ValueTask<Either<EncinaError, int>> WithoutBehavior() => _next();

    /// <summary>
    /// The same step through the transactional outbox behavior on the skip path.
    /// </summary>
    /// <returns>The response.</returns>
    [Benchmark]
    public ValueTask<Either<EncinaError, int>> WithTransactionalOutboxBehavior()
        => _behavior.Handle(_request, _context, _next, CancellationToken.None);
}
