using System.Runtime.CompilerServices;
using System.Security.Claims;
using Encina.Testing;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// The explicit-context rule of <c>AmbientRequestContext.Resolve</c> through <see cref="IEncina"/>
/// (Design 1 rule order: unauthenticated, stale issuer, a user in the chain, no issuer outside an
/// active scope of the same identity, a live different identity, the same identity; then the tenant
/// rule on every accepted context) and the identity- and origin-preserving setter of
/// <see cref="RequestContextAccessor.RequestContext"/>. Every refusal logs EventId 165 with kinds only.
/// </summary>
public sealed class ExplicitContextConflictTests
{
    private const string Anonymous = "(anonymous)";

    public sealed record Probe : IRequest<string?>;

    public sealed record MetaProbe(Action MutateBeforeRead) : IRequest<string?>;

    /// <summary>Awaits <c>BeforeRead</c>, then reads the ambient user id.</summary>
    public sealed record ReadAfter(Func<Task> BeforeRead) : IRequest<string?>;

    /// <summary>Sends <see cref="Probe"/> from inside the handler with <c>Context</c>.</summary>
    public sealed record SendNested(IRequestContext Context) : IRequest<string?>;

    /// <summary>Runs <c>Inside</c> while a dispatch is in flight.</summary>
    public sealed record RunInDispatch(Action Inside) : IRequest<string?>;

    public sealed class MetaProbeHandler(IRequestContextAccessor accessor) : IRequestHandler<MetaProbe, string?>
    {
        public Task<Either<EncinaError, string?>> Handle(MetaProbe request, CancellationToken cancellationToken)
        {
            request.MutateBeforeRead();
            return Task.FromResult<Either<EncinaError, string?>>(accessor.RequestContext?.Metadata["k"] as string);
        }
    }

    public sealed class ReadAfterHandler(IRequestContextAccessor accessor) : IRequestHandler<ReadAfter, string?>
    {
        public async Task<Either<EncinaError, string?>> Handle(ReadAfter request, CancellationToken cancellationToken)
        {
            await request.BeforeRead();
            return accessor.RequestContext?.UserId ?? Anonymous;
        }
    }

    public sealed class SendNestedHandler(IEncina encina) : IRequestHandler<SendNested, string?>
    {
        public async Task<Either<EncinaError, string?>> Handle(SendNested request, CancellationToken cancellationToken)
            => await encina.Send(new Probe(), request.Context, cancellationToken);
    }

    public sealed class RunInDispatchHandler : IRequestHandler<RunInDispatch, string?>
    {
        public Task<Either<EncinaError, string?>> Handle(RunInDispatch request, CancellationToken cancellationToken)
        {
            request.Inside();
            return Task.FromResult<Either<EncinaError, string?>>("ran");
        }
    }

    public sealed record Ping : INotification;

    public sealed record Count : IStreamRequest<int>;

    public sealed class ProbeHandler(IRequestContextAccessor accessor) : IRequestHandler<Probe, string?>
    {
        public Task<Either<EncinaError, string?>> Handle(Probe request, CancellationToken cancellationToken)
            => Task.FromResult<Either<EncinaError, string?>>(accessor.RequestContext?.UserId ?? Anonymous);
    }

    public sealed class PingHandler : INotificationHandler<Ping>
    {
        public Task<Either<EncinaError, Unit>> Handle(Ping notification, CancellationToken cancellationToken)
            => Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    public sealed class CountHandler : IStreamRequestHandler<Count, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(Count request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return 1;
        }
    }

    private readonly FakeLogger<global::Encina.Encina> _logger = new();

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddEncinaServiceIdentity(ScopeTestHost.Job, id => id.WithRoles("job"));
        services.AddSingleton<ILogger<global::Encina.Encina>>(_logger);
        services.AddScoped<IRequestHandler<Probe, string?>, ProbeHandler>();
        services.AddScoped<IRequestHandler<MetaProbe, string?>, MetaProbeHandler>();
        services.AddScoped<IRequestHandler<ReadAfter, string?>, ReadAfterHandler>();
        services.AddScoped<IRequestHandler<SendNested, string?>, SendNestedHandler>();
        services.AddScoped<IRequestHandler<RunInDispatch, string?>, RunInDispatchHandler>();
        services.AddScoped<INotificationHandler<Ping>, PingHandler>();
        services.AddScoped<IStreamRequestHandler<Count, int>, CountHandler>();
        return services.BuildServiceProvider();
    }

    // An issuer-less (built) context: never binds an identity by itself.
    private static IRequestContext Built(string id, string? tenant = null) => TestRequestContext.For(TestIdentity.User(id), tenantId: tenant);

    private static ClaimsPrincipal Principal(string id, string? tenant = null, IEnumerable<Claim>? claims = null, IEnumerable<string>? roles = null)
    {
        var extra = new List<Claim>(claims ?? []);
        if (tenant is not null)
        {
            extra.Add(new Claim("tenant_id", tenant));
        }

        return TestIdentity.Principal(id, roles, claims: extra);
    }

    /// <summary>Runs <paramref name="body"/> inside a user scope opened through the factory.</summary>
    private static async Task InUserScope(ServiceProvider provider, ClaimsPrincipal principal, Func<IRequestContext, Task> body)
    {
        var result = await provider.GetRequiredService<IRequestContextScopeFactory>().RunAsPrincipalAsync(principal, async (context, _) =>
        {
            await body(context);
            return Right<EncinaError, Unit>(unit);
        });

        result.ShouldBeSuccess();
    }

    /// <summary>
    /// Opens a scope for <paramref name="principal"/> in another flow and keeps it live until the
    /// returned release is called: the context it returns is issued and live, while the caller's own
    /// flow carries no chain facts.
    /// </summary>
    private static async Task<(IRequestContext Context, Func<Task> Release)> LiveScopeElsewhere(ServiceProvider provider, ClaimsPrincipal principal)
    {
        var captured = new TaskCompletionSource<IRequestContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopes = provider.GetRequiredService<IRequestContextScopeFactory>();
        var running = Task.Run(() => scopes.RunAsPrincipalAsync(principal, async (context, _) =>
        {
            captured.SetResult(context);
            await gate.Task;
            return Right<EncinaError, Unit>(unit);
        }));

        var context = await captured.Task;
        return (context, async () =>
        {
            gate.TrySetResult();
            (await running).ShouldBeSuccess();
        });
    }

    private void Expect165(string outcome)
    {
        var record = _logger.Collector.GetSnapshot().Last(r => r.Id.Id == 165);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain(outcome);
        record.Message.ShouldNotContain("sentinel");
    }

    private static void ShouldBeRefusedWith(Either<EncinaError, string?> result, string code)
    {
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetEncinaCode().ShouldBe(code));
    }

    // ── Rule 3: a user in the chain and a different identity ─────────────

    [Fact]
    public async Task Send_InsideAUserScope_WithAnotherUser_IsRefused_AndLogs165WithKindsOnly()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice-sentinel"), async _ =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), Built("mallory-sentinel"));

            ShouldBeRefusedWith(result, RequestIdentityErrorCodes.ScopeConflict);
        });

        Expect165("refused");
    }

    [Fact]
    public async Task Publish_And_Stream_InsideAUserScope_WithAnotherUser_AreRefused()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice"), async _ =>
        {
            var encina = provider.GetRequiredService<IEncina>();
            (await encina.Publish(new Ping(), Built("bob"))).IsLeft.ShouldBeTrue();
            var items = new List<Either<EncinaError, int>>();
            await foreach (var item in encina.Stream(new Count(), Built("bob")))
            {
                items.Add(item);
            }

            items.ShouldHaveSingleItem().IsLeft.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Send_WithTheScopeUserIdButOtherRoles_IsRefused()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice", roles: ["reader"]), async _ =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(
                new Probe(), TestRequestContext.For(TestIdentity.User("alice", roles: ["reader", "admin"])));

            ShouldBeRefusedWith(result, RequestIdentityErrorCodes.ScopeConflict);
        });
    }

    [Fact]
    public async Task Send_AnExplicitAnonymousInsideAUserScope_ThenANestedSendWithADifferentLiveIdentity_IsRefused()
    {
        await using var provider = BuildProvider();
        var (other, release) = await LiveScopeElsewhere(provider, Principal("mallory"));

        await InUserScope(provider, Principal("alice"), async _ =>
        {
            // The anonymous explicit Send is accepted (rule 1); inside it the ambient reads Anonymous,
            // but the holder chain still records the user, so the nested identity change is refused.
            var result = await provider.GetRequiredService<IEncina>().Send(new SendNested(other), RequestContext.CreateForTest());

            ShouldBeRefusedWith(result, RequestIdentityErrorCodes.ScopeConflict);
        });

        await release();
    }

    // ── Rules 1, 4 and 6: accepted contexts ──────────────────────────────

    [Fact]
    public async Task Send_WithTheScopesOwnContext_IsAccepted_WithoutALog()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice"), async context =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), TestRequestContext.WithIdentity(RequestContext.CreateForTest(), context.Identity));

            result.ShouldBeSuccess().ShouldBe("alice");
        });

        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165);
    }

    [Fact]
    public async Task Send_WithAnIssuerLessIdentity_InsideAnActiveScopeOfTheSameIdentity_IsAccepted()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice"), async _ =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), Built("alice"));

            result.ShouldBeSuccess().ShouldBe("alice");
        });

        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165);
    }

    [Fact]
    public async Task Send_WithAnExplicitAnonymousContext_InsideAUserScope_IsAccepted()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice"), async _ =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), RequestContext.CreateForTest());

            result.ShouldBeSuccess().ShouldBe(Anonymous);
        });

        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165);
    }

    [Fact]
    public async Task Send_AForeignExplicitContextWithANullIdentity_IsTreatedAsAnonymous()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice"), async _ =>
        {
            var foreign = Substitute.For<IRequestContext>();
            foreign.CorrelationId.Returns("corr-foreign");
            foreign.TenantId.Returns((string?)null);
            foreign.Metadata.Returns(new Dictionary<string, object?>());

            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), foreign);

            result.ShouldBeSuccess().ShouldBe(Anonymous);
        });
    }

    [Fact]
    public async Task Send_AForeignExplicitContext_IsCheckedAndDispatchedAsOneSnapshot()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice"), async context =>
        {
            // The first read (the check) sees alice; every later read would see mallory.
            var foreign = Substitute.For<IRequestContext>();
            foreign.CorrelationId.Returns("corr-foreign");
            foreign.TenantId.Returns((string?)null);
            foreign.Metadata.Returns(new Dictionary<string, object?>());
            foreign.Identity.Returns(context.Identity, TestIdentity.User("mallory"));

            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), foreign);

            result.ShouldBeSuccess().ShouldBe("alice");
            _ = foreign.Received(1).Identity;
        });
    }

    // ── Rule 4: no issuer outside an active scope of the same identity ───

    [Fact]
    public async Task Send_WithAnIssuerLessUser_AndNoAmbient_IsRefused_AndLogs165()
    {
        await using var provider = BuildProvider();

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), Built("job-owner"));

        ShouldBeRefusedWith(result, RequestIdentityErrorCodes.ScopeConflict);
        Expect165("refused");
    }

    [Fact]
    public async Task Send_WithAnIssuerLessUser_InsideAServiceScope_IsRefused()
    {
        await using var provider = BuildProvider();

        var outcome = await provider.GetRequiredService<IRequestContextScopeFactory>().RunAsServiceAsync(
            ScopeTestHost.Job,
            async (_, ct) => await provider.GetRequiredService<IEncina>().Send(new Probe(), Built("job-owner"), ct));

        ShouldBeRefusedWith(outcome, RequestIdentityErrorCodes.ScopeConflict);
    }

    // ── Rule 5: a live different identity with no user in the chain ──────

    [Fact]
    public async Task Send_WithALiveIssuedUser_FromAFlowWithNoUser_IsAccepted_LogsAccepted_AndRestoresTheAmbient()
    {
        await using var provider = BuildProvider();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var ambient = RequestContext.CreateForTest();
        accessor.RequestContext = ambient;
        var (live, release) = await LiveScopeElsewhere(provider, Principal("job-owner"));

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), live);

        result.ShouldBeSuccess().ShouldBe("job-owner");
        Expect165("accepted");
        accessor.RequestContext.ShouldBeSameAs(ambient);
        await release();
    }

    // ── Rule 2: stale issuers ───────────────────────────────────────────

    [Fact]
    public async Task Send_WithAStaleIdentity_IsRefused_EvenInsideAScopeOfTheSameUser()
    {
        await using var provider = BuildProvider();
        IRequestContext? stale = null;
        await InUserScope(provider, Principal("alice"), context =>
        {
            stale = context;
            return Task.CompletedTask;
        });

        await InUserScope(provider, Principal("alice"), async _ =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), stale!);

            ShouldBeRefusedWith(result, RequestIdentityErrorCodes.ScopeConflict);
        });
    }

    [Fact]
    public async Task Send_WithAStaleIdentityWrappedInAForeignContext_IsRefused()
    {
        await using var provider = BuildProvider();
        RequestIdentity? stale = null;
        await InUserScope(provider, Principal("alice"), context =>
        {
            stale = context.Identity;
            return Task.CompletedTask;
        });
        var foreign = Substitute.For<IRequestContext>();
        foreign.CorrelationId.Returns("corr-foreign");
            foreign.TenantId.Returns((string?)null);
        foreign.Metadata.Returns(new Dictionary<string, object?>());
        foreign.Identity.Returns(stale);

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), foreign);

        ShouldBeRefusedWith(result, RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task AnExplicitContextInstalledDuringDispatch_ReadsAnonymous_OnceItsIssuerEnds()
    {
        await using var provider = BuildProvider();
        var (live, release) = await LiveScopeElsewhere(provider, Principal("job-owner"));

        // The handler reads the ambient after the issuing scope has ended in the other flow.
        var result = await provider.GetRequiredService<IEncina>().Send(new ReadAfter(release), live);

        result.ShouldBeSuccess().ShouldBe(Anonymous);
    }

    // ── Tenant rule (every accepted context, chain has a user) ───────────

    [Fact]
    public async Task Send_AnAnonymousContextWithAnotherTenant_InsideAUserScope_IsTenantConflict()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice", tenant: "t1"), async _ =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), RequestContext.CreateForTest(tenantId: "t2"));

            ShouldBeRefusedWith(result, RequestIdentityErrorCodes.TenantConflict);
        });

        Expect165("refused (tenant)");
    }

    [Fact]
    public async Task Send_AnIssuerLessSameIdentityWithAnotherTenant_InsideTheScope_IsTenantConflict()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice", tenant: "t1"), async _ =>
        {
            // The same identity (same claims, tenant claim included) built outside the scope: rule 4
            // accepts it, then the tenant rule refuses the other context tenant.
            var sameIdentity = TestIdentity.User("alice", claims: [new Claim("tenant_id", "t1")]);
            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), TestRequestContext.For(sameIdentity, tenantId: "t2"));

            ShouldBeRefusedWith(result, RequestIdentityErrorCodes.TenantConflict);
        });
    }

    [Fact]
    public async Task Send_TheScopesOwnContextWithAnotherTenant_IsTenantConflict()
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice", tenant: "t1"), async context =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), context.WithTenantId("t2"));

            ShouldBeRefusedWith(result, RequestIdentityErrorCodes.TenantConflict);
        });
    }

    [Fact]
    public async Task Send_AnotherTenant_UnderAServiceScope_IsAccepted()
    {
        await using var provider = BuildProvider();

        var outcome = await provider.GetRequiredService<IRequestContextScopeFactory>().RunAsServiceAsync(
            ScopeTestHost.Job,
            async (_, ct) => await provider.GetRequiredService<IEncina>().Send(new Probe(), RequestContext.CreateForTest(tenantId: "other"), ct),
            new IdentityScopeOptions(TenantId: "t1"));

        outcome.ShouldBeSuccess().ShouldBe(Anonymous);
    }

    // ── Per-token claims (Q2) ────────────────────────────────────────────

    [Fact]
    public async Task Send_PerTokenClaimChanges_AreTheSameIdentity()
    {
        await using var provider = BuildProvider();
        var scopeClaims = new[] { new Claim("exp", "100"), new Claim("jti", "a"), new Claim("amr", "pwd") };

        await InUserScope(provider, Principal("alice", claims: scopeClaims), async _ =>
        {
            var refreshed = TestRequestContext.For(TestIdentity.User("alice", claims: [new Claim("exp", "200"), new Claim("jti", "b"), new Claim("amr", "pwd")]));

            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), refreshed);

            result.ShouldBeSuccess().ShouldBe("alice");
        });
    }

    [Theory]
    [InlineData("amr", "pwd", "mfa")]
    [InlineData("acr", "1", "2")]
    [InlineData("auth_time", "100", "200")]
    public async Task Send_StepUpClaimChanges_AreADifferentIdentity(string claimType, string scopeValue, string explicitValue)
    {
        await using var provider = BuildProvider();

        await InUserScope(provider, Principal("alice", claims: [new Claim(claimType, scopeValue)]), async _ =>
        {
            var other = TestRequestContext.For(TestIdentity.User("alice", claims: [new Claim(claimType, explicitValue)]));

            var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), other);

            ShouldBeRefusedWith(result, RequestIdentityErrorCodes.ScopeConflict);
        });
    }

    // ── The setter: identity- and origin-preserving, never clears ────────

    [Fact]
    public async Task Setter_ReplacingTheScopeUserWithAnotherUser_Throws_AndLogs165WithKindsOnly()
    {
        var host = new ScopeTestHost();
        var logger = new FakeLogger<RequestContextAccessor>();
        var accessor = new RequestContextAccessor(logger);

        await host.InUserScope("alice-sentinel", context =>
        {
            Should.Throw<InvalidOperationException>(() => accessor.RequestContext = Built("mallory-sentinel"));
            accessor.RequestContext.ShouldBeSameAs(context);
            return Task.FromResult(0);
        });

        var record = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 165);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("refused");
        record.Message.ShouldNotContain("sentinel");
    }

    [Fact]
    public async Task Setter_TheScopeUserWithOtherRoles_Throws()
    {
        var host = new ScopeTestHost();

        await host.InUserScope("alice", context =>
        {
            Should.Throw<InvalidOperationException>(
                () => host.Accessor.RequestContext = TestRequestContext.WithIdentity(context, TestIdentity.User("alice", roles: ["admin"])));
            return Task.FromResult(0);
        });
    }

    [Fact]
    public async Task Setter_AnIdentityAndOriginPreservingSet_IsAccepted_WithoutALog()
    {
        var host = new ScopeTestHost();
        var logger = new FakeLogger<RequestContextAccessor>();
        var accessor = new RequestContextAccessor(logger);

        await host.InUserScope("alice", context =>
        {
            accessor.RequestContext = context.WithMetadata("k", "v").WithTenantId("t1");
            accessor.RequestContext!.UserId.ShouldBe("alice");
            accessor.RequestContext.Metadata["k"].ShouldBe("v");
            return Task.FromResult(0);
        });

        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165);
    }

    [Fact]
    public async Task Setter_ADowngradeToAnonymous_Throws()
    {
        var host = new ScopeTestHost();

        await host.InUserScope("alice", _ =>
        {
            Should.Throw<InvalidOperationException>(() => host.Accessor.RequestContext = RequestContext.CreateForTest());
            host.Accessor.RequestContext!.UserId.ShouldBe("alice");
            return Task.FromResult(0);
        });
    }

    [Fact]
    public async Task Setter_AClear_Throws()
    {
        var host = new ScopeTestHost();

        await host.InUserScope("alice", _ =>
        {
            Should.Throw<InvalidOperationException>(() => host.Accessor.RequestContext = null);
            return Task.FromResult(0);
        });
    }

    [Fact]
    public async Task Setter_AnOriginChange_Throws()
    {
        var host = new ScopeTestHost();

        await host.InUserScope("alice", context =>
        {
            // Same identity object, but a fresh context has origin Unspecified, not Scope.
            Should.Throw<InvalidOperationException>(
                () => host.Accessor.RequestContext = TestRequestContext.WithIdentity(RequestContext.CreateForTest(), context.Identity));
            return Task.FromResult(0);
        });
    }

    [Fact]
    public async Task Setter_ATenantChangeDuringADispatch_Throws_AndOutsideADispatchIsAccepted()
    {
        await using var provider = BuildProvider();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        Exception? insideDispatch = null;

        await InUserScope(provider, Principal("alice", tenant: "t1"), async context =>
        {
            var result = await provider.GetRequiredService<IEncina>().Send(new RunInDispatch(() =>
                insideDispatch = Record.Exception(() => accessor.RequestContext = accessor.RequestContext!.WithTenantId("t2"))));
            result.ShouldBeSuccess();

            accessor.RequestContext = context.WithTenantId("t3");
            accessor.RequestContext!.TenantId.ShouldBe("t3");
        });

        insideDispatch.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Setter_WithNoReadableContext_AcceptsOnlyAnAnonymousUnspecifiedContext()
    {
        await Task.Yield();
        var accessor = new RequestContextAccessor();

        Should.Throw<InvalidOperationException>(() => accessor.RequestContext = Built("alice"));
        Should.Throw<InvalidOperationException>(
            () => accessor.RequestContext = ((RequestContext)RequestContext.CreateForTest()).WithOrigin(RequestOrigin.Inbound));

        accessor.RequestContext = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "corr-1");
        accessor.RequestContext!.CorrelationId.ShouldBe("corr-1");
    }

    [Fact]
    public async Task Setter_StoresTheSnapshotOfAForeignContext_NotTheCallersObject()
    {
        await Task.Yield();
        var accessor = new RequestContextAccessor();
        var foreign = Substitute.For<IRequestContext>();
        foreign.CorrelationId.Returns("corr-foreign");
            foreign.TenantId.Returns((string?)null);
        foreign.Metadata.Returns(new Dictionary<string, object?>());

        // The check reads Anonymous; every later read of the foreign object would return mallory.
        foreign.Identity.Returns(RequestIdentity.Anonymous, TestIdentity.User("mallory"));

        accessor.RequestContext = foreign;

        accessor.RequestContext.ShouldBeOfType<RequestContext>();
        accessor.RequestContext!.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        accessor.RequestContext.UserId.ShouldBeNull();
    }

    // ── Snapshots ───────────────────────────────────────────────────────

    [Fact]
    public async Task Send_AForeignExplicitContext_SnapshotsItsMetadata()
    {
        await using var provider = BuildProvider();
        var metadata = new Dictionary<string, object?> { ["k"] = "original" };
        var foreign = Substitute.For<IRequestContext>();
        foreign.CorrelationId.Returns("corr-foreign");
            foreign.TenantId.Returns((string?)null);
        foreign.Metadata.Returns(metadata);
        var encina = provider.GetRequiredService<IEncina>();

        // The handler of MetaProbe reads the metadata after the caller mutated its dictionary.
        var task = encina.Send(new MetaProbe(() => metadata["k"] = "mutated"), foreign);
        var result = await task;

        result.ShouldBeSuccess().ShouldBe("original");
    }
}
