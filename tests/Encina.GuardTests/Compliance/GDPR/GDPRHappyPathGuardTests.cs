#pragma warning disable CA2012 // NSubstitute stubs ValueTask-returning members through Returns
#pragma warning disable CA1859 // Tests use the interface type the package exposes

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Diagnostics;
using Encina.Compliance.GDPR.Export;
using Encina.Compliance.GDPR.Health;

using LanguageExt;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using LawfulBasisValue = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.GuardTests.Compliance.GDPR;

/// <summary>
/// Guard tests for Encina.Compliance.GDPR that reject invalid arguments and also run the guarded
/// member with valid arguments, so the guard flag measures the body of the package code.
/// </summary>
public class GDPRHappyPathGuardTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 2, 17, 10, 0, 0, TimeSpan.Zero);

    private static ProcessingActivity CreateActivity(
        Type requestType,
        string name = "Order Processing",
        string? thirdCountry = null,
        string? safeguards = null) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Purpose = "Fulfil orders",
            LawfulBasis = LawfulBasisValue.Contract,
            CategoriesOfDataSubjects = ["Customers"],
            CategoriesOfPersonalData = ["Name", "Email"],
            Recipients = ["Shipping"],
            ThirdCountryTransfers = thirdCountry,
            Safeguards = safeguards,
            RetentionPeriod = TimeSpan.FromDays(365),
            SecurityMeasures = "Encryption",
            RequestType = requestType,
            CreatedAtUtc = FixedTime,
            LastUpdatedAtUtc = FixedTime
        };

    [ProcessingActivity(
        Purpose = "Guard test",
        LawfulBasis = LawfulBasisValue.Consent,
        DataCategories = ["Email"],
        DataSubjects = ["Customers"],
        RetentionDays = 30,
        Recipients = ["Mailer"],
        SecurityMeasures = "TLS",
        ThirdCountryTransfers = "US",
        Safeguards = "SCC")]
    private sealed record AnnotatedCommand : ICommand<Unit>;

    private sealed record PlainCommand : ICommand<Unit>;

    // -- Exporters --

    [Fact]
    public async Task CsvExporter_ExportAsync_ValidArguments_WritesMetadataHeaderAndEscapedRows()
    {
        var sut = new CsvRoPAExporter();
        var activities = new[]
        {
            CreateActivity(typeof(AnnotatedCommand), name: "Orders, \"quoted\"", thirdCountry: "US", safeguards: "SCC"),
            CreateActivity(typeof(PlainCommand))
        };
        var metadata = new RoPAExportMetadata(
            "Acme", "privacy@acme.test", FixedTime, new DataProtectionOfficer("Dana", "dpo@acme.test"));

        var result = await sut.ExportAsync(activities, metadata, TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        var export = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        export.ActivityCount.ShouldBe(2);
        sut.ContentType.ShouldBe("text/csv");
        var text = Encoding.UTF8.GetString(export.Content);
        text.ShouldContain("# DPO: Dana (dpo@acme.test)");
        text.ShouldContain("\"Orders, \"\"quoted\"\"\"");
        text.ShouldContain("Id,Name,Purpose");
    }

    [Fact]
    public async Task CsvExporter_ExportAsync_WithoutDpo_OmitsDpoLine()
    {
        var sut = new CsvRoPAExporter();
        var metadata = new RoPAExportMetadata("Acme", "privacy@acme.test", FixedTime);

        var result = await sut.ExportAsync([], metadata, TestContext.Current.CancellationToken);

        var export = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        Encoding.UTF8.GetString(export.Content).ShouldNotContain("# DPO:");
        sut.FileExtension.ShouldBe(".csv");
    }

    [Fact]
    public async Task JsonExporter_ExportAsync_ValidArguments_WritesDocumentWithDpoAndActivities()
    {
        var sut = new JsonRoPAExporter();
        var activities = new[] { CreateActivity(typeof(AnnotatedCommand), thirdCountry: "US", safeguards: "SCC") };
        var metadata = new RoPAExportMetadata(
            "Acme", "privacy@acme.test", FixedTime, new DataProtectionOfficer("Dana", "dpo@acme.test", "123"));

        var result = await sut.ExportAsync(activities, metadata, TestContext.Current.CancellationToken);

        var export = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        using var doc = JsonDocument.Parse(export.Content);
        doc.RootElement.GetProperty("metadata").GetProperty("activityCount").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("metadata").GetProperty("dataProtectionOfficer").GetProperty("phone").GetString().ShouldBe("123");
        doc.RootElement.GetProperty("activities")[0].GetProperty("lawfulBasis").GetString().ShouldBe("contract");
        sut.ContentType.ShouldBe("application/json");
        sut.FileExtension.ShouldBe(".json");
    }

    [Fact]
    public async Task JsonExporter_ExportAsync_WithoutDpo_OmitsDpoSection()
    {
        var sut = new JsonRoPAExporter();
        var metadata = new RoPAExportMetadata("Acme", "privacy@acme.test", FixedTime);

        var result = await sut.ExportAsync([CreateActivity(typeof(PlainCommand))], metadata, TestContext.Current.CancellationToken);

        var export = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        using var doc = JsonDocument.Parse(export.Content);
        doc.RootElement.GetProperty("metadata").TryGetProperty("dataProtectionOfficer", out _).ShouldBeFalse();
    }

    // -- Mapper --

    [Fact]
    public void Mapper_ToEntityThenToDomain_RoundTripsActivity()
    {
        var activity = CreateActivity(typeof(AnnotatedCommand), thirdCountry: "US", safeguards: "SCC");

        var entity = ProcessingActivityMapper.ToEntity(activity);
        var back = ProcessingActivityMapper.ToDomain(entity);

        back.ShouldNotBeNull();
        back.Id.ShouldBe(activity.Id);
        back.RequestType.ShouldBe(typeof(AnnotatedCommand));
        back.CategoriesOfPersonalData.ShouldBe(activity.CategoriesOfPersonalData);
        back.RetentionPeriod.ShouldBe(activity.RetentionPeriod);
        back.Safeguards.ShouldBe("SCC");
    }

    [Fact]
    public void Mapper_ToDomain_UnresolvableRequestType_ReturnsNull()
    {
        var entity = ProcessingActivityMapper.ToEntity(CreateActivity(typeof(PlainCommand)));
        entity.RequestTypeName = "No.Such.Type, NoSuchAssembly";

        ProcessingActivityMapper.ToDomain(entity).ShouldBeNull();
    }

    // -- Registry --

    [Fact]
    public async Task Registry_RegisterGetUpdateAndDuplicate_BehaveAsDocumented()
    {
        var sut = new InMemoryProcessingActivityRegistry();
        var activity = CreateActivity(typeof(PlainCommand));

        (await sut.RegisterActivityAsync(activity, TestContext.Current.CancellationToken)).IsRight.ShouldBeTrue();
        (await sut.RegisterActivityAsync(activity, TestContext.Current.CancellationToken)).IsLeft.ShouldBeTrue();
        (await sut.UpdateActivityAsync(activity with { Name = "Renamed" }, TestContext.Current.CancellationToken)).IsRight.ShouldBeTrue();
        (await sut.UpdateActivityAsync(CreateActivity(typeof(AnnotatedCommand)), TestContext.Current.CancellationToken)).IsLeft.ShouldBeTrue();

        var found = (Option<ProcessingActivity>)await sut.GetActivityByRequestTypeAsync(typeof(PlainCommand), TestContext.Current.CancellationToken);
        found.Match(Some: a => a.Name, None: () => "").ShouldBe("Renamed");
    }

    [Fact]
    public async Task Registry_AutoRegisterFromAssemblies_RegistersAnnotatedTypesWithInjectedClock()
    {
        var sut = new InMemoryProcessingActivityRegistry();
        var clock = new FakeTimeProvider(FixedTime);

        var count = sut.AutoRegisterFromAssemblies([typeof(GDPRHappyPathGuardTests).Assembly], clock);

        count.ShouldBeGreaterThanOrEqualTo(1);
        var all = (await sut.GetAllActivitiesAsync(TestContext.Current.CancellationToken))
            .Match(Right: a => a, Left: _ => throw new InvalidOperationException());
        all.Single(a => a.RequestType == typeof(AnnotatedCommand)).CreatedAtUtc.ShouldBe(FixedTime);
    }

    // -- Hosted service --

    [Fact]
    public async Task HostedService_StartAsync_InMemoryRegistry_AutoRegistersAndStopCompletes()
    {
        var registry = new InMemoryProcessingActivityRegistry();
        var descriptor = new GDPRAutoRegistrationDescriptor([typeof(GDPRHappyPathGuardTests).Assembly]);
        IHostedService sut = new GDPRAutoRegistrationHostedService(
            registry, descriptor, new FakeTimeProvider(FixedTime), NullLogger<GDPRAutoRegistrationHostedService>.Instance);

        await sut.StartAsync(TestContext.Current.CancellationToken);
        await sut.StopAsync(TestContext.Current.CancellationToken);

        var found = (Option<ProcessingActivity>)await registry.GetActivityByRequestTypeAsync(typeof(AnnotatedCommand), TestContext.Current.CancellationToken);
        found.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task HostedService_StartAsync_NonInMemoryRegistry_SkipsAutoRegistration()
    {
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var sut = new GDPRAutoRegistrationHostedService(
            registry, new GDPRAutoRegistrationDescriptor([]), TimeProvider.System, NullLogger<GDPRAutoRegistrationHostedService>.Instance);

        await sut.StartAsync(TestContext.Current.CancellationToken);

        await registry.DidNotReceiveWithAnyArgs().RegisterActivityAsync(default!, default);
    }

    // -- Service registration --

    [Fact]
    public async Task AddEncinaGDPR_WithHealthCheckAndAutoRegistration_BuildsValidatedProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaGDPR(o =>
        {
            o.ControllerName = "Acme";
            o.ControllerEmail = "privacy@acme.test";
            o.AddHealthCheck = true;
            o.AssembliesToScan.Add(typeof(GDPRHappyPathGuardTests).Assembly);
        });

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        provider.GetRequiredService<IOptions<GDPROptions>>().Value.ControllerName.ShouldBe("Acme");
        provider.GetServices<IHostedService>().Any(s => s is GDPRAutoRegistrationHostedService).ShouldBeTrue();
        provider.GetRequiredService<JsonRoPAExporter>().ShouldNotBeNull();
        provider.GetRequiredService<CsvRoPAExporter>().ShouldNotBeNull();
        var report = await provider.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>()
            .CheckHealthAsync(TestContext.Current.CancellationToken);
        report.Entries.ContainsKey(GDPRHealthCheck.DefaultName).ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaGDPR_EnforceModeWithoutController_FailsOptionsValidation()
    {
        var services = new ServiceCollection();
        services.AddEncinaGDPR();
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<GDPROptions>>().Value;

        Should.Throw<OptionsValidationException>(act);
    }

    [Fact]
    public void AddEncinaGDPR_AutoRegistrationDisabled_RegistersNoHostedService()
    {
        var services = new ServiceCollection();
        services.AddEncinaGDPR(o => o.AutoRegisterFromAttributes = false);

        services.Any(d => d.ServiceType == typeof(IHostedService)).ShouldBeFalse();
    }

    // -- Health checks --

    private static async Task<HealthCheckResult> RunGdprCheck(IServiceProvider provider) =>
        await new GDPRHealthCheck(provider, NullLogger<GDPRHealthCheck>.Instance)
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task GDPRHealthCheck_FullyConfiguredWithActivities_IsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaGDPR(o =>
        {
            o.ControllerName = "Acme";
            o.ControllerEmail = "privacy@acme.test";
            o.DataProtectionOfficer = new DataProtectionOfficer("Dana", "dpo@acme.test");
            o.AutoRegisterFromAttributes = false;
        });
        using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IProcessingActivityRegistry>()
            .RegisterActivityAsync(CreateActivity(typeof(PlainCommand)), TestContext.Current.CancellationToken);

        var result = await RunGdprCheck(provider);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["dpoConfigured"].ShouldBe(true);
    }

    [Fact]
    public async Task GDPRHealthCheck_EmptyRegistryAndMissingController_IsDegraded()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaGDPR(o =>
        {
            o.EnforcementMode = GDPREnforcementMode.WarnOnly;
            o.AutoRegisterFromAttributes = false;
        });
        using var provider = services.BuildServiceProvider();

        var result = await RunGdprCheck(provider);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("ControllerName");
        result.Description!.ShouldContain("No processing activities");
    }

    [Fact]
    public async Task GDPRHealthCheck_OptionsNotRegistered_IsUnhealthy()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();

        (await RunGdprCheck(provider)).Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task GDPRHealthCheck_RegistryNotRegistered_IsUnhealthy()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new GDPROptions()));
        using var provider = services.BuildServiceProvider();

        (await RunGdprCheck(provider)).Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task GDPRHealthCheck_RegistryReturnsLeftAndNoValidator_IsDegradedWithErrorCodeOnly()
    {
        var registry = Substitute.For<IProcessingActivityRegistry>();
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ProcessingActivity>>>(EncinaError.New("secret detail")));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(new GDPROptions { ControllerName = "A", ControllerEmail = "b@c.d" }));
        services.AddSingleton(registry);
        using var provider = services.BuildServiceProvider();

        var result = await RunGdprCheck(provider);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldNotContain("secret detail");
        result.Description!.ShouldContain("IGDPRComplianceValidator");
    }

    [Fact]
    public async Task GDPRHealthCheck_ScopeCreationThrows_IsUnhealthyWithExceptionTypeOnly()
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("sensitive"));

        var result = await RunGdprCheck(provider);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain(nameof(InvalidOperationException));
        result.Description!.ShouldNotContain("sensitive");
    }

    private static async Task<HealthCheckResult> RunActivityCheck(IServiceProvider provider) =>
        await new ProcessingActivityHealthCheck(provider, NullLogger<ProcessingActivityHealthCheck>.Instance)
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task ProcessingActivityHealthCheck_PopulatedRegistry_IsHealthy()
    {
        var registry = new InMemoryProcessingActivityRegistry();
        await registry.RegisterActivityAsync(CreateActivity(typeof(PlainCommand)), TestContext.Current.CancellationToken);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IProcessingActivityRegistry>(registry);
        using var provider = services.BuildServiceProvider();

        var result = await RunActivityCheck(provider);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["activities_count"].ShouldBe(1);
    }

    [Fact]
    public async Task ProcessingActivityHealthCheck_EmptyRegistry_IsDegraded()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IProcessingActivityRegistry, InMemoryProcessingActivityRegistry>();
        using var provider = services.BuildServiceProvider();

        (await RunActivityCheck(provider)).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task ProcessingActivityHealthCheck_RegistryNotRegistered_IsUnhealthy()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();

        (await RunActivityCheck(provider)).Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task ProcessingActivityHealthCheck_RegistryReturnsLeft_IsDegraded()
    {
        var registry = Substitute.For<IProcessingActivityRegistry>();
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ProcessingActivity>>>(EncinaError.New("boom")));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(registry);
        using var provider = services.BuildServiceProvider();

        (await RunActivityCheck(provider)).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task ProcessingActivityHealthCheck_RegistryThrows_IsDegradedWithExceptionTypeOnly()
    {
        var registry = Substitute.For<IProcessingActivityRegistry>();
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<ProcessingActivity>>>>(_ => throw new TimeoutException("hidden"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(registry);
        using var provider = services.BuildServiceProvider();

        var result = await RunActivityCheck(provider);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain(nameof(TimeoutException));
        result.Description!.ShouldNotContain("hidden");
    }

    // -- Pipeline behavior --

    private static GDPRCompliancePipelineBehavior<TRequest, Unit> CreateBehavior<TRequest>(
        IProcessingActivityRegistry registry,
        IGDPRComplianceValidator validator,
        GDPROptions? options = null)
        where TRequest : IRequest<Unit> =>
        new(registry, validator, Options.Create(options ?? new GDPROptions()),
            NullLogger<GDPRCompliancePipelineBehavior<TRequest, Unit>>.Instance);

    private static RequestHandlerCallback<Unit> Next(Action onCalled) => () =>
    {
        onCalled();
        return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    };

    [Fact]
    public async Task PipelineBehavior_Handle_RequestWithoutGdprAttributes_SkipsToNext()
    {
        var called = false;
        var sut = CreateBehavior<PlainCommand>(new InMemoryProcessingActivityRegistry(), new DefaultGDPRComplianceValidator());

        var result = await sut.Handle(new PlainCommand(), RequestContext.CreateForTest(), Next(() => called = true), TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task PipelineBehavior_Handle_RegisteredCompliantActivity_PassesAndTraces()
    {
        var stopped = new System.Collections.Concurrent.ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == GDPRDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = stopped.Add
        };
        ActivitySource.AddActivityListener(listener);
        var registry = new InMemoryProcessingActivityRegistry();
        registry.AutoRegisterFromAssemblies([typeof(GDPRHappyPathGuardTests).Assembly]);
        var called = false;
        var sut = CreateBehavior<AnnotatedCommand>(registry, new DefaultGDPRComplianceValidator());

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), Next(() => called = true), TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
        stopped
            .Where(a => Equals(a.GetTagItem(GDPRDiagnostics.TagRequestType), nameof(AnnotatedCommand)))
            .ShouldContain(a => Equals(a.GetTagItem(GDPRDiagnostics.TagOutcome), "passed"));
    }

    [Fact]
    public async Task PipelineBehavior_Handle_UnregisteredAndBlocking_ReturnsLeftWithoutNext()
    {
        var called = false;
        var sut = CreateBehavior<AnnotatedCommand>(
            new InMemoryProcessingActivityRegistry(),
            new DefaultGDPRComplianceValidator(),
            new GDPROptions { BlockUnregisteredProcessing = true });

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), Next(() => called = true), TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task PipelineBehavior_Handle_RegistryLookupFails_ReturnsLeft()
    {
        var registry = Substitute.For<IProcessingActivityRegistry>();
        registry.GetActivityByRequestTypeAsync(Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Option<ProcessingActivity>>>(EncinaError.New("down")));
        var sut = CreateBehavior<AnnotatedCommand>(registry, new DefaultGDPRComplianceValidator());

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), Next(() => { }), TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task PipelineBehavior_Handle_ValidatorReturnsLeft_ReturnsLeft()
    {
        var validator = Substitute.For<IGDPRComplianceValidator>();
        validator.ValidateAsync(Arg.Any<AnnotatedCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(EncinaError.New("validator down")));
        var sut = CreateBehavior<AnnotatedCommand>(new InMemoryProcessingActivityRegistry(), validator);

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), Next(() => { }), TestContext.Current.CancellationToken);

        result.IsLeft.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GDPREnforcementMode.Enforce, false)]
    [InlineData(GDPREnforcementMode.WarnOnly, true)]
    public async Task PipelineBehavior_Handle_NonCompliant_FollowsEnforcementMode(GDPREnforcementMode mode, bool expectNext)
    {
        var called = false;
        var validator = Substitute.For<IGDPRComplianceValidator>();
        validator.ValidateAsync(Arg.Any<AnnotatedCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(ComplianceResult.NonCompliant("bad")));
        var sut = CreateBehavior<AnnotatedCommand>(
            new InMemoryProcessingActivityRegistry(), validator, new GDPROptions { EnforcementMode = mode });

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), Next(() => called = true), TestContext.Current.CancellationToken);

        called.ShouldBe(expectNext);
        result.IsRight.ShouldBe(expectNext);
    }

    [Fact]
    public async Task PipelineBehavior_Handle_CompliantWithWarnings_ProceedsToNext()
    {
        var called = false;
        var validator = Substitute.For<IGDPRComplianceValidator>();
        validator.ValidateAsync(Arg.Any<AnnotatedCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(ComplianceResult.CompliantWithWarnings("heads up")));
        var sut = CreateBehavior<AnnotatedCommand>(new InMemoryProcessingActivityRegistry(), validator);

        var result = await sut.Handle(new AnnotatedCommand(), RequestContext.CreateForTest(), Next(() => called = true), TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    // -- Attributes, model and diagnostics --

    [Fact]
    public void Attributes_ConstructedWithValues_ExposeThem()
    {
        var basis = new LawfulBasisAttribute(LawfulBasisValue.LegalObligation)
        {
            Purpose = "p",
            LIAReference = "lia",
            LegalReference = "law",
            ContractReference = "c"
        };
        var processing = typeof(AnnotatedCommand).GetCustomAttribute<ProcessingActivityAttribute>();

        basis.Basis.ShouldBe(LawfulBasisValue.LegalObligation);
        basis.LegalReference.ShouldBe("law");
        processing.ShouldNotBeNull();
        processing.DataCategories.ShouldBe(["Email"]);
        processing.Safeguards.ShouldBe("SCC");
        new ProcessesPersonalDataAttribute().ShouldNotBeNull();
    }

    private static ActivityListener StartListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Encina.Compliance.GDPR", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public void GDPRDiagnostics_ListenerAttached_RecordsOutcomesAsTags()
    {
        using var listener = StartListener();

        using (var check = GDPRDiagnostics.StartComplianceCheck("Req"))
        {
            check.ShouldNotBeNull();
            GDPRDiagnostics.SetLawfulBasis(check, LawfulBasisValue.Consent);
            GDPRDiagnostics.RecordPassed(check);
            check.GetTagItem(GDPRDiagnostics.TagOutcome).ShouldBe("passed");
            GDPRDiagnostics.RecordFailed(check, "why");
            GDPRDiagnostics.RecordSkipped(check);
            check.GetTagItem(GDPRDiagnostics.TagLawfulBasis).ShouldBe("Consent");
        }

        using var export = GDPRDiagnostics.StartRoPAExport("csv", 3);
        export.ShouldNotBeNull();
        GDPRDiagnostics.RecordExportCompleted(export);
        GDPRDiagnostics.RecordExportFailed(export, "why");
        export.GetTagItem(GDPRDiagnostics.TagFailureReason).ShouldBe("why");
    }

    [Fact]
    public void GDPRDiagnostics_NullActivity_RecordersAreNullSafe()
    {
        Should.NotThrow(() =>
        {
            GDPRDiagnostics.RecordPassed(null);
            GDPRDiagnostics.RecordFailed(null, "x");
            GDPRDiagnostics.RecordSkipped(null);
            GDPRDiagnostics.RecordExportCompleted(null);
            GDPRDiagnostics.RecordExportFailed(null, "x");
            GDPRDiagnostics.SetLawfulBasis(null, LawfulBasisValue.Contract);
        });
    }

    [Fact]
    public void LawfulBasisDiagnostics_ListenerAttached_RecordsValidationOutcome()
    {
        using var listener = StartListener();

        using var valid = LawfulBasisDiagnostics.StartValidation(typeof(PlainCommand));
        valid.ShouldNotBeNull();
        LawfulBasisDiagnostics.SetBasis(valid, LawfulBasisValue.Contract);
        LawfulBasisDiagnostics.CompleteValidation(valid, true);
        valid.GetTagItem(LawfulBasisDiagnostics.TagLawfulBasisValid).ShouldBe(true);

        using var invalid = LawfulBasisDiagnostics.StartValidation(typeof(PlainCommand));
        invalid.ShouldNotBeNull();
        LawfulBasisDiagnostics.CompleteValidation(invalid, false);
        invalid.GetTagItem(LawfulBasisDiagnostics.TagFailureReason).ShouldBe("unknown");
        LawfulBasisDiagnostics.CompleteValidation(null, false);
    }

    [Fact]
    public void ProcessingActivityDiagnostics_ListenerAttached_StartsAndRecordsEveryOperation()
    {
        using var listener = StartListener();

        using (var a = ProcessingActivityDiagnostics.StartRegistration(typeof(PlainCommand))) a.ShouldNotBeNull();
        using (var b = ProcessingActivityDiagnostics.StartUpdate(typeof(PlainCommand))) b.ShouldNotBeNull();
        using (var c = ProcessingActivityDiagnostics.StartGetByRequestType(typeof(PlainCommand))) c.ShouldNotBeNull();
        using var all = ProcessingActivityDiagnostics.StartGetAll();
        all.ShouldNotBeNull();

        ProcessingActivityDiagnostics.RecordSuccess(all);
        ProcessingActivityDiagnostics.RecordSuccess(all, 4, "get_all");
        all.GetTagItem(ProcessingActivityDiagnostics.TagActivityCount).ShouldBe(4);
        ProcessingActivityDiagnostics.RecordFailure(all);
        all.GetTagItem(ProcessingActivityDiagnostics.TagFailureReason).ShouldBe("unknown");
        ProcessingActivityDiagnostics.RecordFailure(all, "update", "not_found");
        all.GetTagItem(ProcessingActivityDiagnostics.TagFailureReason).ShouldBe("not_found");
    }

    [Fact]
    public void ProcessingActivityDiagnostics_NullActivity_RecordersAreNullSafe()
    {
        Should.NotThrow(() =>
        {
            ProcessingActivityDiagnostics.RecordSuccess(null);
            ProcessingActivityDiagnostics.RecordSuccess(null, 2, "get_all");
            ProcessingActivityDiagnostics.RecordFailure(null);
        });
    }

    [Fact]
    public void LawfulBasisDiagnostics_NullActivity_RecordersAreNullSafe()
    {
        Should.NotThrow(() =>
        {
            LawfulBasisDiagnostics.SetBasis(null, LawfulBasisValue.Consent);
            LawfulBasisDiagnostics.CompleteValidation(null, true);
        });
    }

    [Fact]
    public void ModelTypes_ComplianceResultAndEntity_ExposeConstructedValues()
    {
        var result = ComplianceResult.NonCompliant(["e"], ["w"]);
        var entity = ProcessingActivityMapper.ToEntity(CreateActivity(typeof(PlainCommand)));

        result.IsCompliant.ShouldBeFalse();
        result.Warnings.ShouldBe(["w"]);
        entity.Name.ShouldBe("Order Processing");
        new RoPAExportResult([1], "text/csv", ".csv", 1, FixedTime).ActivityCount.ShouldBe(1);
    }
}
