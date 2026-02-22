#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Configuration;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsConfigurationProviderTests : IDisposable
{
    private readonly ISecretReader _mockReader;
    private readonly ServiceCollection _services;

    public SecretsConfigurationProviderTests()
    {
        _mockReader = Substitute.For<ISecretReader>();
        _services = new ServiceCollection();
        _services.AddLogging();
        _services.AddSingleton(_mockReader);
    }

    public void Dispose()
    {
        // Dispose any providers created during tests
    }

    #region Load - Basic

    [Fact]
    public void Load_WithSecrets_PopulatesData()
    {
        _mockReader.GetSecretAsync("db-connection", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("Server=localhost"));

        var provider = CreateProvider(o => o.SecretNames = ["db-connection"]);
        provider.Load();

        provider.TryGet("db-connection", out var value).Should().BeTrue();
        value.Should().Be("Server=localhost");
    }

    [Fact]
    public void Load_MultipleSecrets_PopulatesAllData()
    {
        _mockReader.GetSecretAsync("secret-1", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("value-1"));
        _mockReader.GetSecretAsync("secret-2", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("value-2"));

        var provider = CreateProvider(o => o.SecretNames = ["secret-1", "secret-2"]);
        provider.Load();

        provider.TryGet("secret-1", out var v1).Should().BeTrue();
        v1.Should().Be("value-1");
        provider.TryGet("secret-2", out var v2).Should().BeTrue();
        v2.Should().Be("value-2");
    }

    [Fact]
    public void Load_NoSecretNames_ProducesEmptyData()
    {
        var provider = CreateProvider(o => o.SecretNames = []);
        provider.Load();

        provider.TryGet("anything", out _).Should().BeFalse();
    }

    [Fact]
    public void Load_NoSecretReader_DoesNotThrow()
    {
        // Build without registering ISecretReader
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var options = new SecretsConfigurationOptions { SecretNames = ["some-secret"] };
        var provider = new SecretsConfigurationProvider(sp, options);

        var act = () => provider.Load();

        act.Should().NotThrow();
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        var provider = CreateProvider(o => o.SecretNames = []);
        provider.Load();

        provider.TryGet("nonexistent", out _).Should().BeFalse();
    }

    #endregion

    #region Load - Error Handling

    [Fact]
    public void Load_SecretReaderReturnsLeft_SkipsEntry()
    {
        _mockReader.GetSecretAsync("bad-secret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>(
                SecretsErrors.NotFound("bad-secret")));

        var provider = CreateProvider(o => o.SecretNames = ["bad-secret"]);
        provider.Load();

        provider.TryGet("bad-secret", out _).Should().BeFalse();
    }

    [Fact]
    public void Load_MixedSuccessAndFailure_OnlyStoresSuccessful()
    {
        _mockReader.GetSecretAsync("good-secret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("good-value"));
        _mockReader.GetSecretAsync("bad-secret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>(
                SecretsErrors.NotFound("bad-secret")));

        var provider = CreateProvider(o => o.SecretNames = ["good-secret", "bad-secret"]);
        provider.Load();

        provider.TryGet("good-secret", out var value).Should().BeTrue();
        value.Should().Be("good-value");
        provider.TryGet("bad-secret", out _).Should().BeFalse();
    }

    #endregion

    #region Key Mapping - Delimiter

    [Fact]
    public void Load_WithKeyDelimiter_ReplacesDelimiterWithConfigurationSeparator()
    {
        _mockReader.GetSecretAsync("Database--ConnectionString", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("Server=localhost"));

        var provider = CreateProvider(o =>
        {
            o.SecretNames = ["Database--ConnectionString"];
            o.KeyDelimiter = "--";
        });
        provider.Load();

        // "--" should be replaced with ":" (ConfigurationPath.KeyDelimiter)
        provider.TryGet("Database:ConnectionString", out var value).Should().BeTrue();
        value.Should().Be("Server=localhost");
    }

    [Fact]
    public void Load_CustomDelimiter_ReplacesCorrectly()
    {
        _mockReader.GetSecretAsync("App__Settings__Key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("my-value"));

        var provider = CreateProvider(o =>
        {
            o.SecretNames = ["App__Settings__Key"];
            o.KeyDelimiter = "__";
        });
        provider.Load();

        provider.TryGet("App:Settings:Key", out var value).Should().BeTrue();
        value.Should().Be("my-value");
    }

    [Fact]
    public void Load_EmptyDelimiter_KeysPassedThrough()
    {
        _mockReader.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("value"));

        var provider = CreateProvider(o =>
        {
            o.SecretNames = ["my-secret"];
            o.KeyDelimiter = "";
        });
        provider.Load();

        provider.TryGet("my-secret", out var value).Should().BeTrue();
        value.Should().Be("value");
    }

    #endregion

    #region Key Mapping - Prefix

    [Fact]
    public void Load_WithPrefix_PrependsToSecretName()
    {
        _mockReader.GetSecretAsync("myapp/api-key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("key-123"));

        var provider = CreateProvider(o =>
        {
            o.SecretNames = ["api-key"];
            o.SecretPrefix = "myapp/";
            o.StripPrefix = true;
        });
        provider.Load();

        // With StripPrefix=true, "myapp/" is stripped from the config key
        provider.TryGet("api-key", out var value).Should().BeTrue();
        value.Should().Be("key-123");
    }

    [Fact]
    public void Load_WithPrefix_StripPrefixFalse_KeepsPrefixInKey()
    {
        _mockReader.GetSecretAsync("myapp/api-key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("key-123"));

        var provider = CreateProvider(o =>
        {
            o.SecretNames = ["api-key"];
            o.SecretPrefix = "myapp/";
            o.StripPrefix = false;
        });
        provider.Load();

        // With StripPrefix=false, the full resolved name (with prefix) is the key
        provider.TryGet("myapp/api-key", out var value).Should().BeTrue();
        value.Should().Be("key-123");
    }

    [Fact]
    public void Load_WithPrefixAndDelimiter_CombinesBothTransformations()
    {
        _mockReader.GetSecretAsync("prod/Database--Password", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("secret-pass"));

        var provider = CreateProvider(o =>
        {
            o.SecretNames = ["Database--Password"];
            o.SecretPrefix = "prod/";
            o.StripPrefix = true;
            o.KeyDelimiter = "--";
        });
        provider.Load();

        // "prod/" stripped, "--" replaced with ":"
        provider.TryGet("Database:Password", out var value).Should().BeTrue();
        value.Should().Be("secret-pass");
    }

    #endregion

    #region Case Insensitive Keys

    [Fact]
    public void TryGet_CaseInsensitive_ReturnsValue()
    {
        _mockReader.GetSecretAsync("MySecret", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("value"));

        var provider = CreateProvider(o => o.SecretNames = ["MySecret"]);
        provider.Load();

        provider.TryGet("mysecret", out var value).Should().BeTrue();
        value.Should().Be("value");
    }

    #endregion

    #region IConfigurationRoot Integration

    [Fact]
    public void Integration_BuildConfiguration_SecretsAvailableViaIndexer()
    {
        _mockReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("my-api-key"));

        var sp = _services.BuildServiceProvider();
        var options = new SecretsConfigurationOptions { SecretNames = ["api-key"] };

        var config = new ConfigurationBuilder()
            .Add(new SecretsConfigurationSource(sp, options))
            .Build();

        config["api-key"].Should().Be("my-api-key");
    }

    [Fact]
    public void Integration_BuildConfiguration_HierarchicalKeys_AccessViaGetSection()
    {
        _mockReader.GetSecretAsync("Database--ConnectionString", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("Server=localhost"));

        var sp = _services.BuildServiceProvider();
        var options = new SecretsConfigurationOptions
        {
            SecretNames = ["Database--ConnectionString"],
            KeyDelimiter = "--"
        };

        var config = new ConfigurationBuilder()
            .Add(new SecretsConfigurationSource(sp, options))
            .Build();

        config.GetSection("Database")["ConnectionString"].Should().Be("Server=localhost");
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var provider = CreateProvider(o => o.SecretNames = []);

        var act = () =>
        {
            provider.Dispose();
            provider.Dispose();
        };

        act.Should().NotThrow();
    }

    #endregion

    #region Helpers

    private SecretsConfigurationProvider CreateProvider(Action<SecretsConfigurationOptions> configure)
    {
        var sp = _services.BuildServiceProvider();
        var options = new SecretsConfigurationOptions();
        configure(options);
        return new SecretsConfigurationProvider(sp, options);
    }

    #endregion
}
