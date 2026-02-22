using Encina.Security.Secrets.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsConfigurationSourceTests
{
    #region Constructor Validation

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var options = new SecretsConfigurationOptions();

        var act = () => new SecretsConfigurationSource(null!, options);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var provider = new ServiceCollection().BuildServiceProvider();

        var act = () => new SecretsConfigurationSource(provider, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
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

        result.Should().NotBeNull();
        result.Should().BeOfType<SecretsConfigurationProvider>();
    }

    #endregion
}
