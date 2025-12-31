using Shouldly;
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using Xunit;
using SagaStatus = Encina.EntityFrameworkCore.Sagas.SagaStatus;

namespace Encina.EntityFrameworkCore.ContractTests.Sagas;

/// <summary>
/// Contract tests for SagaStoreEF verifying ISagaStore compliance.
/// </summary>
public sealed class SagaStoreEFContractTests : IDisposable
{
    private readonly DbContext _context;
    private readonly SagaStoreEF _store;

    public SagaStoreEFContractTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _store = new SagaStoreEF(_context);
    }

    [Fact]
    public void Contract_MustImplementISagaStore()
    {
        // Assert
        _store.ShouldBeAssignableTo<ISagaStore>();
    }

    [Fact]
    public async Task Contract_AddAsync_MustAcceptISagaState()
    {
        // Arrange
        ISagaState saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        var act = async () => await _store.AddAsync(saga);

        // Assert
        await Should.NotThrowAsync(async () => await act());
    }

    [Fact]
    public async Task Contract_GetAsync_MustReturnISagaState()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "Test",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await _store.AddAsync(saga);
        await _store.SaveChangesAsync();

        // Act
        var retrieved = await _store.GetAsync(sagaId);

        // Assert
        retrieved.ShouldBeAssignableTo<ISagaState>();
    }

    [Fact]
    public async Task Contract_UpdateAsync_MustAcceptISagaState()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "Test",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await _store.AddAsync(saga);
        await _store.SaveChangesAsync();

        // Modify the saga
        saga.CurrentStep = 1;
        saga.LastUpdatedAtUtc = DateTime.UtcNow;

        // Act
        var act = async () => await _store.UpdateAsync(saga);

        // Assert
        await Should.NotThrowAsync(async () => await act());
    }

    [Fact]
    public async Task Contract_GetStuckSagasAsync_MustReturnISagaState()
    {
        // Arrange
        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Test",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        await _store.AddAsync(saga);
        await _store.SaveChangesAsync();

        // Act
        var sagas = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        sagas.ShouldAllBe(x => x is ISagaState);
    }

    [Fact]
    public async Task Contract_SaveChangesAsync_MustPersistChanges()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "Test",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await _store.AddAsync(saga);

        // Act
        await _store.SaveChangesAsync();

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        retrieved.ShouldNotBeNull();
    }

    [Fact]
    public async Task Contract_AddAsync_WithNonEFSagaState_MustThrow()
    {
        // Arrange
        var mockSaga = new MockSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Test",
            Data = "{}",
            Status = "Running",
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        var act = async () => await _store.AddAsync(mockSaga);

        // Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await act());
        ex.Message.ShouldMatch("*SagaState*");
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // Mock implementation for contract testing
    private sealed class MockSagaState : ISagaState
    {
        public Guid SagaId { get; set; }
        public required string SagaType { get; set; }
        public required string Data { get; set; }
        public required string Status { get; set; }
        public int CurrentStep { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime LastUpdatedAtUtc { get; set; }
        public DateTime? TimeoutAtUtc { get; set; }
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }
        public DbSet<SagaState> SagaStates => Set<SagaState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SagaState>().HasKey(s => s.SagaId);
        }
    }
}
