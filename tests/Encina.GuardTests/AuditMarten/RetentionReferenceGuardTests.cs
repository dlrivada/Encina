using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Projections;

using Marten;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests for the retention reference (issue #2770, Q3): an invalid reference is a business failure (<c>Left</c>)
/// and nothing is destroyed; the new projection constructor parameter is guarded.
/// </summary>
public sealed class RetentionReferenceGuardTests
{
    private static readonly RetentionReference Invalid = new("", default, default, "");

    [Fact]
    public async Task Marten_InvalidRetentionReference_ReturnsLeftWithoutQuerying()
    {
        var session = Substitute.For<IDocumentSession>();
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(DateTime.UtcNow, TemporalKeyGranularity.Monthly, Invalid);

        result.IsLeft.ShouldBeTrue();
        session.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Marten_SessionFailure_ReturnsLeftInsteadOfThrowing()
    {
        var session = Substitute.For<IDocumentSession>();
        session.When(s => s.Query<TemporalKeyDocument>()).Do(_ => throw new InvalidOperationException("database down"));
        var sut = new MartenTemporalKeyProvider(
            session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(
            DateTime.UtcNow, TemporalKeyGranularity.Monthly, new RetentionReference("p", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "system"));

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemory_InvalidRetentionReference_ReturnsLeft()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var result = await sut.DestroyKeysBeforeAsync(DateTime.UtcNow, TemporalKeyGranularity.Monthly, Invalid);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void OperationProjection_NullExpiredPlaceholder_Throws() =>
        Should.Throw<ArgumentNullException>(() =>
            new OperationAuditEntryProjection("[S]", null!, NullLogger<OperationAuditEntryProjection>.Instance));

    [Fact]
    public void ReadProjection_NullExpiredPlaceholder_Throws() =>
        Should.Throw<ArgumentNullException>(() =>
            new ReadAuditEntryProjection("[S]", null!, NullLogger<ReadAuditEntryProjection>.Instance));
}
