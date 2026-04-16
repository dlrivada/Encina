using Encina.Security.Secrets.Configuration;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsConfigurationOptionsTests
{
    #region Default Values

    [Fact]
    public void SecretNames_DefaultsToEmptyList()
    {
        var options = new SecretsConfigurationOptions();

        options.SecretNames.ShouldBeEmpty();
    }

    [Fact]
    public void SecretPrefix_DefaultsToNull()
    {
        var options = new SecretsConfigurationOptions();

        options.SecretPrefix.ShouldBeNull();
    }

    [Fact]
    public void StripPrefix_DefaultsToTrue()
    {
        var options = new SecretsConfigurationOptions();

        options.StripPrefix.ShouldBeTrue();
    }

    [Fact]
    public void KeyDelimiter_DefaultsToDoubleDash()
    {
        var options = new SecretsConfigurationOptions();

        options.KeyDelimiter.ShouldBe("--");
    }

    [Fact]
    public void ReloadInterval_DefaultsToNull()
    {
        var options = new SecretsConfigurationOptions();

        options.ReloadInterval.ShouldBeNull();
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

        options.SecretNames.ShouldBe(secretNames);
        options.SecretPrefix.ShouldBe("myapp/");
        options.StripPrefix.ShouldBeFalse();
        options.KeyDelimiter.ShouldBe("__");
        options.ReloadInterval.ShouldBe(reloadInterval);
    }

    #endregion
}
