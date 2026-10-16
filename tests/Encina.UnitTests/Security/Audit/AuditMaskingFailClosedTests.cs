#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Audit;
using Encina.Security.PII;
using Encina.UnitTests.Support;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// The audit entry factory never stores raw personal data: a masking failure fails the audit write closed,
/// and running without a masker is impossible unless the application opts out explicitly (#1835,
/// SPEC-002 DEC-006, AGENTS.md section 3).
/// </summary>
public sealed class AuditMaskingFailClosedTests
{
    private const string Sentinel = "SENTINEL-masker-message-with-patient-data";

    private static readonly IRequestContext Context = RequestContext.CreateForTest();

    private static readonly System.Text.Json.JsonSerializerOptions CamelCase =
        new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

    // -- Factory ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Create_MaskerThrows_ReturnsMaskingFailedCarryingOnlyTheExceptionType()
    {
        var factory = CreateFactory(new ThrowingMasker());

        var result = factory.Create(new PatientCommand("Ana"), Context, AuditOutcome.Success, null);

        var error = LeftOf(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.MaskingFailedCode);
        error.Message.ShouldNotContain(Sentinel);
        error.GetDetails().ShouldContainKeyAndValue("cause", nameof(InvalidOperationException));
        error.GetDetails().ShouldContainKeyAndValue("requestType", nameof(PatientCommand));
    }

    [Fact]
    public void Create_UnserializableAuditableRequest_ReturnsMaskingFailed()
    {
        var options = Options.Create(new OperationAuditOptions());
        var factory = new DefaultOperationAuditEntryFactory(new DefaultSensitiveDataRedactor(options), options);
        var request = new AuditedCircularCommand();
        request.Self = request;

        var result = factory.Create(request, Context, AuditOutcome.Success, null);

        LeftOf(result).GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.MaskingFailedCode);
    }

    [Fact]
    public void Create_RequestPayloadWithACustomMasker_MasksThePayloadThroughTheMasker()
    {
        var options = Options.Create(new OperationAuditOptions
        {
            IncludePayloadHash = false,
            IncludeRequestPayload = true
        });
        var factory = new DefaultOperationAuditEntryFactory(new UpperCaseMasker(), options);

        var entry = factory.Create(new PatientCommand("Ana"), Context, AuditOutcome.Success, null).Entry();

        entry.RequestPayload.ShouldNotBeNull();
        entry.RequestPayload.ShouldContain("\"ANA\"", Case.Sensitive);
        entry.RequestPayload.ShouldNotContain("\"Ana\"", Case.Sensitive);
    }

    [Fact]
    public void Create_RequestPayloadWithAThrowingMasker_ReturnsMaskingFailedInsteadOfStoringRawJson()
    {
        var options = Options.Create(new OperationAuditOptions
        {
            IncludePayloadHash = false,
            IncludeRequestPayload = true
        });
        var factory = new DefaultOperationAuditEntryFactory(new ThrowingMasker(), options);

        var result = factory.Create(new PatientCommand("Ana"), Context, AuditOutcome.Success, null);

        LeftOf(result).GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.MaskingFailedCode);
    }

    [Fact]
    public void Create_ResponsePayloadWithAThrowingMasker_ReturnsMaskingFailed()
    {
        var options = Options.Create(new OperationAuditOptions
        {
            IncludePayloadHash = false,
            IncludeResponsePayload = true
        });
        var factory = new DefaultOperationAuditEntryFactory(new ThrowingMasker(), options);
        var now = DateTimeOffset.UtcNow;

        var result = factory.Create(
            new PatientCommand("Ana"), new PatientCommand("Bea"), Context, AuditOutcome.Success, null, now, now);

        LeftOf(result).GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.MaskingFailedCode);
    }

    [Fact]
    public void Create_NonSerializableRequestWithTheDefaultRedactor_FailsClosedInsteadOfHashingTheRawRequest()
    {
        var options = Options.Create(new OperationAuditOptions());
        var factory = new DefaultOperationAuditEntryFactory(new DefaultSensitiveDataRedactor(options), options);
        var request = new CircularCommand();
        request.Self = request;

        var result = factory.Create(request, Context, AuditOutcome.Success, null);

        LeftOf(result).GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.MaskingFailedCode);
    }

    [Fact]
    public void Create_Hash_IsOverTheMaskedJsonNotTheRawRequest()
    {
        var options = Options.Create(new OperationAuditOptions());
        var factory = new DefaultOperationAuditEntryFactory(new DefaultSensitiveDataRedactor(options), options);

        var first = factory.Create(new CredentialsCommand("ana", "secret-one"), Context, AuditOutcome.Success, null).Entry();
        var second = factory.Create(new CredentialsCommand("ana", "secret-two"), Context, AuditOutcome.Success, null).Entry();
        var otherUser = factory.Create(new CredentialsCommand("bea", "secret-one"), Context, AuditOutcome.Success, null).Entry();

        first.RequestPayloadHash.ShouldBe(second.RequestPayloadHash);
        first.RequestPayloadHash.ShouldNotBe(otherUser.RequestPayloadHash);
    }

    [Fact]
    public void Create_HashWithACustomMasker_IsTheSha256OfTheMaskedJson()
    {
        var options = Options.Create(new OperationAuditOptions());
        var factory = new DefaultOperationAuditEntryFactory(new UpperCaseMasker(), options);
        var maskedJson = System.Text.Json.JsonSerializer.Serialize(
            new PatientCommand("ANA"),
            CamelCase);
        var expected = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(maskedJson)))
            .ToLowerInvariant();

        var entry = factory.Create(new PatientCommand("Ana"), Context, AuditOutcome.Success, null).Entry();

        entry.RequestPayloadHash.ShouldBe(expected);
    }

    [Fact]
    public void Create_OnlyTheHashEnabled_StoresNoPayload()
    {
        var options = Options.Create(new OperationAuditOptions { IncludePayloadHash = true });
        var factory = new DefaultOperationAuditEntryFactory(new UpperCaseMasker(), options);

        var entry = factory.Create(new PatientCommand("Ana"), Context, AuditOutcome.Success, null).Entry();

        entry.RequestPayloadHash.ShouldNotBeNull();
        entry.RequestPayload.ShouldBeNull();
        entry.ResponsePayload.ShouldBeNull();
    }

    [Fact]
    public void Create_OnlyTheRequestPayloadEnabled_StoresNoHash()
    {
        var options = Options.Create(new OperationAuditOptions { IncludePayloadHash = false, IncludeRequestPayload = true });
        var factory = new DefaultOperationAuditEntryFactory(new UpperCaseMasker(), options);

        var entry = factory.Create(new PatientCommand("Ana"), Context, AuditOutcome.Success, null).Entry();

        entry.RequestPayloadHash.ShouldBeNull();
        entry.RequestPayload.ShouldNotBeNull();
    }

    [Fact]
    public void Create_ResponsePayloadOnSuccess_IsMaskedByTheRedactor()
    {
        var options = Options.Create(new OperationAuditOptions { IncludePayloadHash = false, IncludeResponsePayload = true });
        var factory = new DefaultOperationAuditEntryFactory(new DefaultSensitiveDataRedactor(options), options);
        var now = DateTimeOffset.UtcNow;

        var entry = factory.Create(
            new PatientCommand("Ana"), new CredentialsCommand("ana", "secret-one"), Context, AuditOutcome.Success, null, now, now).Entry();

        entry.ResponsePayload.ShouldNotBeNull();
        entry.ResponsePayload.ShouldContain("[REDACTED]");
        entry.ResponsePayload.ShouldNotContain("secret-one");
    }

    [Fact]
    public void Create_ResponsePayloadEnabledButNoResponse_StoresNoResponsePayload()
    {
        var options = Options.Create(new OperationAuditOptions { IncludePayloadHash = false, IncludeResponsePayload = true });
        var factory = new DefaultOperationAuditEntryFactory(new UpperCaseMasker(), options);

        var entry = factory.Create(new PatientCommand("Ana"), Context, AuditOutcome.Success, null).Entry();

        entry.ResponsePayload.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_HandlerLeftAndMaskingFailure_FailClosed_KeepsTheHandlersLeft()
    {
        var store = new InMemoryOperationAuditStore();
        var logger = new FakeLogger<AuditPipelineBehavior<PatientCommand, Unit>>();
        var sut = CreateBehavior(new ThrowingMasker(), store, logger, OperationAuditFailureMode.FailClosed);
        var handlerError = EncinaErrors.Create("handler.failed", "handler said no");

        var result = await sut.Handle(
            new PatientCommand("Ana"),
            Context,
            () => new ValueTask<Either<EncinaError, Unit>>(LanguageExt.Prelude.Left<EncinaError, Unit>(handlerError)),
            CancellationToken.None);

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty)).ShouldBe("handler.failed");
        store.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_HandlerThrowsAndMaskingFails_RethrowsTheHandlersException()
    {
        var store = new InMemoryOperationAuditStore();
        var logger = new FakeLogger<AuditPipelineBehavior<PatientCommand, Unit>>();
        var sut = CreateBehavior(new ThrowingMasker(), store, logger, OperationAuditFailureMode.FailClosed);

        var act = () => sut.Handle(
            new PatientCommand("Ana"), Context, () => throw new ArgumentException("handler boom"), CancellationToken.None).AsTask();

        (await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldBe("handler boom");
        store.Count.ShouldBe(0);
    }

    [Fact]
    public void Create_ResponseOverTheSizeLimit_KeepsTheEntryWithoutTheResponsePayload()
    {
        var options = Options.Create(new OperationAuditOptions
        {
            IncludePayloadHash = false,
            IncludeResponsePayload = true,
            MaxPayloadSizeBytes = 8
        });
        var factory = new DefaultOperationAuditEntryFactory(new DefaultSensitiveDataRedactor(options), options);
        var now = DateTimeOffset.UtcNow;

        var entry = factory.Create(
            new PatientCommand("Ana"), new PatientCommand("Bea-with-a-long-name"), Context, AuditOutcome.Success, null, now, now).Entry();

        entry.ResponsePayload.ShouldBeNull();
    }

    // -- Pipeline: Left with an error code, nothing stored --------------------------------------------------------------

    [Fact]
    public async Task Handle_MaskerThrows_FailClosed_ReturnsMaskingFailedAndStoresNothing()
    {
        var store = new InMemoryOperationAuditStore();
        var logger = new FakeLogger<AuditPipelineBehavior<PatientCommand, Unit>>();
        var sut = CreateBehavior(new ThrowingMasker(), store, logger, OperationAuditFailureMode.FailClosed);

        var result = await sut.Handle(new PatientCommand("Ana"), Context, Ok, CancellationToken.None);

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(OperationAuditErrors.MaskingFailedCode);
        result.Match(Right: _ => string.Empty, Left: e => e.Message).ShouldNotContain(Sentinel);
        store.Count.ShouldBe(0);
        var warning = logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        warning.Id.Id.ShouldBe(5000);
        warning.Message.ShouldContain(OperationAuditErrors.MaskingFailedCode);
        RedactedExceptionLogAssert.NeverLoggedRaw(logger, Sentinel);
    }

    [Fact]
    public async Task Handle_MaskerThrows_BestEffort_PassesLogsAndStoresNothing()
    {
        var store = new InMemoryOperationAuditStore();
        var logger = new FakeLogger<AuditPipelineBehavior<PatientCommand, Unit>>();
        var sut = CreateBehavior(new ThrowingMasker(), store, logger, OperationAuditFailureMode.BestEffort);

        var result = await sut.Handle(new PatientCommand("Ana"), Context, Ok, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        store.Count.ShouldBe(0);
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 5000 && r.Level == LogLevel.Warning);
        RedactedExceptionLogAssert.NeverLoggedRaw(logger, Sentinel);
    }

    [Fact]
    public void MaskingFailed_CarriesTheCodeTheRequestTypeAndTheCauseButNoMessage()
    {
        var error = OperationAuditErrors.MaskingFailed("PatientCommand", nameof(InvalidOperationException));

        error.GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.MaskingFailedCode);
        error.Message.ShouldNotContain(Sentinel);
        error.GetDetails().ShouldContainKeyAndValue("cause", nameof(InvalidOperationException));
    }

    // -- Startup validation: a masker is required ----------------------------------------------------------------------

    [Fact]
    public async Task StartingAsync_NoMasker_ThrowsWithTheFixInTheMessage()
    {
        await using var provider = BuildProvider(configure: null, masker: null);

        var act = () => StartValidator(provider);

        var ex = await Should.ThrowAsync<InvalidOperationException>(act);
        ex.Message.ShouldContain(nameof(IPiiMasker));
        ex.Message.ShouldContain(nameof(OperationAuditOptions.AllowUnmaskedPayloads));
    }

    [Fact]
    public async Task StartingAsync_NullPiiMaskerRegisteredWithoutTheOptOut_Throws()
    {
        await using var provider = BuildProvider(configure: null, masker: new NullPiiMasker());

        var act = () => StartValidator(provider);

        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task StartingAsync_OptOutOnlyThroughConfigurationBinding_ThrowsBecauseNoMaskerCanBeBuilt()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAudit();
        services.Configure<OperationAuditOptions>(o => o.AllowUnmaskedPayloads = true);
        await using var provider = services.BuildServiceProvider();

        var act = () => StartValidator(provider);

        (await Should.ThrowAsync<InvalidOperationException>(act)).Message.ShouldContain("configure action");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StartingAsync_AddEncinaPii_SatisfiesTheRequirementInEitherOrder(bool piiFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (piiFirst)
        {
            services.AddEncinaPII();
            services.AddEncinaAudit();
        }
        else
        {
            services.AddEncinaAudit();
            services.AddEncinaPII();
        }

        await using var provider = services.BuildServiceProvider();

        await StartValidator(provider);
        provider.GetRequiredService<IPiiMasker>().GetType().Name.ShouldBe("PIIMasker");
    }

    [Fact]
    public void Create_SensitiveNamedNonStringProperty_HashesAndStoresTheMaskedJsonWithoutFailing()
    {
        var options = Options.Create(new OperationAuditOptions { IncludeRequestPayload = true });
        var factory = new DefaultOperationAuditEntryFactory(new DefaultSensitiveDataRedactor(options), options);

        var entry = factory.Create(new ShipmentCommand(12.5m, Guid.NewGuid()), Context, AuditOutcome.Success, null).Entry();

        entry.RequestPayloadHash.ShouldNotBeNullOrEmpty();
        entry.RequestPayload.ShouldNotBeNull();
        entry.RequestPayload.ShouldContain("[REDACTED]");
        entry.RequestPayload.ShouldNotContain("12.5");
    }

    [Fact]
    public async Task StartingAsync_RealMasker_StartsWithoutAWarning()
    {
        var logger = new FakeLogger<OperationAuditMaskingStartupValidator>();
        await using var provider = BuildProvider(configure: null, masker: new UpperCaseMasker());

        await StartValidator(provider, logger);

        logger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task StartingAsync_ExplicitOptOut_StartsLogsAWarningAndTheEntryIsStoredUnmasked()
    {
        var logger = new FakeLogger<OperationAuditMaskingStartupValidator>();
        await using var provider = BuildProvider(o => o.AllowUnmaskedPayloads = true, masker: null);

        await StartValidator(provider, logger);

        var warning = logger.Collector.GetSnapshot().Single();
        warning.Level.ShouldBe(LogLevel.Warning);
        warning.Id.Id.ShouldBe(5011);

        using var scope = provider.CreateScope();
        var entry = scope.ServiceProvider.GetRequiredService<IOperationAuditEntryFactory>()
            .Create(new PatientCommand("Ana"), Context, AuditOutcome.Success, null).Entry();
        entry.RequestPayloadHash.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task StartingAsync_WiredIntoTheHost_ThroughTheRegisteredHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAudit();
        await using var provider = services.BuildServiceProvider();

        var hosted = provider.GetServices<IHostedService>().OfType<IHostedLifecycleService>().Single();

        var act = () => hosted.StartingAsync(CancellationToken.None);

        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task LifecycleHooksOtherThanStarting_CompleteWithoutWork()
    {
        await using var provider = BuildProvider(configure: null, masker: new UpperCaseMasker());
        var validator = provider.GetServices<IHostedService>().OfType<IHostedLifecycleService>().Single();

        await validator.StartAsync(CancellationToken.None);
        await validator.StartedAsync(CancellationToken.None);
        await validator.StoppingAsync(CancellationToken.None);
        await validator.StopAsync(CancellationToken.None);
        await validator.StoppedAsync(CancellationToken.None);
    }

    // -- Helpers -----------------------------------------------------------------------------------------------------

    private static EncinaError LeftOf(Either<EncinaError, OperationAuditEntry> result) =>
        result.Match(
            Right: _ => throw new InvalidOperationException("expected a Left"),
            Left: error => error);

    private static ServiceProvider BuildProvider(Action<OperationAuditOptions>? configure, IPiiMasker? masker)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (masker is not null)
        {
            services.AddSingleton(masker);
        }

        services.AddEncinaAudit(configure);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = masker is not null || configure is not null });
    }

    private static async Task StartValidator(
        ServiceProvider provider,
        FakeLogger<OperationAuditMaskingStartupValidator>? logger = null)
    {
        var validator = new OperationAuditMaskingStartupValidator(
            provider,
            provider.GetRequiredService<IOptions<OperationAuditOptions>>(),
            logger ?? new FakeLogger<OperationAuditMaskingStartupValidator>());
        await validator.StartingAsync(CancellationToken.None);
    }

    private static DefaultOperationAuditEntryFactory CreateFactory(IPiiMasker masker) =>
        new(masker, Options.Create(new OperationAuditOptions()));

    private static AuditPipelineBehavior<PatientCommand, Unit> CreateBehavior(
        IPiiMasker masker,
        InMemoryOperationAuditStore store,
        FakeLogger<AuditPipelineBehavior<PatientCommand, Unit>> logger,
        OperationAuditFailureMode mode)
    {
        AuditPipelineBehavior<PatientCommand, Unit>.ClearCache();
        var options = new OperationAuditOptions { AuditAllCommands = true, FailureMode = mode };
        var services = new ServiceCollection();
        services.AddSingleton<IOperationAuditStore>(store);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var factory = new DefaultOperationAuditEntryFactory(masker, Options.Create(options));
        return new(scopeFactory, factory, Options.Create(options), logger);
    }

    private static ValueTask<Either<EncinaError, Unit>> Ok() =>
        new(LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default));

    public sealed record PatientCommand(string Name) : ICommand<Unit>;

    public sealed record CredentialsCommand(string UserName, string Password) : ICommand<Unit>;

    // "ShippingCost" contains the sensitive pattern "pin", "ApiKeyId" contains "key": non-string values.
    public sealed record ShipmentCommand(decimal ShippingCost, Guid ApiKeyId) : ICommand<Unit>;

    [Auditable(SensitiveFields = ["name"])]
    public sealed class AuditedCircularCommand : ICommand<Unit>
    {
        public AuditedCircularCommand? Self { get; set; }
    }

    public sealed class CircularCommand : ICommand<Unit>
    {
        public CircularCommand? Self { get; set; }
    }

    private sealed class ThrowingMasker : IPiiMasker
    {
        public T MaskForAudit<T>(T request) where T : notnull => throw new InvalidOperationException(Sentinel);

        public object MaskForAudit(object request) => throw new InvalidOperationException(Sentinel);
    }

    private sealed class UpperCaseMasker : IPiiMasker
    {
        public T MaskForAudit<T>(T request) where T : notnull => request;

        public object MaskForAudit(object request) =>
            request is PatientCommand command ? command with { Name = command.Name.ToUpperInvariant() } : request;
    }
}
