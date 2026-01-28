using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.EntityFrameworkCore.Infrastructure;
using Encina.EntityFrameworkCore;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Benchmarks.EntityFrameworkCore;

/// <summary>
/// Benchmarks for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> measuring
/// the reflection-based transaction detection and pipeline overhead.
/// </summary>
/// <remarks>
/// <para>
/// <b>Performance Targets:</b>
/// <list type="bullet">
///   <item><description>Non-transactional passthrough: &lt;1μs</description></item>
///   <item><description>Transaction detection (interface): &lt;100ns</description></item>
///   <item><description>Transaction detection (attribute): &lt;1μs</description></item>
/// </list>
/// </para>
/// <para>
/// <b>CA1001 Suppression:</b> BenchmarkDotNet manages lifecycle via [GlobalSetup]/[GlobalCleanup].
/// Implementing IDisposable would interfere with BenchmarkDotNet's resource management.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class TransactionBehaviorBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private EntityFrameworkBenchmarkDbContext _dbContext = null!;

    // Behaviors for different request types
    private TransactionPipelineBehavior<TransactionalInterfaceCommand, Guid> _interfaceBehavior = null!;
    private TransactionPipelineBehavior<TransactionalAttributeCommand, Guid> _attributeBehavior = null!;
    private TransactionPipelineBehavior<NonTransactionalCommand, Guid> _nonTransactionalBehavior = null!;

    // Shared test data
    private static readonly TransactionalInterfaceCommand s_interfaceCommand = new("Test Interface");
    private static readonly TransactionalAttributeCommand s_attributeCommand = new("Test Attribute");
    private static readonly NonTransactionalCommand s_nonTransactionalCommand = new("Test Non-Transactional");
    private static readonly IRequestContext s_context = RequestContext.Create();

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Use SQLite in-memory for realistic transaction behavior
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = EntityFrameworkBenchmarkDbContext.CreateSqlite(_connection);
        _dbContext.Database.EnsureCreated();

        var nullLogger1 = NullLogger<TransactionPipelineBehavior<TransactionalInterfaceCommand, Guid>>.Instance;
        var nullLogger2 = NullLogger<TransactionPipelineBehavior<TransactionalAttributeCommand, Guid>>.Instance;
        var nullLogger3 = NullLogger<TransactionPipelineBehavior<NonTransactionalCommand, Guid>>.Instance;

        _interfaceBehavior = new TransactionPipelineBehavior<TransactionalInterfaceCommand, Guid>(_dbContext, nullLogger1);
        _attributeBehavior = new TransactionPipelineBehavior<TransactionalAttributeCommand, Guid>(_dbContext, nullLogger2);
        _nonTransactionalBehavior = new TransactionPipelineBehavior<NonTransactionalCommand, Guid>(_dbContext, nullLogger3);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }

    /// <summary>
    /// Baseline: Direct handler invocation without pipeline overhead.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Direct handler (baseline)")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public async Task<Guid> DirectHandler_Baseline()
    {
        return await SimulateHandler();
    }

    /// <summary>
    /// Non-transactional request passthrough (target: &lt;1μs).
    /// Tests the RequiresTransaction check overhead for plain requests.
    /// </summary>
    [Benchmark(Description = "Non-transactional passthrough")]
    public async Task<Either<EncinaError, Guid>> NonTransactional_Passthrough()
    {
        return await _nonTransactionalBehavior.Handle(
            s_nonTransactionalCommand,
            s_context,
            SimulateHandlerEither,
            CancellationToken.None);
    }

    /// <summary>
    /// Interface-based transaction detection (ITransactionalCommand).
    /// Tests reflection overhead for interface check.
    /// </summary>
    [Benchmark(Description = "Interface detection + transaction")]
    public async Task<Either<EncinaError, Guid>> InterfaceDetection_WithTransaction()
    {
        return await _interfaceBehavior.Handle(
            s_interfaceCommand,
            s_context,
            SimulateHandlerEither,
            CancellationToken.None);
    }

    /// <summary>
    /// Attribute-based transaction detection ([Transaction]).
    /// Tests reflection overhead for attribute check.
    /// </summary>
    [Benchmark(Description = "Attribute detection + transaction")]
    public async Task<Either<EncinaError, Guid>> AttributeDetection_WithTransaction()
    {
        return await _attributeBehavior.Handle(
            s_attributeCommand,
            s_context,
            SimulateHandlerEither,
            CancellationToken.None);
    }

    /// <summary>
    /// Transaction lifecycle only: BeginTransaction + Commit.
    /// Isolates the database transaction overhead from detection logic.
    /// </summary>
    [Benchmark(Description = "Transaction lifecycle (Begin + Commit)")]
    public async Task TransactionLifecycle_BeginAndCommit()
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Transaction lifecycle: BeginTransaction + Rollback.
    /// Measures rollback cost compared to commit.
    /// </summary>
    [Benchmark(Description = "Transaction lifecycle (Begin + Rollback)")]
    public async Task TransactionLifecycle_BeginAndRollback()
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Measures the pure RequiresTransaction reflection check without full pipeline.
    /// Uses the transactional interface command to test interface detection.
    /// </summary>
    [Benchmark(Description = "RequiresTransaction check (interface)")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public bool RequiresTransaction_InterfaceCheck()
    {
        return s_interfaceCommand is ITransactionalCommand;
    }

    /// <summary>
    /// Measures the pure RequiresTransaction reflection check for attribute detection.
    /// </summary>
    [Benchmark(Description = "RequiresTransaction check (attribute)")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public bool RequiresTransaction_AttributeCheck()
    {
        var attribute = typeof(TransactionalAttributeCommand)
            .GetCustomAttributes(typeof(TransactionAttribute), inherit: true)
            .FirstOrDefault();
        return attribute != null;
    }

    /// <summary>
    /// Measures the pure RequiresTransaction check for non-transactional requests.
    /// Tests both the interface check (fast path) and attribute reflection (slow path).
    /// </summary>
    [Benchmark(Description = "RequiresTransaction check (non-transactional)")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
    public bool RequiresTransaction_NonTransactionalCheck()
    {
        // Use object cast to avoid compile-time type analysis warning CS0184
        object command = s_nonTransactionalCommand;
        if (command is ITransactionalCommand)
            return true;

        var attribute = typeof(NonTransactionalCommand)
            .GetCustomAttributes(typeof(TransactionAttribute), inherit: true)
            .FirstOrDefault();
        return attribute != null;
    }

    // Simulated handler for baseline
    private static Task<Guid> SimulateHandler()
    {
        return Task.FromResult(Guid.NewGuid());
    }

    // Simulated handler returning Either for pipeline
    private static ValueTask<Either<EncinaError, Guid>> SimulateHandlerEither()
    {
        return ValueTask.FromResult<Either<EncinaError, Guid>>(Guid.NewGuid());
    }

    #region Test Request Types

    /// <summary>
    /// Command implementing ITransactionalCommand interface.
    /// </summary>
    public sealed record TransactionalInterfaceCommand(string Name) : IRequest<Guid>, ITransactionalCommand;

    /// <summary>
    /// Command decorated with [Transaction] attribute.
    /// </summary>
    [Transaction]
    public sealed record TransactionalAttributeCommand(string Name) : IRequest<Guid>;

    /// <summary>
    /// Plain command without transaction requirements.
    /// </summary>
    public sealed record NonTransactionalCommand(string Name) : IRequest<Guid>;

    #endregion
}
