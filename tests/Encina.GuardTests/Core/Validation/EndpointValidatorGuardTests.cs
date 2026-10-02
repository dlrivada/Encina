using Encina.Validation;

namespace Encina.GuardTests.Core.Validation;

/// <summary>
/// Guard tests for <see cref="EndpointValidator"/> (#852).
/// </summary>
public sealed class EndpointValidatorGuardTests
{
    private static readonly EndpointPolicy Policy = EndpointPolicy.ForHttps(allowInsecureHttp: false, allowLocalEndpoints: false);

    [Fact]
    public void ValidateUri_NullPropertyName_Throws()
    {
        var act = () => EndpointValidator.ValidateUri(new Uri("https://vault.example.com"), null!, Policy);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("propertyName");
    }

    [Fact]
    public void ValidateUri_NullPolicy_Throws()
    {
        var act = () => EndpointValidator.ValidateUri(new Uri("https://vault.example.com"), "VaultUri", null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("policy");
    }

    [Fact]
    public void ValidateUrl_NullPropertyName_Throws()
    {
        var act = () => EndpointValidator.ValidateUrl("https://vault.example.com", null!, Policy);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("propertyName");
    }

    [Fact]
    public void ValidateUrl_NullPolicy_Throws()
    {
        var act = () => EndpointValidator.ValidateUrl("https://vault.example.com", "VaultAddress", null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("policy");
    }

    [Fact]
    public void ValidateHost_NullPropertyName_Throws()
    {
        var act = () => EndpointValidator.ValidateHost("db.example.com", null!, Policy);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("propertyName");
    }

    [Fact]
    public void ValidateHost_NullPolicy_Throws()
    {
        var act = () => EndpointValidator.ValidateHost("db.example.com", "Hostname", null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("policy");
    }

    [Fact]
    public void ClassifyAddress_NullAddress_Throws()
    {
        Action act = () => EndpointValidator.ClassifyAddress(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("address");
    }

    [Fact]
    public void ClassifyHost_NullHost_ReturnsInvalidInsteadOfThrowing()
    {
        EndpointValidator.ClassifyHost(null).ShouldBe(EndpointHostKind.Invalid);
    }
}
