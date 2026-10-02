using System.Text.Json;
using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.PII;

/// <summary>
/// Hash mode is a keyed HMAC-SHA256 and fails closed without a key (#858).
/// </summary>
public sealed class PIIHashKeyTests
{
    private const string Secret = "super-secret-hash-key-value";

    #region Options validation

    [Fact]
    public void Validate_HashDefaultModeWithoutKey_Fails()
    {
        var logger = new CapturingLogger<PIIOptionsValidator>();
        var sut = new PIIOptionsValidator(logger);

        var result = sut.Validate(null, new PIIOptions { DefaultMode = MaskingMode.Hash });

        result.Failed.ShouldBeTrue();
        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_HashDefaultModeWithKey_Succeeds()
    {
        var sut = new PIIOptionsValidator(NullLogger<PIIOptionsValidator>.Instance);

        var result = sut.Validate(null, new PIIOptions { DefaultMode = MaskingMode.Hash, HashKey = Secret });

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_HashWithAllowUnkeyedHash_SucceedsAndLogsOneWarning()
    {
        var logger = new CapturingLogger<PIIOptionsValidator>();
        var sut = new PIIOptionsValidator(logger);

        var result = sut.Validate(
            null, new PIIOptions { DefaultMode = MaskingMode.Hash, AllowUnkeyedHash = true });

        result.Succeeded.ShouldBeTrue();
        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
        logger.Entries[0].EventId.Id.ShouldBe(8019);
    }

    [Fact]
    public void Validate_PartialModeWithoutKey_SucceedsWithoutWarning()
    {
        var logger = new CapturingLogger<PIIOptionsValidator>();
        var sut = new PIIOptionsValidator(logger);

        var result = sut.Validate(null, new PIIOptions());

        result.Succeeded.ShouldBeTrue();
        logger.Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceKey_Fails(string key)
    {
        var sut = new PIIOptionsValidator(NullLogger<PIIOptionsValidator>.Instance);

        var result = sut.Validate(null, new PIIOptions { HashKey = key, AllowUnkeyedHash = true });

        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_FailureMessage_NeverContainsTheKey()
    {
        var sut = new PIIOptionsValidator(NullLogger<PIIOptionsValidator>.Instance);

        var result = sut.Validate(null, new PIIOptions { HashKey = " " });

        string.Join(' ', result.Failures!).ShouldNotContain(Secret);
    }

    #endregion

    #region Secret handling

    [Fact]
    public void PIIOptions_ToStringAndJson_NeverContainTheKey()
    {
        var options = new PIIOptions { HashKey = Secret };

        options.ToString().ShouldNotContain(Secret);
        options.ToString().ShouldContain("HashKeyConfigured = True");
        JsonSerializer.Serialize(options).ShouldNotContain(Secret);
    }

    [Fact]
    public void MaskingOptions_ToStringAndJson_NeverContainTheKey()
    {
        var options = new MaskingOptions { HashKey = Secret };

        options.ToString().ShouldNotContain(Secret);
        options.ToString().ShouldContain("HashKeyConfigured = True");
        JsonSerializer.Serialize(options).ShouldNotContain(Secret);
    }

    #endregion

    #region Registration

    [Fact]
    public void AddEncinaPII_HashWithoutKey_ThrowsAtRegistration()
    {
        var services = new ServiceCollection();

        Should.Throw<OptionsValidationException>(() =>
            services.AddEncinaPII(o => o.DefaultMode = MaskingMode.Hash));
    }

    [Fact]
    public void AddEncinaPII_HashWithKey_RegistersAndBuildsWithValidateOnBuildAndScopes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPII(o =>
        {
            o.DefaultMode = MaskingMode.Hash;
            o.HashKey = Secret;
        });

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IStartupValidator>().Validate();
        provider.GetRequiredService<IPIIMasker>().ShouldBeOfType<PIIMasker>();
    }

    [Fact]
    public void AddEncinaPII_OptionsChangedAfterRegistration_FailValidationOnStart()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPII();
        services.Configure<PIIOptions>(o => o.DefaultMode = MaskingMode.Hash);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    [Fact]
    public void AddEncinaPII_AllowUnkeyedHash_BuildsAndLogsWarningOnStart()
    {
        var logger = new CapturingLogger<PIIOptionsValidator>();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<PIIOptionsValidator>>(logger);
        services.AddLogging();
        services.AddEncinaPII(o =>
        {
            o.DefaultMode = MaskingMode.Hash;
            o.AllowUnkeyedHash = true;
        });

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IStartupValidator>().Validate();

        logger.Entries.Count(e => e.EventId.Id == 8019).ShouldBe(1);
    }

    #endregion

    #region Masker

    [Fact]
    public void Mask_HashModeWithKey_ReturnsKeyedHmac()
    {
        var masker = CreateMasker(new PIIOptions { DefaultMode = MaskingMode.Hash, HashKey = "Jefe" });

        var masked = masker.Mask("what do ya want for nothing?", PIIType.Custom);

        masked.ShouldBe("5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843");
    }

    [Fact]
    public void Mask_HashModeWithAllowUnkeyedHash_ReturnsPlainSha256()
    {
        var masker = CreateMasker(new PIIOptions { DefaultMode = MaskingMode.Hash, AllowUnkeyedHash = true });

        masker.Mask("abc", PIIType.Custom)
            .ShouldBe("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }

    [Fact]
    public void Mask_HashModeWithoutKeyOrOptOut_RedactsInsteadOfHashing()
    {
        var logger = new CapturingLogger<PIIMasker>();
        var masker = CreateMasker(new PIIOptions { DefaultMode = MaskingMode.Hash }, logger);

        var masked = masker.Mask("abc", PIIType.Custom);

        masked.ShouldBe("[REDACTED]");
        logger.Entries.ShouldContain(e => e.EventId.Id == 8020 && e.Level == LogLevel.Error);
    }

    private static PIIMasker CreateMasker(PIIOptions options, ILogger<PIIMasker>? logger = null) =>
        new(Options.Create(options), logger ?? NullLogger<PIIMasker>.Instance,
            new ServiceCollection().BuildServiceProvider());

    #endregion

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, EventId EventId)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, eventId));
    }
}
