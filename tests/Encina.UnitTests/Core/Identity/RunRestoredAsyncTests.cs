using Encina.Testing;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// <see cref="RequestContextScopeFactory.RunRestoredAsync{T}"/>: validation (every failure is
/// <c>invalid_persisted_identity</c> without invoking work), Internal and External sources (D3, E2),
/// no authority from the row, and logs without tenants or actors.
/// </summary>
public sealed class RunRestoredAsyncTests
{
    private const string Sentinel = "sentinel-restore-9b2";

    private readonly ScopeTestHost _host = new();

    private Task<Either<EncinaError, IRequestContext>> Restore(
        PersistedRequestIdentity persisted, PersistedIdentitySource source, string? configuredTenantId = null) =>
        _host.Factory.RunRestoredAsync(persisted, source, ScopeTestHost.Capture, configuredTenantId);

    private static PersistedRequestIdentity User(string actor = "alice", string? tenant = "t-row") =>
        new(IdentityKind.User, actor, tenant, "corr-1", "cause-1");

    private static void ShouldBeInvalid(Either<EncinaError, IRequestContext> result)
    {
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.InvalidPersistedIdentity));
    }

    [Fact]
    public async Task AnInternalUser_IsRestored_WithNoRolesOrPermissions_AndOriginRestored()
    {
        var context = (await Restore(User(), PersistedIdentitySource.Internal)).ShouldBeSuccess();

        context.Issued().UserId.ShouldBe("alice");
        context.Issued().Roles.ShouldBeEmpty();
        context.Issued().Permissions.ShouldBeEmpty();
        context.Issued().Principal!.Identity!.AuthenticationType.ShouldBe(RequestContextScopeFactory.RestoredAuthenticationType);
        context.TenantId.ShouldBe("t-row");
        context.CorrelationId.ShouldBe("corr-1");
        context.CausationId.ShouldBe("cause-1");
        ((RequestContext)context).Origin.ShouldBe(RequestOrigin.Restored);
        _host.Logger.Collector.GetSnapshot().Single(r => r.Id.Id == 171).Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task AnInternalRow_IgnoresTheTrustedTenant()
    {
        var context = (await Restore(User(), PersistedIdentitySource.Internal, configuredTenantId: "configured")).ShouldBeSuccess();

        context.TenantId.ShouldBe("t-row");
    }

    [Fact]
    public async Task AnInternalService_TakesItsAuthorityFromTheCatalog()
    {
        var persisted = new PersistedRequestIdentity(IdentityKind.Service, ScopeTestHost.Job, null, "corr-1", null);

        var context = (await Restore(persisted, PersistedIdentitySource.Internal)).ShouldBeSuccess();

        context.Issued().UserId.ShouldBe("service:test-job");
        context.Issued().Roles.ShouldBe(["job"]);
    }

    [Fact]
    public async Task AnInternalAnonymousRow_OpensAnAnonymousScope()
    {
        var persisted = new PersistedRequestIdentity(IdentityKind.Anonymous, null, null, "corr-1", null);

        (await Restore(persisted, PersistedIdentitySource.Internal)).ShouldBeSuccess().Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
    }

    [Theory]
    [InlineData(IdentityKind.User, "service:test-job")]
    [InlineData(IdentityKind.User, " alice")]
    [InlineData(IdentityKind.User, null)]
    [InlineData(IdentityKind.Service, "not-declared")]
    [InlineData(IdentityKind.Service, ScopeTestHost.BuiltIn)]
    [InlineData(IdentityKind.Service, null)]
    [InlineData(IdentityKind.Anonymous, "someone")]
    [InlineData((IdentityKind)9, "alice")]
    public async Task AnInvalidActor_IsRefused_WithoutInvokingWork(IdentityKind kind, string? actor)
    {
        var invoked = false;
        var persisted = new PersistedRequestIdentity(kind, actor, null, "corr-1", null);

        var result = await _host.Factory.RunRestoredAsync<int>(persisted, PersistedIdentitySource.Internal, (_, _) =>
        {
            invoked = true;
            return Task.FromResult(Right<EncinaError, int>(1));
        });

        result.IsLeft.ShouldBeTrue();
        invoked.ShouldBeFalse();
        _host.EventIds.ShouldContain(167);
    }

    [Theory]
    [InlineData("", null, null)]
    [InlineData("corr\n", null, null)]
    [InlineData("corr-1", "cause\u0001", null)]
    [InlineData("corr-1", null, "tenant\r")]
    public async Task InvalidIds_AreRefused(string correlationId, string? causationId, string? tenantId)
    {
        var persisted = new PersistedRequestIdentity(IdentityKind.User, "alice", tenantId, correlationId, causationId);

        ShouldBeInvalid(await Restore(persisted, PersistedIdentitySource.Internal));
    }

    [Fact]
    public async Task AnOverLongCorrelationId_IsRefused()
    {
        ShouldBeInvalid(await Restore(User() with { CorrelationId = new string('c', 129) }, PersistedIdentitySource.Internal));
    }

    [Fact]
    public async Task TheDefaultSource_IsRefused_WithoutInvokingWork()
    {
        ShouldBeInvalid(await Restore(User(), default));
        ShouldBeInvalid(await Restore(User(), (PersistedIdentitySource)7));
    }

    [Fact]
    public async Task AnExternalUserRow_RunsAnonymous_WithOriginInbound_AndNoTenant()
    {
        var context = (await Restore(User(Sentinel, tenant: Sentinel + "-tenant"), PersistedIdentitySource.External)).ShouldBeSuccess();

        context.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        context.TenantId.ShouldBeNull();
        ((RequestContext)context).Origin.ShouldBe(RequestOrigin.Inbound);
        foreach (var record in _host.Logger.Collector.GetSnapshot())
        {
            record.Message.ShouldNotContain(Sentinel);
        }
    }

    [Fact]
    public async Task AnExternalRow_UsesTheTrustedTenant_WhateverTheRowSays()
    {
        var context = (await Restore(User(tenant: "row-tenant"), PersistedIdentitySource.External, configuredTenantId: "t1")).ShouldBeSuccess();

        context.TenantId.ShouldBe("t1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("t\n1")]
    public async Task AnInvalidTrustedTenant_IsRefused(string configuredTenantId)
    {
        ShouldBeInvalid(await Restore(User(), PersistedIdentitySource.External, configuredTenantId));
    }

    [Fact]
    public async Task AnOverLongTrustedTenant_IsRefused()
    {
        ShouldBeInvalid(await Restore(User(), PersistedIdentitySource.External, new string('t', 129)));
    }

    [Fact]
    public async Task InsideAnExternalRestore_RunAsService_NeedsAllowOverInbound_AndThenLogs174WithTheServiceName()
    {
        var outcome = await _host.Factory.RunRestoredAsync(User(), PersistedIdentitySource.External, async (_, ct) =>
        {
            var refused = await _host.Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture, cancellationToken: ct);
            refused.IsLeft.ShouldBeTrue();
            return await _host.Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture, new IdentityScopeOptions(AllowOverInbound: true), ct);
        });

        outcome.ShouldBeSuccess().Issued().UserId.ShouldBe("service:test-job");
        _host.Logger.Collector.GetSnapshot().Single(r => r.Id.Id == 174).Message.ShouldContain(ScopeTestHost.Job);
    }

    [Fact]
    public async Task InsideAnInternalUserRestore_RunAsService_IsRefused()
    {
        var outcome = await _host.Factory.RunRestoredAsync(User(), PersistedIdentitySource.Internal, (_, ct) =>
            _host.Factory.RunAsServiceAsync(ScopeTestHost.Job, ScopeTestHost.Capture, new IdentityScopeOptions(AllowOverInbound: true), ct));

        outcome.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ToPersisted_RoundTripsThroughRunRestoredAsync()
    {
        var persisted = TestIdentity.User("alice", roles: ["admin"]).ToPersisted("t1", "corr-9", null);

        var context = (await Restore(persisted, PersistedIdentitySource.Internal)).ShouldBeSuccess();

        context.Issued().UserId.ShouldBe("alice");
        context.Issued().Roles.ShouldBeEmpty();
    }
}
