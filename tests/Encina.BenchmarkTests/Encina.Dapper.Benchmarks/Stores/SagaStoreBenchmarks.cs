using BenchmarkDotNet.Attributes;
using Encina.Dapper.Benchmarks.Infrastructure;
using Encina.Dapper.Sqlite.Sagas;
using Encina.Messaging.Sagas;
using Microsoft.Data.Sqlite;

namespace Encina.Dapper.Benchmarks.Stores;

/// <summary>
/// Benchmarks for Dapper SagaStore operations.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class SagaStoreBenchmarks
{
    private SqliteConnection _connection = null!;
    private SagaStoreDapper _store = null!;
    private List<BenchmarkSagaState> _sagas = null!;
    private Guid _existingSagaId;
    private BenchmarkSagaState _sagaToUpdate = null!;
    private int _newSagaCounter;

    /// <summary>
    /// Payload size in bytes for saga Data to measure JSON serialization impact.
    /// </summary>
    [Params(100, 1000, 10000)]
    public int DataPayloadSize { get; set; }

    /// <summary>
    /// Batch size for batch operations.
    /// </summary>
    [Params(10, 100)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Global setup - creates database and populates with test data.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = DapperConnectionFactory.CreateSharedMemorySqliteConnection("saga_dapper_benchmark");
        ProviderTypeHandlers.EnsureRegistered(DatabaseProvider.Sqlite);
        DapperSchemaBuilder.CreateSagaTable(_connection, DatabaseProvider.Sqlite);

        _sagas = new List<BenchmarkSagaState>();

        // Running sagas (will be queried)
        for (int i = 0; i < 500; i++)
        {
            var saga = CreateSagaWithPayload("Running");
            _sagas.Add(saga);
            InsertSaga(saga);
        }

        // Stuck sagas (old LastUpdatedAtUtc)
        for (int i = 0; i < 200; i++)
        {
            var saga = CreateSagaWithPayload("Running");
            saga.LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2);
            InsertSaga(saga);
        }

        // Expired sagas (past TimeoutAtUtc)
        for (int i = 0; i < 200; i++)
        {
            var saga = CreateSagaWithPayload("Running");
            saga.TimeoutAtUtc = DateTime.UtcNow.AddMinutes(-10);
            InsertSaga(saga);
        }

        // Completed sagas
        for (int i = 0; i < 100; i++)
        {
            var saga = CreateSagaWithPayload("Completed");
            saga.CompletedAtUtc = DateTime.UtcNow.AddMinutes(-30);
            InsertSaga(saga);
        }

        _existingSagaId = _sagas[0].SagaId;
        _sagaToUpdate = _sagas[1];
        _newSagaCounter = 0;

        _store = new SagaStoreDapper(_connection);
    }

    /// <summary>
    /// Iteration setup - resets state for consistent benchmarks.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        _newSagaCounter++;
    }

    /// <summary>
    /// Global cleanup - disposes resources.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection.Dispose();
    }

    /// <summary>
    /// Benchmarks retrieving a saga by ID.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<ISagaState?> GetAsync()
    {
        return await _store.GetAsync(_existingSagaId);
    }

    /// <summary>
    /// Benchmarks adding a new saga with JSON serialization.
    /// </summary>
    [Benchmark]
    public async Task AddAsync()
    {
        var saga = CreateSagaWithPayload("Running");
        saga.SagaId = Guid.NewGuid();
        await _store.AddAsync(saga);
    }

    /// <summary>
    /// Benchmarks updating a saga state with JSON re-serialization.
    /// </summary>
    [Benchmark]
    public async Task UpdateAsync()
    {
        _sagaToUpdate.CurrentStep++;
        _sagaToUpdate.Data = GeneratePayload(DataPayloadSize);
        await _store.UpdateAsync(_sagaToUpdate);
    }

    /// <summary>
    /// Benchmarks querying for stuck sagas.
    /// </summary>
    [Benchmark]
    public async Task<List<ISagaState>> GetStuckSagasAsync()
    {
        var sagas = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), BatchSize);
        return sagas.ToList();
    }

    /// <summary>
    /// Benchmarks querying for expired sagas.
    /// </summary>
    [Benchmark]
    public async Task<List<ISagaState>> GetExpiredSagasAsync()
    {
        var sagas = await _store.GetExpiredSagasAsync(BatchSize);
        return sagas.ToList();
    }

    private BenchmarkSagaState CreateSagaWithPayload(string status)
    {
        var saga = BenchmarkEntityFactory.CreateSagaState();
        saga.Status = status;
        saga.Data = GeneratePayload(DataPayloadSize);
        return saga;
    }

    private static string GeneratePayload(int size)
    {
        // Generate JSON-like payload of approximately the requested size
        var padding = new string('x', Math.Max(0, size - 50));
        return $$"""{"orderId":"12345","step":"Processing","data":"{{padding}}"}""";
    }

    private void InsertSaga(BenchmarkSagaState saga)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO SagaStates
            (SagaId, SagaType, Data, Status, CurrentStep, StartedAtUtc, CompletedAtUtc, ErrorMessage, LastUpdatedAtUtc, TimeoutAtUtc)
            VALUES
            (@SagaId, @SagaType, @Data, @Status, @CurrentStep, @StartedAtUtc, @CompletedAtUtc, @ErrorMessage, @LastUpdatedAtUtc, @TimeoutAtUtc)";

        command.Parameters.AddWithValue("@SagaId", saga.SagaId.ToString());
        command.Parameters.AddWithValue("@SagaType", saga.SagaType);
        command.Parameters.AddWithValue("@Data", saga.Data);
        command.Parameters.AddWithValue("@Status", saga.Status);
        command.Parameters.AddWithValue("@CurrentStep", saga.CurrentStep);
        command.Parameters.AddWithValue("@StartedAtUtc", saga.StartedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@CompletedAtUtc", saga.CompletedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", saga.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LastUpdatedAtUtc", saga.LastUpdatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@TimeoutAtUtc", saga.TimeoutAtUtc?.ToString("O") ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }
}
