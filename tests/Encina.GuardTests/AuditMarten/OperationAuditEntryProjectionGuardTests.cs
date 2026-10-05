using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests for <see cref="OperationAuditEntryProjection"/> and <see cref="ReadAuditEntryProjection"/>.
/// Validates constructor and method null-argument guards.
/// </summary>
/// <remarks>
/// The <c>Create(IDocumentOperations, ...)</c> path cannot be exercised from guard tests because
/// Marten's <c>IDocumentOperations</c> cannot be faked with a simple stub — the LINQ query
/// pipeline requires a real Marten document session. End-to-end coverage is provided by the
/// integration test suite, which boots a real PostgreSQL-backed Marten store.
/// </remarks>
public class OperationAuditEntryProjectionGuardTests
{
    [Fact]
    public void OperationAuditEntryProjection_Constructor_Parameterless_DoesNotThrow()
    {
        var projection = new OperationAuditEntryProjection();
        projection.ShouldNotBeNull();
        projection.Name.ShouldBe("OperationAuditEntryProjection");
    }

    [Fact]
    public void OperationAuditEntryProjection_Constructor_NullPlaceholder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new OperationAuditEntryProjection(null!, NullLogger<OperationAuditEntryProjection>.Instance));
    }

    [Fact]
    public void OperationAuditEntryProjection_Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new OperationAuditEntryProjection("[SHREDDED]", null!));
    }

    [Fact]
    public async Task OperationAuditEntryProjection_Create_NullEvent_Throws()
    {
        var projection = new OperationAuditEntryProjection();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await projection.Create(null!, operations: null!, CancellationToken.None));
    }

    [Fact]
    public async Task OperationAuditEntryProjection_Create_NullOperations_Throws()
    {
        var projection = new OperationAuditEntryProjection();
        var evt = new OperationAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "c",
            Action = "A",
            EntityType = "E",
            Outcome = 0,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            TemporalKeyPeriod = "2026-03"
        };

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await projection.Create(evt, operations: null!, CancellationToken.None));
    }

    [Fact]
    public void ReadAuditEntryProjection_Constructor_Parameterless_DoesNotThrow()
    {
        var projection = new ReadAuditEntryProjection();
        projection.ShouldNotBeNull();
        projection.Name.ShouldBe("ReadAuditEntryProjection");
    }

    [Fact]
    public void ReadAuditEntryProjection_Constructor_NullPlaceholder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReadAuditEntryProjection(null!, NullLogger<ReadAuditEntryProjection>.Instance));
    }

    [Fact]
    public void ReadAuditEntryProjection_Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReadAuditEntryProjection("[SHREDDED]", null!));
    }

    [Fact]
    public async Task ReadAuditEntryProjection_Create_NullEvent_Throws()
    {
        var projection = new ReadAuditEntryProjection();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await projection.Create(null!, operations: null!, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAuditEntryProjection_Create_NullOperations_Throws()
    {
        var projection = new ReadAuditEntryProjection();
        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "E",
            EntityId = null,
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = 0,
            EntityCount = 0,
            TemporalKeyPeriod = "2026-03"
        };

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await projection.Create(evt, operations: null!, CancellationToken.None));
    }
}
