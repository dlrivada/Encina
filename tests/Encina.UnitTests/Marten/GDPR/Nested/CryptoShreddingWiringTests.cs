#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern
using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Health;

using LanguageExt;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten.GDPR.Nested;

/// <summary>
/// Registration completeness (AGENTS §3), erasure routing, the health check, the graph walker and the locator.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShreddingWiringTests
{
    private const string Subject = "subject-w";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddEncinaMartenGdpr_BuildsWithValidateOnBuildAndValidateScopes(bool postgreSqlKeyStore)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IDocumentSession>());
        services.AddSingleton(Substitute.For<IDocumentStore>());
        services.AddEncinaMartenGdpr(o => o.UsePostgreSqlKeyStore = postgreSqlKeyStore);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISubjectKeyProvider>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IDataErasureStrategy>().ShouldBeOfType<CryptoShredRoutingErasureStrategy>();
        scope.ServiceProvider.GetRequiredService<IPersonalDataLocator>().ShouldBeOfType<MartenEventPersonalDataLocator>();
    }

    [Fact]
    public void AddHealthCheck_RegistersTheCryptoShreddingCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaMartenGdpr(o => o.AddHealthCheck = true);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .ShouldContain(r => r.Name == CryptoShreddingHealthCheck.DefaultName);
    }

    [Fact]
    public void Activities_AreRecordedPerCallWithTheOutcome()
    {
        var stopped = new List<System.Diagnostics.Activity>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Marten.GDPR",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add,
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        using var harness = new CryptoHarness();

        var json = harness.Serializer.ToJson(new NestedEvent { Contact = new ContactInfo { SubjectId = Subject, Email = "a" } });
        harness.FromJson<NestedEvent>(json);
        Should.Throw<CryptoShreddingEncryptionException>(() => harness.Serializer.ToJson(new NestedEvent { Contact = new ContactInfo { Email = "a" } }));

        stopped.ShouldContain(a => a.OperationName == "CryptoShredding.Encrypt" && a.Status == System.Diagnostics.ActivityStatusCode.Ok);
        stopped.ShouldContain(a => a.OperationName == "CryptoShredding.Decrypt" && a.Status == System.Diagnostics.ActivityStatusCode.Ok);
        stopped.ShouldContain(a => a.Status == System.Diagnostics.ActivityStatusCode.Error
            && (string?)a.GetTagItem("crypto.failure_reason") == nameof(CryptoShreddingEncryptionFailureReason.SubjectIdMissing));
        stopped.ShouldAllBe(a => a.Tags.All(t => t.Value == null || !t.Value.Contains(Subject)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DataSubjectRightsBeforeOrAfter_TheRouterIsResolved(bool dsrFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IDocumentSession>());
        if (dsrFirst)
        {
            services.AddEncinaDataSubjectRights();
        }

        services.AddEncinaMartenGdpr();
        services.AddEncinaMartenGdpr();
        if (!dsrFirst)
        {
            services.AddEncinaDataSubjectRights();
        }

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var router = scope.ServiceProvider.GetRequiredService<IDataErasureStrategy>().ShouldBeOfType<CryptoShredRoutingErasureStrategy>();
        services.Count(d => d.ServiceType == typeof(IDataErasureStrategy) && !d.IsKeyedService).ShouldBe(1);
        if (dsrFirst)
        {
            router.Inner.ShouldBeOfType<HardDeleteErasureStrategy>();
        }
    }

    [Fact]
    public async Task Router_MartenLocationsReachCryptoShredding_OthersReachTheInnerStrategy()
    {
        var keys = Substitute.For<ISubjectKeyProvider>();
        keys.DeleteSubjectKeysAsync(Subject, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, CryptoShreddingResult>(new CryptoShreddingResult { SubjectId = Subject, KeysDeleted = 1, FieldsAffected = 0, ShreddedAtUtc = DateTimeOffset.UnixEpoch }));
        var inner = new ApplicationErasureStrategy();
        var router = new CryptoShredRoutingErasureStrategy(
            new CryptoShredErasureStrategy(keys, NullLogger<CryptoShredErasureStrategy>.Instance), [inner]);
        var marten = Location();
        CryptoShredRoutingErasureStrategy.MarkMartenLocation(marten);
        var other = Location();

        (await router.EraseFieldAsync(marten)).IsRight.ShouldBeTrue();
        (await router.EraseFieldAsync(other)).IsRight.ShouldBeTrue();

        await keys.Received(1).DeleteSubjectKeysAsync(Subject, Arg.Any<CancellationToken>());
        inner.Received.ShouldBe([other]);
    }

    [Fact]
    public async Task Router_WithoutInnerStrategy_FailsForNonMartenLocations()
    {
        var router = new CryptoShredRoutingErasureStrategy(
            new CryptoShredErasureStrategy(Substitute.For<ISubjectKeyProvider>(), NullLogger<CryptoShredErasureStrategy>.Instance), []);

        var result = await router.EraseFieldAsync(Location());

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(CryptoShreddingErrors.ErasureStrategyMissingCode));
    }

    [Fact]
    public async Task HealthCheck_Healthy_ReportsCountsAndProvider()
    {
        using var host = new StartupValidationHost(typeof(CryptoShredderSerializer).Assembly);
        var check = new CryptoShreddingHealthCheck(host.Provider, NullLogger<CryptoShreddingHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["keyProviderType"].ShouldBe(nameof(InMemorySubjectKeyProvider));
        result.Data.ShouldContainKey("cryptoContractCount");
        result.Data.ShouldContainKey("misconfiguredTypeCount");
    }

    [Fact]
    public async Task HealthCheck_WrongResolverIdentity_IsUnhealthy()
    {
        using var host = new StartupValidationHost(
            typeof(CryptoShredderSerializer).Assembly,
            configureStore: o => ((CryptoShredderSerializer)o.Serializer()).Inner.UseTypeInfoResolver(DefaultModeContext.Default));
        var check = new CryptoShreddingHealthCheck(host.Provider, NullLogger<CryptoShreddingHealthCheck>.Instance);

        (await check.CheckHealthAsync(new HealthCheckContext())).Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task HealthCheck_SerializerNotWrapped_IsUnhealthy()
    {
        using var host = new StartupValidationHost(typeof(CryptoShredderSerializer).Assembly, configureMarten: false);
        var check = new CryptoShreddingHealthCheck(host.Provider, NullLogger<CryptoShreddingHealthCheck>.Instance);

        (await check.CheckHealthAsync(new HealthCheckContext())).Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task HealthCheck_ResolutionThrows_ReportsOnlyTheExceptionType()
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IDocumentStore)).Returns(_ => throw new InvalidOperationException("secret detail"));
        var check = new CryptoShreddingHealthCheck(provider, NullLogger<CryptoShreddingHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldNotContain("secret detail");
    }

    [Fact]
    public void Walker_ReportsPathsWithoutIndexesOrKeys_AndSurvivesCycles()
    {
        using var harness = new CryptoHarness();
        var json = harness.Serializer.ToJson(new CollectionsEvent { List = [new() { SubjectId = Subject, Email = "a" }] });
        harness.FromJson<CollectionsEvent>(json);
        harness.Serializer.ToJson(new DictionaryEvent());
        harness.Serializer.ToJson(new NestedEvent());
        var root = new CollectionsEvent { List = [new() { SubjectId = Subject, Email = "a" }] };
        var shared = new ContactInfo { SubjectId = Subject, Email = "x" };
        var dictionary = new DictionaryEvent { Notes = new Dictionary<string, ContactInfo> { ["secret-key"] = shared } };

        var listPaths = CryptoShreddedGraphWalker.Walk(root, harness.Serializer.WalkOptions!, harness.Serializer.Registry).Select(o => o.Path);
        var dictionaryPaths = CryptoShreddedGraphWalker.Walk(dictionary, harness.Serializer.WalkOptions!, harness.Serializer.Registry).Select(o => o.Path);
        var nestedPaths = CryptoShreddedGraphWalker.Walk(new NestedEvent { Contact = shared }, harness.Serializer.WalkOptions!, harness.Serializer.Registry).Select(o => o.Path);

        listPaths.ShouldBe(["List[].Email"]);
        dictionaryPaths.ShouldContain("Notes{}.Email");
        dictionaryPaths.ShouldAllBe(p => !p.Contains("secret-key"));
        nestedPaths.ShouldBe(["Contact.Email"]);
    }

    [Fact]
    public void Locator_FieldsInEvent_MatchesEachOwnersOwnSubject()
    {
        using var harness = new CryptoHarness();
        harness.Serializer.ToJson(new TwoSubjectEvent());
        var body = new TwoSubjectEvent
        {
            PatientId = Subject, PatientEmail = "p@example.com", Therapist = new ContactInfo { SubjectId = "therapist", Email = "t@example.com" },
        };

        var patient = MartenEventPersonalDataLocator.LocateFieldsInEvent(body, Subject, harness.Serializer.WalkOptions!, harness.Serializer.Registry).ToList();
        var therapist = MartenEventPersonalDataLocator.LocateFieldsInEvent(body, "therapist", harness.Serializer.WalkOptions!, harness.Serializer.Registry).ToList();

        patient.Single().FieldName.ShouldBe(nameof(TwoSubjectEvent.PatientEmail));
        patient.Single().EntityType.ShouldBe(typeof(TwoSubjectEvent));
        patient.Single().EntityId.ShouldBe(Subject);
        therapist.Single().FieldName.ShouldBe("Therapist.Email");
        therapist.Single().CurrentValue.ShouldBe("t@example.com");
        CryptoShredRoutingErasureStrategy.IsMartenLocation(therapist.Single()).ShouldBeTrue();
    }

    [Fact]
    public async Task Locator_StoreSerializerNotWrapped_FailsClosed()
    {
        var session = Substitute.For<IDocumentSession>();
        session.DocumentStore.Options.Serializer().Returns(Substitute.For<ISerializer>());
        var locator = new MartenEventPersonalDataLocator(session, NullLogger<MartenEventPersonalDataLocator>.Instance);

        var result = await locator.LocateAllDataAsync(Subject);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(CryptoShreddingErrors.SerializerUnsupportedCode));
    }

    [Fact]
    public async Task Locator_QueryFailure_ReturnsKeyStoreErrorAndLogs8483()
    {
        using var harness = new CryptoHarness();
        var session = Substitute.For<IDocumentSession>();
        session.DocumentStore.Options.Serializer().Returns(harness.Serializer);
        session.Events.Returns(_ => throw new InvalidOperationException("boom"));
        var logger = new Microsoft.Extensions.Logging.Testing.FakeLogger<MartenEventPersonalDataLocator>();
        var locator = new MartenEventPersonalDataLocator(session, logger);

        var result = await locator.LocateAllDataAsync(Subject);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode));
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8483);
        logger.Collector.GetSnapshot().ShouldAllBe(r => !r.Message.Contains(Subject));
    }

    private static PersonalDataLocation Location() => new()
    {
        EntityType = typeof(TopLevelOwner),
        EntityId = Subject,
        FieldName = "Email",
        Category = PersonalDataCategory.Contact,
        IsErasable = true,
        IsPortable = true,
        HasLegalRetention = false,
    };
}
