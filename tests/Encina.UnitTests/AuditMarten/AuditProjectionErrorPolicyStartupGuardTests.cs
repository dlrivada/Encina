using Encina.Audit.Marten;
using Encina.Audit.Marten.Projections;

using Marten;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Issue #2770 (D22): Encina does not change Marten's store-wide projection error flags; the startup guard fails the
/// host when the application left them skipping, unless the application opts out explicitly (and the opt-out is logged).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class AuditProjectionErrorPolicyStartupGuardTests
{
    private const string Connection = "Host=localhost;Database=unused;Username=u;Password=p";

    public enum SkipSetting
    {
        ErrorsApply,
        ErrorsSerialization,
        RebuildApply,
        RebuildSerialization
    }

    private static void SetFlag(StoreOptions options, SkipSetting flag, bool value)
    {
        switch (flag)
        {
            case SkipSetting.ErrorsApply: options.Projections.Errors.SkipApplyErrors = value; break;
            case SkipSetting.ErrorsSerialization: options.Projections.Errors.SkipSerializationErrors = value; break;
            case SkipSetting.RebuildApply: options.Projections.RebuildErrors.SkipApplyErrors = value; break;
            default: options.Projections.RebuildErrors.SkipSerializationErrors = value; break;
        }
    }

    private static StoreOptions StoreWithAllFlags(bool value)
    {
        var options = new StoreOptions();
        foreach (var flag in Enum.GetValues<SkipSetting>())
        {
            SetFlag(options, flag, value);
        }

        return options;
    }

    private static (AuditProjectionErrorPolicyStartupGuard Guard, CapturingLogger Log) CreateSut(StoreOptions storeOptions, bool allowSkipped = false)
    {
        var store = Substitute.For<IDocumentStore>();
        store.Options.Returns(storeOptions);
        var log = new CapturingLogger();
        var guard = new AuditProjectionErrorPolicyStartupGuard(
            store,
            Options.Create(new MartenOperationAuditOptions { AllowSkippedProjectionErrors = allowSkipped }),
            log);
        return (guard, log);
    }

    [Fact]
    public async Task StartingAsync_AllFlagsFalse_Starts()
    {
        var (guard, log) = CreateSut(StoreWithAllFlags(false));

        await Should.NotThrowAsync(() => guard.StartingAsync(CancellationToken.None));

        log.Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(SkipSetting.ErrorsApply, "Errors.SkipApplyErrors")]
    [InlineData(SkipSetting.ErrorsSerialization, "Errors.SkipSerializationErrors")]
    [InlineData(SkipSetting.RebuildApply, "RebuildErrors.SkipApplyErrors")]
    [InlineData(SkipSetting.RebuildSerialization, "RebuildErrors.SkipSerializationErrors")]
    public async Task StartingAsync_OneFlagSkipping_FailsNamingThatFlag(SkipSetting flag, string expectedName)
    {
        var storeOptions = StoreWithAllFlags(false);
        SetFlag(storeOptions, flag, true);
        var (guard, log) = CreateSut(storeOptions);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => guard.StartingAsync(CancellationToken.None));

        ex.Message.ShouldStartWith(AuditProjectionErrorPolicyStartupGuard.FailureMessage);
        ex.Message.ShouldEndWith(expectedName + ".");
        log.Entries.Single().Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task StartingAsync_MartenDefaults_FailBecauseMartenSkipsByDefault()
    {
        var (guard, _) = CreateSut(new StoreOptions());

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => guard.StartingAsync(CancellationToken.None));

        ex.Message.ShouldContain("Errors.SkipApplyErrors");
    }

    [Fact]
    public async Task StartingAsync_FailureMessage_ExplainsWhyAndTheStoreWideEffectAndTheOptOut()
    {
        var (guard, _) = CreateSut(StoreWithAllFlags(true));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => guard.StartingAsync(CancellationToken.None));

        ex.Message.ShouldContain("pause the projection shard");
        ex.Message.ShouldContain("store-wide");
        ex.Message.ShouldContain("every other projection");
        ex.Message.ShouldContain(nameof(MartenOperationAuditOptions.AllowSkippedProjectionErrors));
        ex.Message.ShouldContain("Errors.SkipApplyErrors, Errors.SkipSerializationErrors, RebuildErrors.SkipApplyErrors, RebuildErrors.SkipSerializationErrors");
    }

    [Fact]
    public async Task StartingAsync_OptOut_StartsAndLogsAWarningWithTheOptionNameOnly()
    {
        var (guard, log) = CreateSut(StoreWithAllFlags(true), allowSkipped: true);

        await Should.NotThrowAsync(() => guard.StartingAsync(CancellationToken.None));

        var entry = log.Entries.Single();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldContain(nameof(MartenOperationAuditOptions.AllowSkippedProjectionErrors));
        entry.Message.ShouldNotContain(Connection);
    }

    [Fact]
    public async Task StartingAsync_OptOutButFlagsAlreadyFalse_StartsWithoutLogging()
    {
        var (guard, log) = CreateSut(StoreWithAllFlags(false), allowSkipped: true);

        await guard.StartingAsync(CancellationToken.None);

        log.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartingAsync_StoreOptionsAreNotMartensStoreOptions_FailsClosed()
    {
        var store = Substitute.For<IDocumentStore>();
        store.Options.Returns(Substitute.For<IReadOnlyStoreOptions>());
        var guard = new AuditProjectionErrorPolicyStartupGuard(
            store, Options.Create(new MartenOperationAuditOptions { AllowSkippedProjectionErrors = true }), new CapturingLogger());

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => guard.StartingAsync(CancellationToken.None));

        ex.Message.ShouldBe(AuditProjectionErrorPolicyStartupGuard.UnverifiableMessage);
    }

    [Fact]
    public async Task OtherLifecycleMethods_Complete()
    {
        var (guard, _) = CreateSut(StoreWithAllFlags(true));

        await guard.StartAsync(CancellationToken.None);
        await guard.StartedAsync(CancellationToken.None);
        await guard.StoppingAsync(CancellationToken.None);
        await guard.StopAsync(CancellationToken.None);
        await guard.StoppedAsync(CancellationToken.None);
    }

    [Fact]
    public void SkippingFlags_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => AuditProjectionErrorPolicyStartupGuard.SkippingFlags(null!));

    // ---- the guard sees the application's final configuration, whatever the registration order ----

    [Fact]
    public async Task Registered_ApplicationSetsTheFlagsAfterTheAuditRegistration_Starts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuditMarten();
        services.AddMarten(options =>
        {
            options.Connection(Connection);
            options.Projections.Errors.SkipApplyErrors = false;
            options.Projections.Errors.SkipSerializationErrors = false;
            options.Projections.RebuildErrors.SkipApplyErrors = false;
            options.Projections.RebuildErrors.SkipSerializationErrors = false;
        });

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await Should.NotThrowAsync(() => GuardOf(provider).StartingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Registered_ApplicationSetsTheFlagsBeforeTheAuditRegistration_Starts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarten(options =>
        {
            options.Connection(Connection);
            options.Projections.Errors.SkipApplyErrors = false;
            options.Projections.Errors.SkipSerializationErrors = false;
            options.Projections.RebuildErrors.SkipApplyErrors = false;
            options.Projections.RebuildErrors.SkipSerializationErrors = false;
        });
        services.AddEncinaAuditMarten();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await Should.NotThrowAsync(() => GuardOf(provider).StartingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Registered_ApplicationLeavesMartenDefaults_Fails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuditMarten();
        services.AddMarten(options => options.Connection(Connection));

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await Should.ThrowAsync<InvalidOperationException>(() => GuardOf(provider).StartingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Registered_ApplicationReEnablesSkippingAfterTheAuditRegistration_Fails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuditMarten();
        services.AddMarten(options =>
        {
            options.Connection(Connection);
            options.Projections.Errors.SkipApplyErrors = false;
            options.Projections.Errors.SkipSerializationErrors = false;
            options.Projections.RebuildErrors.SkipApplyErrors = false;
            options.Projections.RebuildErrors.SkipSerializationErrors = false;
        });
        services.ConfigureMarten(options => options.Projections.Errors.SkipSerializationErrors = true);

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => GuardOf(provider).StartingAsync(CancellationToken.None));
        ex.Message.ShouldEndWith("Errors.SkipSerializationErrors.");
    }

    [Fact]
    public async Task Registered_OptOutThroughTheOptions_StartsWithMartenDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAuditMarten(o => o.AllowSkippedProjectionErrors = true);
        services.AddMarten(options => options.Connection(Connection));

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await Should.NotThrowAsync(() => GuardOf(provider).StartingAsync(CancellationToken.None));
    }

    private static AuditProjectionErrorPolicyStartupGuard GuardOf(IServiceProvider provider) =>
        provider.GetServices<IHostedService>().OfType<AuditProjectionErrorPolicyStartupGuard>().Single();

    // ---- options ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddEncinaAuditMarten_BlankRetentionPolicyId_FailsAtRegistration(string policyId)
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<OptionsValidationException>(() =>
            services.AddEncinaAuditMarten(o => o.RetentionPolicyId = policyId));

        ex.Failures.ShouldContain(MartenOperationAuditOptionsValidator.RetentionPolicyIdBlankMessage);
    }

    [Fact]
    public void Validator_NullAndNamedPolicyIds_Succeed()
    {
        var validator = new MartenOperationAuditOptionsValidator();

        validator.Validate(null, new MartenOperationAuditOptions()).Succeeded.ShouldBeTrue();
        validator.Validate(null, new MartenOperationAuditOptions { RetentionPolicyId = "policy-7y" }).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validator_BlankPolicyId_Fails() =>
        new MartenOperationAuditOptionsValidator()
            .Validate(null, new MartenOperationAuditOptions { RetentionPolicyId = " " })
            .Failed.ShouldBeTrue();

    [Fact]
    public void Validator_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new MartenOperationAuditOptionsValidator().Validate(null, null!));

    private sealed class CapturingLogger : ILogger<AuditProjectionErrorPolicyStartupGuard>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
