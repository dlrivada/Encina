using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.CombiningAlgorithms;

/// <summary>
/// Unit tests for <see cref="CombiningAlgorithmFactory"/>.
/// Verifies all 8 XACML 3.0 algorithms are pre-registered and
/// invalid IDs throw <see cref="ArgumentOutOfRangeException"/>.
/// </summary>
public sealed class CombiningAlgorithmFactoryTests
{
    private readonly CombiningAlgorithmFactory _sut = new();

    #region GetAlgorithm — All 8 Registered Algorithms

    [Theory]
    [InlineData(CombiningAlgorithmId.DenyOverrides, typeof(DenyOverridesAlgorithm))]
    [InlineData(CombiningAlgorithmId.PermitOverrides, typeof(PermitOverridesAlgorithm))]
    [InlineData(CombiningAlgorithmId.FirstApplicable, typeof(FirstApplicableAlgorithm))]
    [InlineData(CombiningAlgorithmId.OnlyOneApplicable, typeof(OnlyOneApplicableAlgorithm))]
    [InlineData(CombiningAlgorithmId.DenyUnlessPermit, typeof(DenyUnlessPermitAlgorithm))]
    [InlineData(CombiningAlgorithmId.PermitUnlessDeny, typeof(PermitUnlessDenyAlgorithm))]
    [InlineData(CombiningAlgorithmId.OrderedDenyOverrides, typeof(OrderedDenyOverridesAlgorithm))]
    [InlineData(CombiningAlgorithmId.OrderedPermitOverrides, typeof(OrderedPermitOverridesAlgorithm))]
    public void GetAlgorithm_RegisteredId_ReturnsCorrectType(
        CombiningAlgorithmId algorithmId, Type expectedType)
    {
        var algorithm = _sut.GetAlgorithm(algorithmId);

        algorithm.ShouldNotBeNull();
        algorithm.ShouldBeOfType(expectedType);
    }

    [Theory]
    [InlineData(CombiningAlgorithmId.DenyOverrides)]
    [InlineData(CombiningAlgorithmId.PermitOverrides)]
    [InlineData(CombiningAlgorithmId.FirstApplicable)]
    [InlineData(CombiningAlgorithmId.OnlyOneApplicable)]
    [InlineData(CombiningAlgorithmId.DenyUnlessPermit)]
    [InlineData(CombiningAlgorithmId.PermitUnlessDeny)]
    [InlineData(CombiningAlgorithmId.OrderedDenyOverrides)]
    [InlineData(CombiningAlgorithmId.OrderedPermitOverrides)]
    public void GetAlgorithm_RegisteredId_AlgorithmIdMatchesRequested(
        CombiningAlgorithmId algorithmId)
    {
        var algorithm = _sut.GetAlgorithm(algorithmId);

        algorithm.AlgorithmId.ShouldBe(algorithmId);
    }

    #endregion

    #region GetAlgorithm — Same Instance on Repeated Calls

    [Theory]
    [InlineData(CombiningAlgorithmId.DenyOverrides)]
    [InlineData(CombiningAlgorithmId.PermitOverrides)]
    [InlineData(CombiningAlgorithmId.FirstApplicable)]
    public void GetAlgorithm_SameId_ReturnsSameInstance(CombiningAlgorithmId algorithmId)
    {
        var first = _sut.GetAlgorithm(algorithmId);
        var second = _sut.GetAlgorithm(algorithmId);

        first.ShouldBeSameAs(second,
            "Factory pre-registers instances; same reference should be returned");
    }

    #endregion

    #region GetAlgorithm — Invalid Id

    [Fact]
    public void GetAlgorithm_InvalidId_ThrowsArgumentOutOfRangeException()
    {
        var invalidId = (CombiningAlgorithmId)999;

        var act = () => _sut.GetAlgorithm(invalidId);

        Should.Throw<ArgumentOutOfRangeException>(act)
                .ParamName.ShouldBe("algorithmId");
    }

    #endregion

    #region Coverage — All Enum Values Have Registrations

    [Fact]
    public void AllCombiningAlgorithmIds_AreRegistered()
    {
        var allIds = Enum.GetValues<CombiningAlgorithmId>();

        foreach (var id in allIds)
        {
            var act = () => _sut.GetAlgorithm(id);

            Should.NotThrow(act);
        }
    }

    [Fact]
    public void AllCombiningAlgorithmIds_Count_IsEight()
    {
        var count = Enum.GetValues<CombiningAlgorithmId>().Length;

        count.ShouldBe(8,
            "XACML 3.0 defines exactly 8 combining algorithms");
    }

    #endregion
}
