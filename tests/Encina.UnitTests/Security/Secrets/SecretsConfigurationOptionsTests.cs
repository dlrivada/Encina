using Encina.Security.Secrets.Configuration;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsConfigurationOptionsTests
{
    #region Default Values

    [Fact]
    public void SecretNames_DefaultsToEmptyList()
    {
        var options = new SecretsConfigurationOptions();

        options.SecretNames.Should().BeEmpty();
    }

    [Fact]
    public void SecretPrefix_DefaultsToNull()
    {
        var options = new SecretsConfigurationOptions();

        options.SecretPrefix.Should().BeNull();
    }

    [Fact]
    public void StripPrefix_DefaultsToTrue()
    {
        var options = new SecretsConfigurationOptions();

        options.StripPrefix.Should().BeTrue();
    }

    [Fact]
    public void KeyDelimiter_DefaultsToDoubleDash()
    {
        var options = new SecretsConfigurationOptions();

        options.KeyDelimiter.Should().Be("--");
    }

    [Fact]
    public void ReloadInterval_DefaultsToNull()
    {
        var options = new SecretsConfigurationOptions();

        options.ReloadInterval.Should().BeNull();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void AllProperties_AreSettable()
    {
        var secretNames = new List<string> { "secret-1", "secret-2" };
        var reloadInterval = TimeSpan.FromMinutes(10);

        var options = new SecretsConfigurationOptions
        {
            SecretNames = secretNames,
            SecretPrefix = "myapp/",
            StripPrefix = false,
            KeyDelimiter = "__",
            ReloadInterval = reloadInterval
        };

        options.SecretNames.Should().BeEquivalentTo(secretNames);
        options.SecretPrefix.Should().Be("myapp/");
        options.StripPrefix.Should().BeFalse();
        options.KeyDelimiter.Should().Be("__");
        options.ReloadInterval.Should().Be(reloadInterval);
    }

    #endregion
}
