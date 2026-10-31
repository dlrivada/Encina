#pragma warning disable CA2012 // NSubstitute stubbing of ValueTask members

using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>
/// A blank or whitespace tenant id is "no tenant": the service and the gate normalise it to <see langword="null"/>
/// before any store call or audit write, so it behaves exactly like <see langword="null"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassBlankTenantTests
{
    private const string Subject = "patient-secret-77";

    private static readonly DateTimeOffset Now = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    private static BreakTheGlassRequest OpenRequest() =>
        new(Subject, BreakTheGlassTestKit.Scope, BreakTheGlassTestKit.Justification, TimeSpan.FromHours(1));

    private static DefaultBreakTheGlassService Service(IBreakTheGlassStore store, InMemoryOperationAuditStore audit, RequestIdentity identity, string? tenant)
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<BreakTheGlassOpenedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, Unit>(unit)));
        encina.Publish(Arg.Any<BreakTheGlassReviewedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, Unit>(unit)));
        var options = BreakTheGlassTestKit.Options();
        return new DefaultBreakTheGlassService(
            store,
            audit,
            BreakTheGlassTestKit.Accessor(BreakTheGlassTestKit.Context(identity, tenant)),
            new RoleBreakTheGlassAuthorizer(BreakTheGlassTestKit.Wrap(options)),
            new BreakTheGlassRequestValidator(BreakTheGlassTestKit.Wrap(options)),
            encina,
            BreakTheGlassTestKit.Wrap(options),
            new FakeTimeProvider(Now));
    }

    private static T Right<T>(Either<EncinaError, T> result) =>
        result.Match(Right: value => value, Left: error => throw new InvalidOperationException("unexpected Left " + error.GetCode().IfNone("?")));

    private static async Task<(InMemoryBreakTheGlassStore Store, BreakTheGlassElevation Row)> UntenantedRowAsync()
    {
        var store = new InMemoryBreakTheGlassStore();
        var row = BreakTheGlassTestKit.Elevation(Now, tenantId: null, subjectId: Subject);
        Right(await store.AddAsync(row));
        return (store, row);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t ")]
    public async Task Open_BlankTenant_StoresAndAuditsNoTenant(string blank)
    {
        var store = new InMemoryBreakTheGlassStore();
        var audit = new InMemoryOperationAuditStore();

        var opened = Right(await Service(store, audit, BreakTheGlassTestKit.Clinician(), blank).OpenAsync(OpenRequest(), CancellationToken.None));

        opened.TenantId.ShouldBeNull();
        Right(await store.GetByIdAsync(null, opened.ElevationId)).IsSome.ShouldBeTrue();
        Right(await store.GetByIdAsync(blank, opened.ElevationId)).IsNone.ShouldBeTrue();
        audit.GetAllEntries().ShouldNotBeEmpty();
        audit.GetAllEntries().ShouldAllBe(entry => entry.TenantId == null);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Open_BlankTenant_ConflictsWithAnUntenantedActiveElevation(string blank)
    {
        var store = new InMemoryBreakTheGlassStore();
        var audit = new InMemoryOperationAuditStore();
        Right(await Service(store, audit, BreakTheGlassTestKit.Clinician(), null).OpenAsync(OpenRequest(), CancellationToken.None));

        var second = await Service(store, audit, BreakTheGlassTestKit.Clinician(), blank).OpenAsync(OpenRequest(), CancellationToken.None);

        second.Match(Right: _ => string.Empty, Left: error => error.GetCode().IfNone(string.Empty)).ShouldBe(BreakTheGlassErrors.AlreadyActiveCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Get_BlankTenant_ReadsTheUntenantedRow(string blank)
    {
        var (store, row) = await UntenantedRowAsync();

        var found = Right(await Service(store, new InMemoryOperationAuditStore(), BreakTheGlassTestKit.Reviewer(), blank).GetAsync(row.ElevationId, CancellationToken.None));

        found.ElevationId.ShouldBe(row.ElevationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Revoke_BlankTenant_ClosesTheUntenantedRowAndAuditsNoTenant(string blank)
    {
        var (store, row) = await UntenantedRowAsync();
        var audit = new InMemoryOperationAuditStore();

        Right(await Service(store, audit, BreakTheGlassTestKit.Clinician(), blank).RevokeAsync(row.ElevationId, "no longer needed", CancellationToken.None));

        Right(await store.GetByIdAsync(null, row.ElevationId)).Match(found => found.Status, () => throw new InvalidOperationException()).ShouldBe(BreakTheGlassStatus.Revoked);
        audit.GetAllEntries().ShouldNotBeEmpty();
        audit.GetAllEntries().ShouldAllBe(entry => entry.TenantId == null);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Review_BlankTenant_ReviewsTheUntenantedRowAndAuditsNoTenant(string blank)
    {
        var (store, row) = await UntenantedRowAsync();
        var audit = new InMemoryOperationAuditStore();

        Right(await Service(store, audit, BreakTheGlassTestKit.Reviewer(), blank).ReviewAsync(row.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None));

        Right(await store.GetByIdAsync(null, row.ElevationId)).Match(found => found.ReviewedAtUtc, () => throw new InvalidOperationException()).ShouldNotBeNull();
        audit.GetAllEntries().ShouldNotBeEmpty();
        audit.GetAllEntries().ShouldAllBe(entry => entry.TenantId == null);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetReviewQueue_BlankTenant_QueriesTheStoreWithNull(string blank)
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        store.QueryReviewQueueAsync(Arg.Any<string?>(), Arg.Any<BreakTheGlassReviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, PagedResult<BreakTheGlassElevation>>(PagedResult<BreakTheGlassElevation>.Empty())));

        var result = await Service(store, new InMemoryOperationAuditStore(), BreakTheGlassTestKit.Reviewer(), blank).GetReviewQueueAsync(new BreakTheGlassReviewQuery(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await store.Received(1).QueryReviewQueueAsync(Arg.Is<string?>(tenant => tenant == null), Arg.Any<BreakTheGlassReviewQuery>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Normalise_BlankIsNull_AnythingElseIsKept(string blank)
    {
        BreakTheGlassTenant.Normalise(blank).ShouldBeNull();
        BreakTheGlassTenant.Normalise(null).ShouldBeNull();
        BreakTheGlassTenant.Normalise("tenant-1").ShouldBe("tenant-1");
        BreakTheGlassTenant.Normalise(" tenant-1 ").ShouldBe(" tenant-1 ");
    }

    // ── the gate ────────────────────────────────────────────────────────

    private static BreakTheGlassAccessGate Gate(IBreakTheGlassStore store, InMemoryOperationAuditStore audit) =>
        new(store, audit, new ReadAuditContext(), new BreakTheGlassRequestGrants(), BreakTheGlassTestKit.Wrap(BreakTheGlassTestKit.Options()), new FakeTimeProvider(Now),
            new Microsoft.Extensions.Logging.Testing.FakeLogger<BreakTheGlassAccessGate>());

    private static ValueTask<Either<EncinaError, Option<AccessElevation>>> Try(BreakTheGlassAccessGate gate, string? tenant) =>
        gate.TryElevateAsync(
            new EligibleGateRequest(Subject),
            BreakTheGlassTestKit.Context(BreakTheGlassTestKit.Clinician(), tenant),
            EncinaErrors.Forbidden(),
            CancellationToken.None);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Gate_BlankTenant_FindsTheUntenantedElevationAndAuditsNoTenant(string blank)
    {
        var store = new InMemoryBreakTheGlassStore();
        Right(await store.AddAsync(BreakTheGlassTestKit.Elevation(Now, tenantId: null, subjectId: Subject)));
        var audit = new InMemoryOperationAuditStore();

        var result = Right(await Try(Gate(store, audit), blank));

        result.IsSome.ShouldBeTrue();
        audit.GetAllEntries().ShouldNotBeEmpty();
        audit.GetAllEntries().ShouldAllBe(entry => entry.TenantId == null);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Gate_BlankTenant_DoesNotSeeATenantedElevation(string blank)
    {
        var store = new InMemoryBreakTheGlassStore();
        Right(await store.AddAsync(BreakTheGlassTestKit.Elevation(Now, tenantId: "tenant-1", subjectId: Subject)));

        var result = Right(await Try(Gate(store, new InMemoryOperationAuditStore()), blank));

        result.IsNone.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Gate_BlankTenant_AsksTheStoreWithNull(string blank)
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        store.FindEffectiveAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, Option<BreakTheGlassElevation>>(Option<BreakTheGlassElevation>.None)));

        Right(await Try(Gate(store, new InMemoryOperationAuditStore()), blank)).IsNone.ShouldBeTrue();

        await store.Received(1).FindEffectiveAsync(Arg.Is<string?>(tenant => tenant == null), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
