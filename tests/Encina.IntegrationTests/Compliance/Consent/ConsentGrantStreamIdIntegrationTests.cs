using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.Events;
using Encina.Compliance.Consent.ReadModels;
using Encina.Compliance.Consent.Services;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Encina.Marten.Projections;
using Marten;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Compliance.Consent;

/// <summary>
/// Integration tests proving, against a real PostgreSQL/Marten event store, that
/// <see cref="DefaultConsentService.GrantConsentAsync"/> keeps at most one consent stream per
/// (tenant, data subject, purpose) through the deterministic stream id (#3769), including under
/// concurrent grants, and that Marten's stream collision is reported as
/// <see cref="MartenErrorCodes.StreamAlreadyExists"/> (the detection in
/// <c>MartenAggregateRepository</c> matches exception type names, so only a real store proves it),
/// and that a grant on an existing stream follows the rules of #3771 (renewal, regrant, act
/// ordering and concurrent losers continuing on the existing stream).
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class ConsentGrantStreamIdIntegrationTests : IAsyncLifetime
{
    private const string Purpose = "marketing";

    private readonly MartenFixture _fixture;
    private readonly List<IDocumentSession> _sessions = [];

    public ConsentGrantStreamIdIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var session in _sessions)
        {
            await session.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateAsync_ExistingStreamId_ReturnsStreamAlreadyExists()
    {
        // Arrange
        var subjectId = NewSubjectId();
        var id = ConsentStreamId.For(null, subjectId, Purpose);
        var firstResult = await CreateRepository().CreateAsync(NewAggregate(id, subjectId));

        // Act — a second stream with the same id, from another session.
        var secondResult = await CreateRepository().CreateAsync(NewAggregate(id, subjectId));

        // Assert
        firstResult.IsRight.ShouldBeTrue();
        secondResult.IsLeft.ShouldBeTrue();
        secondResult.IfLeft(error => error.GetCode().ShouldBe(Some(MartenErrorCodes.StreamAlreadyExists)));
    }

    [Fact]
    public async Task GrantConsentAsync_SequentialRepeat_RecordsARenewalOnTheOneStream()
    {
        // Arrange
        var subjectId = NewSubjectId();
        var service = CreateService(tenantId: null);

        // Act
        var first = await service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var second = await service.GrantConsentAsync(subjectId, Purpose, "v2", "api", "actor", proofOfConsent: "hash-2");

        // Assert — D4: the repeated grant is a renewal carrying its own proof (#3771).
        var expectedId = ConsentStreamId.For(null, subjectId, Purpose);
        first.ShouldBe(Right<EncinaError, Guid>(expectedId));
        second.ShouldBe(Right<EncinaError, Guid>(expectedId));
        (await CountGrantedEventsAsync(subjectId)).ShouldBe(1);
        var events = await FetchEventsAsync(expectedId);
        events.Select(e => e.GetType()).ShouldBe([typeof(ConsentGranted), typeof(ConsentRenewedByGrant)]);
        ((ConsentRenewedByGrant)events[1]).ProofOfConsent.ShouldBe("hash-2");
    }

    [Fact]
    public async Task GrantConsentAsync_ConcurrentGrantsOfOneKey_OneStreamWithEveryAct()
    {
        // Arrange — each grant runs in its own session, as concurrent requests would. Five is the
        // number of concurrent writes on one key the service records without error (#3771).
        const int concurrentGrants = 5;
        var subjectId = NewSubjectId();
        var services = Enumerable.Range(0, concurrentGrants)
            .Select(_ => CreateService(tenantId: "tenant-a"))
            .ToList();

        // Act
        var results = await Task.WhenAll(services.Select(service =>
            service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor").AsTask()));

        // Assert — every grant succeeds with the same id; one stream holds the grant and a
        // renewal per concurrent loser (D3: a loser continues as a grant on the existing stream).
        var expectedId = ConsentStreamId.For("tenant-a", subjectId, Purpose);
        results.ShouldAllBe(result => result == Right<EncinaError, Guid>(expectedId));
        (await CountGrantedEventsAsync(subjectId)).ShouldBe(1);
        var events = await FetchEventsAsync(expectedId);
        events.Count.ShouldBe(concurrentGrants);
        events.OfType<ConsentRenewedByGrant>().Count().ShouldBe(concurrentGrants - 1);
    }

    [Fact]
    public async Task GrantConsentAsync_RepeatThenWithdrawInTheSameScope_WithdrawsTheOneRecord()
    {
        // Arrange — two grants of one key, then the withdrawal; the one-session test below spells out the single-session case.
        var subjectId = NewSubjectId();
        var service = CreateService(tenantId: null);
        var granted = await service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var repeated = await service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var consentId = granted.Match(Right: id => id, Left: _ => Guid.Empty);

        // Act
        var withdrawn = await service.WithdrawConsentAsync(consentId, "actor");

        // Assert
        repeated.ShouldBe(granted);
        withdrawn.IsRight.ShouldBeTrue($"Withdrawal after a repeated grant should succeed: {withdrawn}");
        (await CountGrantedEventsAsync(subjectId)).ShouldBe(1);
        var reloaded = await CreateRepository().LoadAsync(consentId);
        reloaded.IfRight(aggregate => aggregate.Status.ShouldBe(ConsentStatus.Withdrawn));
        reloaded.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GrantConsentAsync_RepeatThenWithdraw_OnOneSession_WithdrawsTheOneRecord()
    {
        // Arrange — one service, hence one scoped session, for every step (no fresh session in between).
        var subjectId = NewSubjectId();
        var service = CreateService(tenantId: null);

        // Act
        var granted = await service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var repeated = await service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var consentId = granted.Match(Right: id => id, Left: _ => Guid.Empty);
        var withdrawn = await service.WithdrawConsentAsync(consentId, "actor");

        // Assert
        repeated.ShouldBe(granted);
        withdrawn.IsRight.ShouldBeTrue($"Withdrawal on the same session should succeed: {withdrawn}");
        (await CountGrantedEventsAsync(subjectId)).ShouldBe(1);
        var reloaded = await CreateRepository().LoadAsync(consentId);
        reloaded.IsRight.ShouldBeTrue();
        reloaded.IfRight(aggregate => aggregate.Status.ShouldBe(ConsentStatus.Withdrawn));
    }

    [Fact]
    public async Task GrantConsentAsync_AfterWithdrawal_RegrantsAndTheKeyIsValidAgain()
    {
        // Arrange — inline projections on, so the read side follows the stream.
        await using var provider = BuildProjectedServiceProvider();
        var subjectId = NewSubjectId();
        using var scope = provider.CreateScope();
        var service = CreateProjectedService(scope);
        var consentId = (await service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor"))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));
        (await service.WithdrawConsentAsync(consentId, "actor")).IsRight.ShouldBeTrue();
        (await service.HasValidConsentAsync(subjectId, Purpose)).ShouldBe(Right<EncinaError, bool>(false));

        // Act
        var regranted = await service.GrantConsentAsync(subjectId, Purpose, "v2", "paper", "reception", proofOfConsent: "hash-3");

        // Assert — D5: an explicit regrant on the same stream makes the key valid again.
        regranted.ShouldBe(Right<EncinaError, Guid>(consentId));
        (await service.HasValidConsentAsync(subjectId, Purpose)).ShouldBe(Right<EncinaError, bool>(true));
        var events = await FetchEventsAsync(consentId);
        events.Select(e => e.GetType()).ShouldBe([typeof(ConsentGranted), typeof(ConsentWithdrawn), typeof(ConsentRegranted)]);
        var readModel = (await service.GetConsentAsync(consentId)).Match(m => m, e => throw new InvalidOperationException(e.Message));
        readModel.Status.ShouldBe(ConsentStatus.Active);
        readModel.ConsentVersionId.ShouldBe("v2");
        readModel.ProofOfConsent.ShouldBe("hash-3");
        readModel.WithdrawnAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task GrantConsentAsync_LateActAfterWithdrawal_IsRecordedButTheKeyStaysWithdrawn()
    {
        // Arrange
        await using var provider = BuildProjectedServiceProvider();
        var subjectId = NewSubjectId();
        using var scope = provider.CreateScope();
        var service = CreateProjectedService(scope);
        var consentId = (await service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor"))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));
        (await service.WithdrawConsentAsync(consentId, "actor")).IsRight.ShouldBeTrue();

        // Act — a grant the subject made an hour before the withdrawal, delivered only now.
        var late = await service.GrantConsentAsync(
            subjectId, Purpose, "v1", "web-form", "actor", occurredAtUtc: DateTimeOffset.UtcNow.AddHours(-1));

        // Assert
        late.ShouldBe(Right<EncinaError, Guid>(consentId));
        (await FetchEventsAsync(consentId)).OfType<ConsentRegranted>().ShouldHaveSingleItem();
        (await service.HasValidConsentAsync(subjectId, Purpose)).ShouldBe(Right<EncinaError, bool>(false));
    }

    [Fact]
    public async Task GrantConsentAsync_ConcurrentRegrants_BothRecordedAndTheConsentIsActive()
    {
        // Arrange — a withdrawn key, then two regrants from two concurrent requests.
        var subjectId = NewSubjectId();
        var consentId = (await CreateService(tenantId: null).GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor"))
            .Match(Right: id => id, Left: e => throw new InvalidOperationException(e.Message));
        (await CreateService(tenantId: null).WithdrawConsentAsync(consentId, "actor")).IsRight.ShouldBeTrue();
        var services = new[] { CreateService(tenantId: null), CreateService(tenantId: null) };

        // Act
        var results = await Task.WhenAll(services.Select(service =>
            service.GrantConsentAsync(subjectId, Purpose, "v2", "web-form", "actor").AsTask()));

        // Assert — the winner regrants; the loser of the optimistic race reloads the now Active
        // stream and records a renewal (D3). Neither reports an error.
        results.ShouldAllBe(result => result == Right<EncinaError, Guid>(consentId));
        var events = await FetchEventsAsync(consentId);
        events.Count.ShouldBe(4);
        events.OfType<ConsentRegranted>().ShouldHaveSingleItem();
        events.OfType<ConsentRenewedByGrant>().ShouldHaveSingleItem();
        var reloaded = (await CreateRepository().LoadAsync(consentId)).Match(a => a, e => throw new InvalidOperationException(e.Message));
        reloaded.Status.ShouldBe(ConsentStatus.Active);
    }

    [Fact]
    public async Task GrantConsentAsync_SameSubjectAndPurposeInTwoTenants_CreatesTwoStreams()
    {
        // Arrange
        var subjectId = NewSubjectId();

        // Act
        var inTenantA = await CreateService(tenantId: "tenant-a").GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var inTenantB = await CreateService(tenantId: "tenant-b").GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");

        // Assert
        inTenantA.ShouldBe(Right<EncinaError, Guid>(ConsentStreamId.For("tenant-a", subjectId, Purpose)));
        inTenantB.ShouldBe(Right<EncinaError, Guid>(ConsentStreamId.For("tenant-b", subjectId, Purpose)));
        (await CountGrantedEventsAsync(subjectId)).ShouldBe(2);
    }

    private static string NewSubjectId() => $"subject-{Guid.NewGuid():N}";

    private static ConsentAggregate NewAggregate(Guid id, string subjectId) =>
        ConsentAggregate.Grant(
            id, subjectId, Purpose, "v1", "web-form", null, null,
            new Dictionary<string, object?>(), null, "actor", DateTimeOffset.UtcNow);

    private async Task<List<object>> FetchEventsAsync(Guid consentId)
    {
        await using var session = _fixture.Store!.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(consentId);
        return stream.OrderBy(e => e.Version).Select(e => e.Data).ToList();
    }

    /// <summary>
    /// A provider with the Marten aggregate repository and the inline consent projection, so a
    /// grant updates the read model that <c>HasValidConsentAsync</c> queries.
    /// </summary>
    private ServiceProvider BuildProjectedServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddScoped(_ => Substitute.For<IRequestContext>());
        services.AddEncinaMarten(_ => { });
        services.AddConsentAggregates();
        return services.BuildServiceProvider();
    }

    private static DefaultConsentService CreateProjectedService(IServiceScope scope)
    {
        var requestContextAccessor = Substitute.For<IRequestContextAccessor>();
        requestContextAccessor.RequestContext.Returns((IRequestContext?)null);

        return new DefaultConsentService(
            scope.ServiceProvider.GetRequiredService<IAggregateRepository<ConsentAggregate>>(),
            scope.ServiceProvider.GetRequiredService<IReadModelRepository<ConsentReadModel>>(),
            Substitute.For<ICacheProvider>(),
            TimeProvider.System,
            requestContextAccessor,
            Options.Create(new ConsentOptions()),
            NullLogger<DefaultConsentService>.Instance);
    }

    private async Task<int> CountGrantedEventsAsync(string subjectId)
    {
        await using var session = _fixture.Store!.LightweightSession();
        return await session.Events.QueryRawEventDataOnly<ConsentGranted>()
            .Where(e => e.DataSubjectId == subjectId)
            .CountAsync();
    }

    private MartenAggregateRepository<ConsentAggregate> CreateRepository()
    {
        var session = _fixture.Store!.LightweightSession();
        _sessions.Add(session);
        return CreateRepository(session);
    }

    private static MartenAggregateRepository<ConsentAggregate> CreateRepository(IDocumentSession session)
    {
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        return new MartenAggregateRepository<ConsentAggregate>(
            session,
            requestContext,
            NullLogger<MartenAggregateRepository<ConsentAggregate>>.Instance,
            Options.Create(new EncinaMartenOptions()));
    }

    private DefaultConsentService CreateService(string? tenantId)
    {
        var session = _fixture.Store!.LightweightSession();
        _sessions.Add(session);

        var requestContextAccessor = Substitute.For<IRequestContextAccessor>();
        requestContextAccessor.RequestContext.Returns(
            tenantId is null ? null : RequestContext.CreateForTest(tenantId: tenantId));

        return new DefaultConsentService(
            CreateRepository(session),
            new MartenReadModelRepository<ConsentReadModel>(
                session, NullLogger<MartenReadModelRepository<ConsentReadModel>>.Instance),
            Substitute.For<ICacheProvider>(),
            TimeProvider.System,
            requestContextAccessor,
            Options.Create(new ConsentOptions()),
            NullLogger<DefaultConsentService>.Instance);
    }
}
