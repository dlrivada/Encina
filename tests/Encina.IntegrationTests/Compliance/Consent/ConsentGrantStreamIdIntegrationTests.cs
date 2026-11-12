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
/// <c>MartenAggregateRepository</c> matches exception type names, so only a real store proves it).
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
    public async Task GrantConsentAsync_SequentialRepeat_ReturnsExistingIdAndKeepsOneStream()
    {
        // Arrange
        var subjectId = NewSubjectId();
        var service = CreateService(tenantId: null);

        // Act
        var first = await service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var second = await service.GrantConsentAsync(subjectId, Purpose, "v2", "api", "actor");

        // Assert
        var expectedId = ConsentStreamId.For(null, subjectId, Purpose);
        first.ShouldBe(Right<EncinaError, Guid>(expectedId));
        second.ShouldBe(Right<EncinaError, Guid>(expectedId));
        (await CountGrantedEventsAsync(subjectId)).ShouldBe(1);
    }

    [Fact]
    public async Task GrantConsentAsync_ConcurrentGrantsOfOneKey_CreateExactlyOneStream()
    {
        // Arrange — each grant runs in its own session, as concurrent requests would.
        const int concurrentGrants = 8;
        var subjectId = NewSubjectId();
        var services = Enumerable.Range(0, concurrentGrants)
            .Select(_ => CreateService(tenantId: "tenant-a"))
            .ToList();

        // Act
        var results = await Task.WhenAll(services.Select(service =>
            service.GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor").AsTask()));

        // Assert — every grant succeeds with the same id, and the store holds one stream.
        var expectedId = ConsentStreamId.For("tenant-a", subjectId, Purpose);
        results.ShouldAllBe(result => result == Right<EncinaError, Guid>(expectedId));
        (await CountGrantedEventsAsync(subjectId)).ShouldBe(1);

        await using var session = _fixture.Store!.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(expectedId);
        stream.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GrantConsentAsync_RepeatThenWithdraw_WithdrawsTheOneRecord()
    {
        // Arrange — two grants of one key, then a withdrawal in a later request (its own session).
        var subjectId = NewSubjectId();
        var granted = await CreateService(tenantId: null).GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var repeated = await CreateService(tenantId: null).GrantConsentAsync(subjectId, Purpose, "v1", "web-form", "actor");
        var consentId = granted.Match(Right: id => id, Left: _ => Guid.Empty);

        // Act
        var withdrawn = await CreateService(tenantId: null).WithdrawConsentAsync(consentId, "actor");

        // Assert
        repeated.ShouldBe(granted);
        withdrawn.IsRight.ShouldBeTrue($"Withdrawal after a repeated grant should succeed: {withdrawn}");
        (await CountGrantedEventsAsync(subjectId)).ShouldBe(1);
        var reloaded = await CreateRepository().LoadAsync(consentId);
        reloaded.IfRight(aggregate => aggregate.Status.ShouldBe(ConsentStatus.Withdrawn));
        reloaded.IsRight.ShouldBeTrue();
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
