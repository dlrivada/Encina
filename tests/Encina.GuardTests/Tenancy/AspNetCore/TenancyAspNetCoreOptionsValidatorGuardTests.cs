using Encina.Tenancy.AspNetCore;

using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Tenancy.AspNetCore;

/// <summary>
/// Guard tests for <see cref="TenancyAspNetCoreOptionsValidator"/>.
/// </summary>
public class TenancyAspNetCoreOptionsValidatorGuardTests
{
    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new TenancyAspNetCoreOptionsValidator();

        var act = () => sut.Validate(null, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var sut = new TenancyAspNetCoreOptionsValidator();

        var result = sut.Validate(null, new TenancyAspNetCoreOptions());

        result.ShouldBe(ValidateOptionsResult.Success);
    }
}
