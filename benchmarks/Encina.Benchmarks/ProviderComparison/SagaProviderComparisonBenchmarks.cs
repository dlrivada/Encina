using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Encina.Benchmarks.Infrastructure;
using Encina.Messaging.Sagas;
using DapperSagas = Encina.Dapper.Sqlite.Sagas;
using EFSagas = Encina.EntityFrameworkCore.Sagas;

namespace Encina.Benchmarks.ProviderComparison;

/// <summary>
/// Benchmarks comparing Saga performance across different data access providers.
/// Tests EF Core and Dapper implementations to answer:
/// - Which provider is fastest for saga state persistence?
/// - Which provider is fastest for saga state transitions?
/// - Which provider is fastest for saga completion/compensation?
/// - What's the memory allocation difference?
/// </summary>
/// <remarks>
/// <para>
/// <b>Note</b>: ADO.NET providers do NOT support Sagas.
/// Only Dapper and EF Core providers implement the Saga pattern.
/// </para>
/// <para>
/// Saga patterns are used for distributed transaction orchestration.
/// Typical operations:
/// <list type="bullet">
/// <item><description>Create saga instance (AddAsync)</description></item>
/// <item><description>Progress through steps (UpdateAsync)</description></item>
/// <item><description>Complete successfully (MarkAsCompletedAsync)</description></item>
/// <item><description>Compensate on failure (MarkAsFailedAsync)</description></item>
/// </list>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net90)]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class SagaProviderComparisonBenchmarks
#pragma warning restore CA1001
{
    /// <summary>
    /// The data access provider to benchmark.
    /// Only Dapper and EFCore support Sagas (ADO.NET does NOT).
    /// </summary>
    [Params("Dapper", "EFCore")]
    public string Provider { get; set; } = "Dapper";

    private SqliteConnection _connection = null!;
    private BenchmarkDbContext? _context;
    private ISagaStore _store = null!;

    /// <summary>
    /// Sets up the database connection and store based on the selected provider.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Register Dapper type handlers
        DapperTypeHandlers.Register();

        // Create in-memory SQLite connection
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create store based on provider parameter
        _store = Provider switch
        {
            "Dapper" => await SetupDapperStore(),
            "EFCore" => await SetupEfCoreStore(),
            _ => throw new InvalidOperationException($"Unknown provider: {Provider}")
        };
    }

    /// <summary>
    /// Cleans up resources after benchmarks complete.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _context?.Dispose();
        _connection?.Dispose();
    }

    /// <summary>
    /// Cleans the table before each iteration to ensure consistent results.
    /// </summary>
    [IterationSetup]
    public async Task IterationSetup()
    {
        if (Provider == "EFCore")
        {
            await _context!.Database.ExecuteSqlRawAsync("DELETE FROM SagaStates");
        }
        else
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM SagaStates";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Benchmarks adding a single saga to the store.
    /// Tests the overhead of creating a new saga instance.
    /// </summary>
    [Benchmark(Baseline = true, Description = "AddAsync single saga")]
    public async Task AddAsync_Single()
    {
        var saga = CreateSagaState(
            sagaType: "OrderSaga",
            status: "Running",
            currentStep: 0,
            data: "{\"orderId\":123,\"customerId\":456}");

        await _store.AddAsync(saga);
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks retrieving a saga by its ID.
    /// Tests read performance for saga state lookup.
    /// </summary>
    [Benchmark(Description = "GetAsync by ID")]
    public async Task GetAsync_ById()
    {
        // Setup: Add a saga
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(
            sagaId: sagaId,
            sagaType: "OrderSaga",
            status: "Running",
            currentStep: 0,
            data: "{\"orderId\":123}");

        await _store.AddAsync(saga);
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.GetAsync(sagaId);
    }

    /// <summary>
    /// Benchmarks updating a saga's state (step transition).
    /// Tests update performance for saga progression through steps.
    /// </summary>
    [Benchmark(Description = "UpdateAsync - state transition")]
    public async Task UpdateAsync_StateTransition()
    {
        // Setup: Add a saga
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(
            sagaId: sagaId,
            sagaType: "OrderSaga",
            status: "Running",
            currentStep: 0,
            data: "{\"orderId\":123}");

        await _store.AddAsync(saga);
        await _store.SaveChangesAsync();

        // Retrieve and update
        var retrieved = await _store.GetAsync(sagaId);
        if (retrieved != null)
        {
            retrieved.CurrentStep = 1;
            retrieved.Data = "{\"orderId\":123,\"paymentId\":789}";
            retrieved.LastUpdatedAtUtc = DateTime.UtcNow;

            // Benchmark: Update state
            await _store.UpdateAsync(retrieved);
            await _store.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Benchmarks updating multiple saga steps in sequence.
    /// Tests update performance for multiple state transitions.
    /// </summary>
    [Benchmark(Description = "UpdateAsync - 5 step transitions")]
    public async Task UpdateAsync_FiveSteps()
    {
        // Setup: Add a saga
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(
            sagaId: sagaId,
            sagaType: "OrderSaga",
            status: "Running",
            currentStep: 0,
            data: "{\"orderId\":123}");

        await _store.AddAsync(saga);
        await _store.SaveChangesAsync();

        // Benchmark: Progress through 5 steps
        for (int step = 1; step <= 5; step++)
        {
            var retrieved = await _store.GetAsync(sagaId);
            if (retrieved != null)
            {
                retrieved.CurrentStep = step;
                retrieved.Data = $"{{\"orderId\":123,\"step\":{step}}}";
                retrieved.LastUpdatedAtUtc = DateTime.UtcNow;
                await _store.UpdateAsync(retrieved);
                await _store.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// Benchmarks querying stuck sagas that need intervention.
    /// Tests read performance with filtering and ordering.
    /// </summary>
    [Benchmark(Description = "GetStuckSagasAsync batch=10")]
    public async Task GetStuckSagas_Batch10()
    {
        // Setup: Add 50 sagas (30 stuck, 20 recent)
        var oldTime = DateTime.UtcNow.AddHours(-2);
        for (int i = 0; i < 50; i++)
        {
            var isStuck = i < 30;
            await _store.AddAsync(CreateSagaState(
                sagaType: $"OrderSaga{i}",
                status: "Running",
                currentStep: i % 5,
                data: "{}",
                lastUpdatedAtUtc: isStuck ? oldTime : DateTime.UtcNow));
        }
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);
    }

    /// <summary>
    /// Creates a saga state instance for benchmarking.
    /// </summary>
    private ISagaState CreateSagaState(
        Guid? sagaId = null,
        string sagaType = "TestSaga",
        string status = "Running",
        int currentStep = 0,
        string data = "{}",
        DateTime? lastUpdatedAtUtc = null)
    {
        var now = DateTime.UtcNow;

        if (Provider == "Dapper")
        {
            return new DapperSagas.SagaState
            {
                SagaId = sagaId ?? Guid.NewGuid(),
                SagaType = sagaType,
                Status = status,
                CurrentStep = currentStep,
                Data = data,
                StartedAtUtc = now,
                LastUpdatedAtUtc = lastUpdatedAtUtc ?? now
            };
        }
        else // EFCore
        {
            return new EFSagas.SagaState
            {
                SagaId = sagaId ?? Guid.NewGuid(),
                SagaType = sagaType,
                Status = Enum.Parse<EFSagas.SagaStatus>(status),
                CurrentStep = currentStep,
                Data = data,
                StartedAtUtc = now,
                LastUpdatedAtUtc = lastUpdatedAtUtc ?? now
            };
        }
    }

    private async Task<ISagaStore> SetupDapperStore()
    {
        await SqliteSchemaBuilder.CreateSagaSchemaAsync(_connection);
        return new DapperSagas.SagaStoreDapper(_connection);
    }

    private async Task<ISagaStore> SetupEfCoreStore()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        return new EFSagas.SagaStoreEF(_context);
    }
}
