using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.Services;

namespace Encina.GuardTests.Compliance.LawfulBasis;

/// <summary>
/// Guard tests for <see cref="DefaultLawfulBasisProvider"/> method argument validation.
/// </summary>
public class LawfulBasisProviderGuardTests
{
    private readonly DefaultLawfulBasisProvider _sut = new(Substitute.For<ILawfulBasisService>());

    [Fact]
    public void Constructor_NullService_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DefaultLawfulBasisProvider(null!));
    }

    [Fact]
    public async Task GetBasisForRequestAsync_NullType_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.GetBasisForRequestAsync(null!));
    }
}
