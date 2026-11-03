using Encina.Audit.Marten.Resolution;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="AuditResolutionIds"/>: the deterministic document id and the valid field set per trail
/// (issue #3049, phase 1).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditResolutionIdsTests
{
    private static readonly Guid EventId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    [Theory]
    [InlineData(AuditResolutionTrail.Operation, AuditResolutionField.All, "audit-resolution:Operation:0f8fad5b-d9cb-469f-a165-70867728950e:All")]
    [InlineData(AuditResolutionTrail.Operation, AuditResolutionField.IpAddress, "audit-resolution:Operation:0f8fad5b-d9cb-469f-a165-70867728950e:IpAddress")]
    [InlineData(AuditResolutionTrail.Read, AuditResolutionField.Purpose, "audit-resolution:Read:0f8fad5b-d9cb-469f-a165-70867728950e:Purpose")]
    public void Format_ReturnsTheDocumentedIdShape(AuditResolutionTrail trail, AuditResolutionField field, string expected) =>
        AuditResolutionIds.Format(trail, EventId, field).ShouldBe(expected);

    [Fact]
    public void Format_DifferentTrailEventOrField_GivesDifferentIds()
    {
        var baseId = AuditResolutionIds.Format(AuditResolutionTrail.Operation, EventId, AuditResolutionField.UserId);

        AuditResolutionIds.Format(AuditResolutionTrail.Read, EventId, AuditResolutionField.UserId).ShouldNotBe(baseId);
        AuditResolutionIds.Format(AuditResolutionTrail.Operation, Guid.NewGuid(), AuditResolutionField.UserId).ShouldNotBe(baseId);
        AuditResolutionIds.Format(AuditResolutionTrail.Operation, EventId, AuditResolutionField.MetadataJson).ShouldNotBe(baseId);
    }

    [Fact]
    public void Format_SameInputs_GivesTheSameId() =>
        AuditResolutionIds.Format(AuditResolutionTrail.Read, EventId, AuditResolutionField.All)
            .ShouldBe(AuditResolutionIds.Format(AuditResolutionTrail.Read, EventId, AuditResolutionField.All));

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
        ((int)AuditResolutionField.All).ShouldBe(0);
        ((int)AuditResolutionField.Purpose).ShouldBe(7);
    }
}
