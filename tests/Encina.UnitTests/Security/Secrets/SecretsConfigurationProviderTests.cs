#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Diagnostics;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Configuration;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

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

        provider.TryGet("db-connection", out var value).ShouldBeTrue();
        value.ShouldBe("Server=localhost");
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

        provider.TryGet("secret-1", out var v1).ShouldBeTrue();
        v1.ShouldBe("value-1");
        provider.TryGet("secret-2", out var v2).ShouldBeTrue();
        v2.ShouldBe("value-2");
    }

    [Fact]
    public void Load_NoSecretNames_ProducesEmptyData()
    {
        var provider = CreateProvider(o => o.SecretNames = []);
        provider.Load();

        provider.TryGet("anything", out _).ShouldBeFalse();
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

        Should.NotThrow(act);
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        var provider = CreateProvider(o => o.SecretNames = []);
        provider.Load();

        provider.TryGet("nonexistent", out _).ShouldBeFalse();
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

        provider.TryGet("bad-secret", out _).ShouldBeFalse();
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

        provider.TryGet("good-secret", out var value).ShouldBeTrue();
        value.ShouldBe("good-value");
        provider.TryGet("bad-secret", out _).ShouldBeFalse();
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
        provider.TryGet("Database:ConnectionString", out var value).ShouldBeTrue();
        value.ShouldBe("Server=localhost");
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

        provider.TryGet("App:Settings:Key", out var value).ShouldBeTrue();
        value.ShouldBe("my-value");
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

        provider.TryGet("my-secret", out var value).ShouldBeTrue();
        value.ShouldBe("value");
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
        provider.TryGet("api-key", out var value).ShouldBeTrue();
        value.ShouldBe("key-123");
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
        provider.TryGet("myapp/api-key", out var value).ShouldBeTrue();
        value.ShouldBe("key-123");
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
        provider.TryGet("Database:Password", out var value).ShouldBeTrue();
        value.ShouldBe("secret-pass");
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

        provider.TryGet("mysecret", out var value).ShouldBeTrue();
        value.ShouldBe("value");
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

        config["api-key"].ShouldBe("my-api-key");
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

        config.GetSection("Database")["ConnectionString"].ShouldBe("Server=localhost");
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

        Should.NotThrow(act);
    }

    #endregion

    #region Reload timer

    private static readonly TimeSpan ReloadInterval = TimeSpan.FromMinutes(1);

    [Fact]
    public void ReloadTimer_InjectedClockReachesTheInterval_ReloadsSecretsAndSignalsTheReload()
    {
        // Arrange
        var values = new Queue<string>(["v1", "v2"]);
        _mockReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>(values.Dequeue()));
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var provider = CreateReloadingProvider(clock, new FakeLoggerProvider());
        provider.Load();
        var reloaded = false;
        provider.GetReloadToken().RegisterChangeCallback(_ => reloaded = true, null);

        // Act: the injected clock, not the system clock, fires the reload
        clock.Advance(ReloadInterval);

        // Assert
        provider.TryGet("api-key", out var value).ShouldBeTrue();
        value.ShouldBe("v2");
        reloaded.ShouldBeTrue();
    }

    [Fact]
    public void ReloadTimer_LoadThrows_LogsTheExceptionTypeWithoutItsMessageKeepsTheDataAndKeepsRunning()
    {
        // Arrange: the first load succeeds, the first reload throws, the second reload succeeds
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var call = 0;
        _mockReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(_ => ++call switch
            {
                1 => new ValueTask<Either<EncinaError, string>>("v1"),
                2 => throw new InvalidOperationException(secret),
                _ => new ValueTask<Either<EncinaError, string>>("v3")
            });
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var logs = new FakeLoggerProvider();
        using var provider = CreateReloadingProvider(clock, logs);
        provider.Load();

        // Act: the failing reload must not escape the timer callback
        Should.NotThrow(() => clock.Advance(ReloadInterval));

        // Assert: logged once with EventId 5151, message redacted, previous data kept
        var record = logs.Collector.GetSnapshot().Where(r => r.Id.Id == 5151).ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        var logged = record.Exception.ShouldBeOfType<RedactedException>();
        logged.ToString().ShouldContain(nameof(InvalidOperationException));
        logged.ToString().ShouldNotContain(secret);
        provider.TryGet("api-key", out var kept).ShouldBeTrue();
        kept.ShouldBe("v1");

        // Act: the timer keeps running, so the next interval reloads
        clock.Advance(ReloadInterval);

        // Assert
        provider.TryGet("api-key", out var reloaded).ShouldBeTrue();
        reloaded.ShouldBe("v3");
    }

    [Fact]
    public void ReloadTimer_ReloadCallbackThrows_IsLoggedAndDoesNotEscapeTheTimer()
    {
        // Arrange
        _mockReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("v1"));
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var logs = new FakeLoggerProvider();
        using var provider = CreateReloadingProvider(clock, logs);
        provider.Load();
        provider.GetReloadToken().RegisterChangeCallback(_ => throw new InvalidOperationException("listener failed"), null);

        // Act
        Should.NotThrow(() => clock.Advance(ReloadInterval));

        // Assert
        logs.Collector.GetSnapshot().Where(r => r.Id.Id == 5151).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ReloadTimer_AfterDispose_DoesNotReload()
    {
        // Arrange
        _mockReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, string>>("v1"));
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var provider = CreateReloadingProvider(clock, new FakeLoggerProvider());
        provider.Load();
        _mockReader.ClearReceivedCalls();

        // Act
        provider.Dispose();
        clock.Advance(ReloadInterval * 3);

        // Assert
        await _mockReader.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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

    private SecretsConfigurationProvider CreateReloadingProvider(FakeTimeProvider clock, FakeLoggerProvider logs)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(logs));
        services.AddSingleton(_mockReader);
        var options = new SecretsConfigurationOptions
        {
            SecretNames = ["api-key"],
            ReloadInterval = ReloadInterval
        };
        return new SecretsConfigurationProvider(services.BuildServiceProvider(), options, clock);
    }

    #endregion
}
