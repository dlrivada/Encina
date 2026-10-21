using Encina.Compliance.AIAct.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.AIAct.Model;

/// <summary>
/// Unit tests for <see cref="ProhibitedPractice"/> and <see cref="ProhibitedPracticeExtensions"/>.
/// </summary>
public class ProhibitedPracticeTests
{
    public static TheoryData<ProhibitedPractice, string> ExpectedPoints => new()
    {
        { ProhibitedPractice.SocialScoring, "5(1)(c)" },
        { ProhibitedPractice.RealTimeBiometricPublicSpaces, "5(1)(h)" },
        { ProhibitedPractice.EmotionRecognitionWorkplace, "5(1)(f)" },
        { ProhibitedPractice.EmotionRecognitionEducation, "5(1)(f)" },
        { ProhibitedPractice.UntargetedFacialScraping, "5(1)(e)" },
        { ProhibitedPractice.PredictivePolicing, "5(1)(d)" },
        { ProhibitedPractice.BiometricCategorisation, "5(1)(g)" },
        { ProhibitedPractice.SubliminalManipulation, "5(1)(a)" },
        { ProhibitedPractice.VulnerabilityExploitation, "5(1)(b)" },
        { ProhibitedPractice.NonConsensualIntimateDeepfakes, "5(1)(ba)" },
        { ProhibitedPractice.ChildSexualAbuseMaterial, "5(1)(bb)" },
    };

    [Theory]
    [MemberData(nameof(ExpectedPoints))]
    public void ArticlePoint_ReturnsExpectedPoint(ProhibitedPractice practice, string expected)
    {
        practice.ArticlePoint.ShouldBe(expected);
    }

    [Fact]
    public void ExpectedTable_CoversEveryMember()
    {
        var tabled = ExpectedPoints.Select(row => row.Data.Item1).ToHashSet();

        tabled.ShouldBe(Enum.GetValues<ProhibitedPractice>().ToHashSet(), ignoreOrder: true);
    }

    [Fact]
    public void ArticlePoint_AcrossAllMembers_YieldsTheTenPointsOfArticle5Paragraph1()
    {
        var points = Enum.GetValues<ProhibitedPractice>()
            .Select(practice => practice.ArticlePoint)
            .ToHashSet();

        points.ShouldBe(
            ["5(1)(a)", "5(1)(b)", "5(1)(ba)", "5(1)(bb)", "5(1)(c)", "5(1)(d)", "5(1)(e)", "5(1)(f)", "5(1)(g)", "5(1)(h)"],
            ignoreOrder: true);
    }

    [Fact]
    public void ArticlePoint_UndefinedValue_ThrowsArgumentOutOfRangeException()
    {
        var undefined = (ProhibitedPractice)999;

        var act = () => undefined.ArticlePoint;

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("practice");
    }

    [Theory]
    [InlineData(ProhibitedPractice.VulnerabilityExploitation, 8)]
    [InlineData(ProhibitedPractice.NonConsensualIntimateDeepfakes, 9)]
    [InlineData(ProhibitedPractice.ChildSexualAbuseMaterial, 10)]
    public void NewMembers_KeepTheirExplicitValues(ProhibitedPractice practice, int expected)
    {
        ((int)practice).ShouldBe(expected);
    }
}
