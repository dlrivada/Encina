using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests for <see cref="AuditEntryProjection"/> and <see cref="ReadAuditEntryProjection"/>.
/// Validates constructor and method null-argument guards.
/// </summary>
/// <remarks>
/// The <c>Create(IDocumentOperations, ...)</c> path cannot be exercised from guard tests because
/// Marten's <c>IDocumentOperations</c> cannot be faked with a simple stub — the LINQ query
/// pipeline requires a real Marten document session. End-to-end coverage is provided by the
/// integration test suite, which boots a real PostgreSQL-backed Marten store.
/// </remarks>
public class AuditEntryProjectionGuardTests
{
    [Fact]
    public void AuditEntryProjection_Constructor_Parameterless_DoesNotThrow()
    {
        var projection = new AuditEntryProjection();
        projection.ShouldNotBeNull();
        projection.Name.ShouldBe("AuditEntryProjection");
    }

    [Fact]
    public void AuditEntryProjection_Constructor_NullPlaceholder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuditEntryProjection(null!, NullLogger<AuditEntryProjection>.Instance));
    }

    [Fact]
    public void AuditEntryProjection_Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuditEntryProjection("[SHREDDED]", null!));
    }

    [Fact]
    public async Task AuditEntryProjection_Create_NullEvent_Throws()
    {
        var projection = new AuditEntryProjection();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await projection.Create(null!, operations: null!, CancellationToken.None));
    }

    [Fact]
    public async Task AuditEntryProjection_Create_NullOperations_Throws()
    {
        var projection = new AuditEntryProjection();
        var evt = new AuditEntryRecordedEvent
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
