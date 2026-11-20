using Encina.OpenTelemetry.Audit;
using Encina.Security.Audit;
using LanguageExt;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Valid-argument guard tests for <see cref="InstrumentedOperationAuditStore"/>: a nullable
/// <c>entityId</c> is accepted and the decorator forwards each call to the inner store.
/// </summary>
public sealed class InstrumentedOperationAuditStoreDelegationGuardTests
{
    private sealed class FakeStore(bool throwOnPurge = false) : IOperationAuditStore
    {
        public string? LastEntityId { get; private set; } = "unset";

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(OperationAuditEntry entry, CancellationToken cancellationToken = default) =>
            new(Prelude.Right<EncinaError, Unit>(Unit.Default));

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByEntityAsync(
            string entityType, string? entityId, CancellationToken cancellationToken = default)
        {
            LastEntityId = entityId;
            return new(Prelude.Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([]));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByUserAsync(
            string userId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
            new(Prelude.Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([]));

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByCorrelationIdAsync(
            string correlationId, CancellationToken cancellationToken = default) =>
            new(Prelude.Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([]));

        public ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryAsync(
            OperationAuditQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default) =>
            throwOnPurge
                ? throw new InvalidOperationException("boom")
                : new(Prelude.Right<EncinaError, int>(3));
    }

    [Fact]
    public async Task GetByEntityAsync_NullEntityId_IsAcceptedAndForwarded()
    {
        var inner = new FakeStore();
        var sut = new InstrumentedOperationAuditStore(inner);

        var result = await sut.GetByEntityAsync("Order", null);

        result.IsRight.ShouldBeTrue();
        inner.LastEntityId.ShouldBeNull();
    }

    [Fact]
    public async Task PurgeEntriesAsync_ValidArguments_ReturnsTheInnerResult()
    {
        var sut = new InstrumentedOperationAuditStore(new FakeStore());

        var result = await sut.PurgeEntriesAsync(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.Match(Right: count => count, Left: _ => -1).ShouldBe(3);
    }

    [Fact]
    public async Task PurgeEntriesAsync_InnerThrows_PropagatesTheException()
    {
        var sut = new InstrumentedOperationAuditStore(new FakeStore(throwOnPurge: true));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.PurgeEntriesAsync(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }
}
