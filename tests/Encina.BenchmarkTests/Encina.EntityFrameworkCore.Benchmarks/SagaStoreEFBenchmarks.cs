using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Sagas;

namespace Encina.EntityFrameworkCore.Benchmarks;

/// <summary>
/// Benchmarks for EF Core-based Saga implementation.
/// </summary>
/// <remarks>
/// Measures performance of core saga operations:
/// - Retrieving saga state by ID
/// - Adding new saga instances
/// - Updating saga state (most common operation)
/// - Retrieving stuck/timed-out sagas for recovery
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class SagaStoreEFBenchmarks
#pragma warning restore CA1001
{
    private BenchmarkDbContext _context = null!;
    private SagaStoreEF _store = null!;
    private Guid _testSagaId;

    /// <summary>
    /// Global setup: Create in-memory database and store instance.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseInMemoryDatabase(databaseName: "SagaBenchmarks")
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _store = new SagaStoreEF(_context);
        _testSagaId = Guid.NewGuid();
    }

    /// <summary>
    /// Global cleanup: Dispose database context.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Iteration setup: Clean saga table before each benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        // Clear all sagas for consistent benchmarks
        _context.SagaStates.RemoveRange(_context.SagaStates);
        _context.SaveChanges();
    }

    /// <summary>
    /// Baseline benchmark: Retrieve a saga by ID.
    /// </summary>
    [Benchmark(Baseline = true, Description = "GetAsync")]
    public async Task GetSaga()
    {
        // Setup: Add a saga
        await _store.AddAsync(new SagaState
        {
            SagaId = _testSagaId,
            SagaType = "BenchmarkSaga",
            Data = "{}",
            CurrentStep = 0,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.GetAsync(_testSagaId);
    }

    /// <summary>
    /// Benchmark: Add a new saga instance.
    /// </summary>
    [Benchmark(Description = "AddAsync")]
    public async Task AddSaga()
    {
        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderProcessingSaga",
            Data = "{\"orderId\":\"123\",\"amount\":99.99}",
            CurrentStep = 0,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await _store.AddAsync(saga);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmark: Update saga state (most common operation during saga execution).
    /// </summary>
    [Benchmark(Description = "UpdateAsync")]
    public async Task UpdateSaga()
    {
        // Setup
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "PaymentSaga",
            Data = "{\"step\":1}",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
        await _store.AddAsync(saga);
        await _context.SaveChangesAsync();

        // Benchmark: Update to next step
        saga.CurrentStep = 2;
        saga.Data = "{\"step\":2,\"payment\":\"completed\"}";
        saga.LastUpdatedAtUtc = DateTime.UtcNow;

        await _store.UpdateAsync(saga);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmark: Retrieve stuck/timed-out sagas for recovery.
    /// </summary>
    [Benchmark(Description = "GetStuckSagasAsync")]
    public async Task GetStuckSagas()
    {
        // Setup: Add 100 sagas (20 stuck, 80 healthy)
        var now = DateTime.UtcNow;
        for (int i = 0; i < 100; i++)
        {
            var isStuck = i % 5 == 0; // Every 5th saga is stuck
            await _store.AddAsync(new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"RecoverySaga{i}",
                Data = "{}",
                CurrentStep = isStuck ? 1 : 0,
                Status = SagaStatus.Running,
                StartedAtUtc = isStuck ? now.AddHours(-2) : now.AddMinutes(-5),
                LastUpdatedAtUtc = isStuck ? now.AddHours(-1) : now.AddMinutes(-1),
                TimeoutAtUtc = isStuck ? now.AddMinutes(-30) : now.AddHours(1)
            });
        }
        await _context.SaveChangesAsync();

        // Benchmark: Find sagas that have timed out
        await _store.GetStuckSagasAsync(TimeSpan.FromMinutes(30), 50);
    }
}
