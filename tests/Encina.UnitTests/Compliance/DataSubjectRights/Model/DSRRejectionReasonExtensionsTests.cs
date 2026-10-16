using Encina.Compliance.DataSubjectRights;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Model;

public class DSRRejectionReasonExtensionsTests
{
    public static TheoryData<DSRRejectionReason> AllReasons => new(Enum.GetValues<DSRRejectionReason>());

    [Fact]
    public void IsDraftLawGround_IsTrueOnlyForAbusiveIntent()
    {
        var draft = Enum.GetValues<DSRRejectionReason>().Where(r => r.IsDraftLawGround).ToArray();

        draft.ShouldBe([DSRRejectionReason.AbusiveIntent]);
    }

    [Theory]
    [MemberData(nameof(AllReasons))]
    public void LegalBasis_IsNotEmptyForEveryDefinedReason(DSRRejectionReason reason)
    {
        reason.LegalBasis.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LegalBasis_AbusiveIntentCitesTheOmnibusProposal()
    {
        DSRRejectionReason.AbusiveIntent.LegalBasis.ShouldContain("COM(2025) 837");
    }

    [Fact]
    public void LegalBasis_UndefinedValue_IsEmpty()
    {
        ((DSRRejectionReason)0).LegalBasis.ShouldBeEmpty();
    }

    [Fact]
    public void Members_HaveStableExplicitValuesAndNoZero()
    {
        ((int)DSRRejectionReason.ManifestlyUnfounded).ShouldBe(1);
        ((int)DSRRejectionReason.Excessive).ShouldBe(2);
        ((int)DSRRejectionReason.IdentityNotVerifiable).ShouldBe(3);
        ((int)DSRRejectionReason.RestrictedByLaw).ShouldBe(4);
        ((int)DSRRejectionReason.RightNotApplicable).ShouldBe(5);
        ((int)DSRRejectionReason.Other).ShouldBe(99);
        ((int)DSRRejectionReason.AbusiveIntent).ShouldBe(100);
        Enum.IsDefined((DSRRejectionReason)0).ShouldBeFalse();
    }
}
