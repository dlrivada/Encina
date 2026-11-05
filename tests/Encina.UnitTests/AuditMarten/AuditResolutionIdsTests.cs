using Encina.Audit.Marten.Resolution;

using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="AuditResolutionIds"/>: the per-resolution id and the valid field set per trail
/// (issue #3049, phase 1).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditResolutionIdsTests
{
    [Fact]
    public void NewId_IsAVersion7Guid()
    {
        var id = AuditResolutionIds.NewId(new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero)));

        id.Version.ShouldBe(7);
    }

    [Fact]
    public void NewId_TakesItsTimestampFromTheTimeProvider()
    {
        var at = new DateTimeOffset(2026, 11, 4, 9, 30, 15, 250, TimeSpan.Zero);

        var id = AuditResolutionIds.NewId(new FakeTimeProvider(at));

        // The first 48 bits of a UUID version 7 are the Unix time in milliseconds (the leading 12 hex digits).
        var hex = id.ToString("N")[..12];
        Convert.ToInt64(hex, 16).ShouldBe(at.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void NewId_IdsCreatedInDifferentMilliseconds_SortByCreationTime()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero));
        var ids = new List<Guid>();
        for (var i = 0; i < 50; i++)
        {
            ids.Add(AuditResolutionIds.NewId(clock));
            clock.Advance(TimeSpan.FromMilliseconds(1 + (i % 3)));
        }

        // Postgres uuid and the canonical text form compare big-endian, which is the order that sorts version 7 ids.
        var sorted = ids.OrderBy(x => x.ToString("D"), StringComparer.Ordinal).ToList();

        sorted.ShouldBe(ids);
    }

    [Fact]
    public void NewId_SameInstant_GivesDistinctIds()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero));

        AuditResolutionIds.NewId(clock).ShouldNotBe(AuditResolutionIds.NewId(clock));
    }

    [Fact]
    public void NewId_NullTimeProvider_Throws() =>
        Should.Throw<ArgumentNullException>(() => AuditResolutionIds.NewId(null!));

    [Theory]
    [InlineData(AuditResolutionField.All)]
    [InlineData(AuditResolutionField.UserId)]
    [InlineData(AuditResolutionField.IpAddress)]
    [InlineData(AuditResolutionField.UserAgent)]
    [InlineData(AuditResolutionField.RequestPayload)]
    [InlineData(AuditResolutionField.ResponsePayload)]
    [InlineData(AuditResolutionField.MetadataJson)]
    public void IsValid_OperationTrail_AcceptsItsFields(AuditResolutionField field) =>
        AuditResolutionIds.IsValid(AuditResolutionTrail.Operation, field).ShouldBeTrue();

    [Fact]
    public void IsValid_OperationTrail_RejectsPurpose() =>
        AuditResolutionIds.IsValid(AuditResolutionTrail.Operation, AuditResolutionField.Purpose).ShouldBeFalse();

    [Theory]
    [InlineData(AuditResolutionField.All)]
    [InlineData(AuditResolutionField.UserId)]
    [InlineData(AuditResolutionField.Purpose)]
    [InlineData(AuditResolutionField.MetadataJson)]
    public void IsValid_ReadTrail_AcceptsItsFields(AuditResolutionField field) =>
        AuditResolutionIds.IsValid(AuditResolutionTrail.Read, field).ShouldBeTrue();

    [Theory]
    [InlineData(AuditResolutionField.IpAddress)]
    [InlineData(AuditResolutionField.UserAgent)]
    [InlineData(AuditResolutionField.RequestPayload)]
    [InlineData(AuditResolutionField.ResponsePayload)]
    public void IsValid_ReadTrail_RejectsOperationOnlyFields(AuditResolutionField field) =>
        AuditResolutionIds.IsValid(AuditResolutionTrail.Read, field).ShouldBeFalse();

    [Fact]
    public void IsValid_UndefinedTrail_IsRejectedForEveryField()
    {
        const AuditResolutionTrail undefined = (AuditResolutionTrail)99;

        AuditResolutionIds.IsValid(undefined, AuditResolutionField.All).ShouldBeFalse();
        AuditResolutionIds.IsValid(undefined, AuditResolutionField.UserId).ShouldBeFalse();
        AuditResolutionIds.IsValid(undefined, AuditResolutionField.IpAddress).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_UndefinedField_IsRejected() =>
        AuditResolutionIds.IsValid(AuditResolutionTrail.Operation, (AuditResolutionField)99).ShouldBeFalse();

    [Fact]
    public void Enums_HaveTheDocumentedExplicitValues()
    {
        ((int)AuditResolutionTrail.Operation).ShouldBe(0);
        ((int)AuditResolutionTrail.Read).ShouldBe(1);
        ((int)AuditResolutionKind.Unreadable).ShouldBe(0);
        ((int)AuditResolutionKind.Shredded).ShouldBe(1);
        ((int)AuditResolutionKind.Expired).ShouldBe(2);
        ((int)AuditResolutionKind.Excluded).ShouldBe(3);
        ((int)AuditResolutionKind.Withdrawn).ShouldBe(4);
        ((int)AuditResolutionField.All).ShouldBe(0);
        ((int)AuditResolutionField.Purpose).ShouldBe(7);
    }
}
