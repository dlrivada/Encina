using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.BreakTheGlass;

/// <summary>
/// Guard tests of the break-the-glass package: every public constructor and method with parameters
/// rejects <c>null</c> (or a blank string) with the parameter's name.
/// </summary>
public sealed class BreakTheGlassGuardTests
{
    private static readonly IOptions<BreakTheGlassOptions> Opts = Options.Create(new BreakTheGlassOptions());
    private static readonly BreakTheGlassElevation Elevation = new()
    {
        ElevationId = Guid.NewGuid(),
        UserId = "u",
        SubjectId = "s",
        Scope = "sc",
        Justification = "j",
        OpenedAtUtc = DateTimeOffset.UnixEpoch,
        ExpiresAtUtc = DateTimeOffset.UnixEpoch.AddHours(1)
    };

    private static DefaultBreakTheGlassService Service() => new(
        new InMemoryBreakTheGlassStore(),
        new InMemoryOperationAuditStore(),
        Substitute.For<IRequestContextAccessor>(),
        new RoleBreakTheGlassAuthorizer(Opts),
        new BreakTheGlassRequestValidator(Opts),
        Substitute.For<IEncina>(),
        Opts,
        TimeProvider.System);

    private static void Param(Func<object?> act, string name) => Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(name);

    private static async Task ParamAsync(Func<Task> act, string name) =>
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe(name);

    // ── Constructors ────────────────────────────────────────────────────

    [Fact]
    public void RoleAuthorizer_NullOptions() => Param(() => new RoleBreakTheGlassAuthorizer(null!), "options");

    [Fact]
    public void Validator_NullOptions() => Param(() => new BreakTheGlassRequestValidator(null!), "options");

    [Fact]
    public void Service_Constructor_RejectsEachNullDependency()
    {
        var store = new InMemoryBreakTheGlassStore();
        var audit = new InMemoryOperationAuditStore();
        var accessor = Substitute.For<IRequestContextAccessor>();
        var authorizer = new RoleBreakTheGlassAuthorizer(Opts);
        var validator = new BreakTheGlassRequestValidator(Opts);
        var encina = Substitute.For<IEncina>();

        Param(() => new DefaultBreakTheGlassService(null!, audit, accessor, authorizer, validator, encina, Opts, TimeProvider.System), "store");
        Param(() => new DefaultBreakTheGlassService(store, null!, accessor, authorizer, validator, encina, Opts, TimeProvider.System), "auditStore");
        Param(() => new DefaultBreakTheGlassService(store, audit, null!, authorizer, validator, encina, Opts, TimeProvider.System), "contextAccessor");
        Param(() => new DefaultBreakTheGlassService(store, audit, accessor, null!, validator, encina, Opts, TimeProvider.System), "authorizer");
        Param(() => new DefaultBreakTheGlassService(store, audit, accessor, authorizer, null!, encina, Opts, TimeProvider.System), "validator");
        Param(() => new DefaultBreakTheGlassService(store, audit, accessor, authorizer, validator, null!, Opts, TimeProvider.System), "encina");
        Param(() => new DefaultBreakTheGlassService(store, audit, accessor, authorizer, validator, encina, null!, TimeProvider.System), "options");
        Param(() => new DefaultBreakTheGlassService(store, audit, accessor, authorizer, validator, encina, Opts, null!), "timeProvider");
    }

    [Fact]
    public void Gate_Constructor_RejectsEachNullDependency()
    {
        var store = new InMemoryBreakTheGlassStore();
        var audit = new InMemoryOperationAuditStore();
        var readAudit = new ReadAuditContext();
        var grants = new BreakTheGlassRequestGrants();

        Param(() => new BreakTheGlassAccessGate(null!, audit, readAudit, grants, Opts, TimeProvider.System), "store");
        Param(() => new BreakTheGlassAccessGate(store, null!, readAudit, grants, Opts, TimeProvider.System), "auditStore");
        Param(() => new BreakTheGlassAccessGate(store, audit, null!, grants, Opts, TimeProvider.System), "readAuditContext");
        Param(() => new BreakTheGlassAccessGate(store, audit, readAudit, null!, Opts, TimeProvider.System), "grants");
        Param(() => new BreakTheGlassAccessGate(store, audit, readAudit, grants, null!, TimeProvider.System), "options");
        Param(() => new BreakTheGlassAccessGate(store, audit, readAudit, grants, Opts, null!), "timeProvider");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Attribute_BlankScope(string? scope) => Should.Throw<ArgumentException>(() => new BreakTheGlassEligibleAttribute(scope!));

    // ── Methods ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RoleAuthorizer_Methods()
    {
        var sut = new RoleBreakTheGlassAuthorizer(Opts);

        await ParamAsync(async () => await sut.AuthorizeOpenAsync(null!, "s"), "identity");
        await ParamAsync(async () => await sut.AuthorizeOpenAsync(TestIdentity.Anonymous, null!), "scope");
        await ParamAsync(async () => await sut.AuthorizeReviewAsync(null!, Elevation), "identity");
        await ParamAsync(async () => await sut.AuthorizeReviewAsync(TestIdentity.Anonymous, null!), "elevation");
    }

    [Fact]
    public void Validator_NullRequest() => Param(() => new BreakTheGlassRequestValidator(Opts).Validate(null!), "request");

    [Fact]
    public async Task Service_Methods()
    {
        var sut = Service();

        await ParamAsync(async () => await sut.OpenAsync((BreakTheGlassRequest)null!, CancellationToken.None), "request");
        await ParamAsync(async () => await sut.RevokeAsync(Guid.NewGuid(), null!, CancellationToken.None), "reason");
        await ParamAsync(async () => await sut.GetReviewQueueAsync(null!, CancellationToken.None), "query");
    }

    [Fact]
    public async Task Gate_Methods()
    {
        var gate = new BreakTheGlassAccessGate(new InMemoryBreakTheGlassStore(), new InMemoryOperationAuditStore(), new ReadAuditContext(), new BreakTheGlassRequestGrants(), Opts, TimeProvider.System);
        var context = Substitute.For<IRequestContext>();

        await ParamAsync(async () => await gate.TryElevateAsync<string>(null!, context, EncinaErrors.Forbidden(), CancellationToken.None), "request");
        await ParamAsync(async () => await gate.TryElevateAsync("r", null!, EncinaErrors.Forbidden(), CancellationToken.None), "context");
    }

    [Fact]
    public async Task InMemoryStore_Methods()
    {
        var sut = new InMemoryBreakTheGlassStore();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UnixEpoch;

        await ParamAsync(async () => await sut.AddAsync(null!), "elevation");
        await ParamAsync(async () => await sut.FindEffectiveAsync("t", null!, "s", "sc", now), "userId");
        await ParamAsync(async () => await sut.FindEffectiveAsync("t", "u", null!, "sc", now), "subjectId");
        await ParamAsync(async () => await sut.FindEffectiveAsync("t", "u", "s", null!, now), "scope");
        await ParamAsync(async () => await sut.TryCloseAsync("t", id, BreakTheGlassStatus.Revoked, null!, now), "reason");
        await ParamAsync(async () => await sut.TryRecordReviewAsync("t", id, BreakTheGlassReviewOutcome.Justified, null!, null, now), "reviewerUserId");
        await ParamAsync(async () => await sut.QueryReviewQueueAsync("t", null!), "query");
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await sut.GetExpiredUnrecordedAsync(now, 0));
    }

    [Fact]
    public void Options_DeclareScope()
    {
        var options = new BreakTheGlassOptions();

        Should.Throw<ArgumentException>(() => options.DeclareScope(" ", _ => { }));
        Param(() => options.DeclareScope("a", null!), "configure");
    }

    [Fact]
    public void ReadAuditContext_WithMetadata()
    {
        var context = new ReadAuditContext();

        Should.Throw<ArgumentException>(() => context.WithMetadata(" ", "v"));
        Param(() => context.WithMetadata("k", null!), "value");
    }
}
