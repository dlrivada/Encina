using Encina.Marten;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Unit tests for <see cref="CausalChainDirection"/> enum.
/// </summary>
public sealed class CausalChainDirectionTests
{
    [Fact]
    public void Ancestors_HasExpectedValue()
        => CausalChainDirection.Ancestors.ShouldBe(CausalChainDirection.Ancestors);

    [Fact]
    public void Descendants_HasExpectedValue()
        => CausalChainDirection.Descendants.ShouldBe(CausalChainDirection.Descendants);

    [Fact]
    public void HasTwoValues()
        => Enum.GetValues<CausalChainDirection>().Length.ShouldBe(2);
}
