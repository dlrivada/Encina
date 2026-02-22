using Encina.Security.Secrets;
using Encina.Security.Secrets.Providers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class ConfigurationSecretProviderTests
{
    private readonly ILogger<ConfigurationSecretProvider> _logger =
        Substitute.For<ILogger<ConfigurationSecretProvider>>();

    #region GetSecretAsync (string)

    [Fact]
    public async Task GetSecretAsync_ExistingKey_ReturnsRight()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Secrets:api-key"] = "my-api-key-value"
        });
        var provider = new ConfigurationSecretProvider(config, _logger);

        var result = await provider.GetSecretAsync("api-key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("my-api-key-value"));
    }

    [Fact]
    public async Task GetSecretAsync_NonExistentKey_ReturnsLeftNotFound()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = new ConfigurationSecretProvider(config, _logger);

        var result = await provider.GetSecretAsync("missing-key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_CustomSectionPath_UsesCorrectSection()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Vault:db-password"] = "p@ssw0rd"
        });
        var provider = new ConfigurationSecretProvider(config, _logger, "Vault");

        var result = await provider.GetSecretAsync("db-password");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("p@ssw0rd"));
    }

    [Fact]
    public async Task GetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = new ConfigurationSecretProvider(config, _logger);

        var act = () => provider.GetSecretAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSecretAsync_EmptySecretName_ThrowsArgumentException()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = new ConfigurationSecretProvider(config, _logger);

        var act = () => provider.GetSecretAsync("").AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetSecretAsync<T> (typed)

    [Fact]
    public async Task GetSecretAsync_Typed_ComplexObject_ReturnsDeserializedObject()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Secrets:db-config:Host"] = "localhost",
            ["Secrets:db-config:Port"] = "5432"
        });
        var provider = new ConfigurationSecretProvider(config, _logger);

        var result = await provider.GetSecretAsync<TestDbConfig>("db-config");

        result.IsRight.Should().BeTrue();
        result.IfRight(v =>
        {
            v.Host.Should().Be("localhost");
            v.Port.Should().Be(5432);
        });
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NonExistentKey_ReturnsLeftNotFound()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = new ConfigurationSecretProvider(config, _logger);

        var result = await provider.GetSecretAsync<TestDbConfig>("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_JsonString_DeserializesFromJson()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Secrets:json-secret"] = """{"Host":"db.example.com","Port":3306}"""
        });
        var provider = new ConfigurationSecretProvider(config, _logger);

        var result = await provider.GetSecretAsync<TestDbConfig>("json-secret");

        result.IsRight.Should().BeTrue();
        result.IfRight(v =>
        {
            v.Host.Should().Be("db.example.com");
            v.Port.Should().Be(3306);
        });
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NullSecretName_ThrowsArgumentException()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = new ConfigurationSecretProvider(config, _logger);

        var act = () => provider.GetSecretAsync<TestDbConfig>(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_DefaultSectionPath_IsSecrets()
    {
        ConfigurationSecretProvider.DefaultSectionPath.Should().Be("Secrets");
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        var act = () => new ConfigurationSecretProvider(null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var act = () => new ConfigurationSecretProvider(config, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_EmptySectionPath_ThrowsArgumentException()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var act = () => new ConfigurationSecretProvider(config, _logger, "");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("sectionPath");
    }

    #endregion

    #region Helpers

    private static IConfiguration BuildConfig(Dictionary<string, string?> data) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

    private sealed class TestDbConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    #endregion
}
