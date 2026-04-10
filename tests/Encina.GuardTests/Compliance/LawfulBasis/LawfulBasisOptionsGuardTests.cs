using Encina.Compliance.LawfulBasis;

namespace Encina.GuardTests.Compliance.LawfulBasis;

/// <summary>
/// Guard tests for <see cref="LawfulBasisOptions"/> and <see cref="LawfulBasisOptionsValidator"/>.
/// </summary>
public class LawfulBasisOptionsGuardTests
{
    [Fact]
    public void ScanAssembly_NullAssembly_Throws()
    {
        var options = new LawfulBasisOptions();
        Should.Throw<ArgumentNullException>(() => options.ScanAssembly(null!));
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        var validator = new LawfulBasisOptionsValidator();
        Should.Throw<ArgumentNullException>(() => validator.Validate(null, null!));
    }

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var validator = new LawfulBasisOptionsValidator();
        var result = validator.Validate(null, new LawfulBasisOptions());
        result.Succeeded.ShouldBeTrue();
    }
}
