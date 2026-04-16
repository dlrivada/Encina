using Encina.Security.Secrets.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsConfigurationSourceTests
{
    #region Constructor Validation

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var options = new SecretsConfigurationOptions();

        var act = () => new SecretsConfigurationSource(null!, options);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var provider = new ServiceCollection().BuildServiceProvider();

        var act = () => new SecretsConfigurationSource(provider, null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    #endregion

    #region Build

    [Fact]
    public void Build_ReturnsSecretsConfigurationProvider()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var options = new SecretsConfigurationOptions();
        var source = new SecretsConfigurationSource(provider, options);
        var builder = new ConfigurationBuilder();

        var result = source.Build(builder);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<SecretsConfigurationProvider>();
    }

    #endregion
}
