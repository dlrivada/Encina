#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute .Returns() pattern for ValueTask
using Encina.Security.ABAC;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Tests for <see cref="ABACPolicySeedingHostedService"/>. Seeding runs under the built-in service
/// identity through the real scope factory (#1705 Phase 4).
/// </summary>
public sealed class ABACPolicySeedingHostedServiceTests : IDisposable
{
    private readonly IPolicyAdministrationPoint _pap = Substitute.For<IPolicyAdministrationPoint>();
    private readonly ServiceProvider _provider;
    private readonly IInternalRequestContextScopeFactory _scopes;

    public ABACPolicySeedingHostedServiceTests()
    {
        _provider = ScopeProvider(declareSeedingIdentity: true);
        _scopes = _provider.GetRequiredService<IInternalRequestContextScopeFactory>();
    }

    public void Dispose() => _provider.Dispose();

    private static ServiceProvider ScopeProvider(bool declareSeedingIdentity)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRequestIdentity();
        if (declareSeedingIdentity)
        {
            services.AddBuiltInServiceIdentity(ABACPolicySeedingHostedService.ServiceIdentityName);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void Constructor_WithNullPap_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ABACPolicySeedingHostedService(
            null!,
            _scopes,
            Options.Create(new ABACOptions()),
            NullLogger<ABACPolicySeedingHostedService>.Instance));
    }

    [Fact]
    public void Constructor_WithNullScopeFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ABACPolicySeedingHostedService(
            _pap,
            null!,
            Options.Create(new ABACOptions()),
            NullLogger<ABACPolicySeedingHostedService>.Instance)).ParamName.ShouldBe("scopes");
    }

    [Fact]
    public async Task StartAsync_SeedsUnderTheBuiltInServiceIdentity()
    {
        var policy = CreatePolicy("identity-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(policy);
        RequestIdentity? seenIdentity = null;
        var accessor = _provider.GetRequiredService<IRequestContextAccessor>();
        _pap.AddPolicyAsync(policy, null, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            seenIdentity = accessor.RequestContext?.Identity;
            return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
        });
        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        seenIdentity.ShouldNotBeNull();
        seenIdentity.Kind.ShouldBe(IdentityKind.Service);
        seenIdentity.UserId.ShouldBe("service:encina.abac.policy-seeding");
        (accessor.RequestContext?.Identity ?? RequestIdentity.Anonymous).IsAuthenticated
            .ShouldBeFalse("the service identity ends with the seeding scope");
    }

    [Fact]
    public async Task StartAsync_LogsTheSummaryWithCountsAsEventId9096AndNoAdHocInformation()
    {
        var seeded = CreatePolicySet("summary-ps");
        var duplicate = CreatePolicy("summary-dup");
        var fresh = CreatePolicy("summary-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(seeded);
        options.Value.SeedPolicies.Add(duplicate);
        options.Value.SeedPolicies.Add(fresh);
        _pap.AddPolicySetAsync(seeded, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
        _pap.AddPolicyAsync(duplicate, null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(ABACErrors.DuplicatePolicy("summary-dup"))));
        _pap.AddPolicyAsync(fresh, null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
        var logger = new FakeLogger<ABACPolicySeedingHostedService>();
        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, logger);

        await sut.StartAsync(CancellationToken.None);

        var records = logger.Collector.GetSnapshot();
        var summary = records.Single(r => r.Id.Id == 9096);
        summary.Level.ShouldBe(Microsoft.Extensions.Logging.LogLevel.Information);
        summary.GetStructuredStateValue("SeededPolicySets").ShouldBe("1");
        summary.GetStructuredStateValue("TotalPolicySets").ShouldBe("1");
        summary.GetStructuredStateValue("SeededPolicies").ShouldBe("1");
        summary.GetStructuredStateValue("TotalPolicies").ShouldBe("2");
        records.Where(r => r.Level == Microsoft.Extensions.Logging.LogLevel.Information)
            .ShouldAllBe(r => r.Id.Id == 9096, "the summary replaces the ad hoc Information logs");
    }

    [Fact]
    public async Task StartAsync_ScopeRefusedForCancellation_ThrowsOperationCanceledException()
    {
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(CreatePolicy("cancelled-p"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        var ex = await Should.ThrowAsync<OperationCanceledException>(() => sut.StartAsync(cts.Token));

        ex.ShouldNotBeOfType<InvalidOperationException>();
        await _pap.DidNotReceive().AddPolicyAsync(Arg.Any<Policy>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ScopeRefusedForAnyOtherReason_ThrowsInvalidOperationExceptionWithTheCodeOnly()
    {
        using var provider = ScopeProvider(declareSeedingIdentity: false);
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(CreatePolicy("undeclared-p"));
        var sut = new ABACPolicySeedingHostedService(
            _pap,
            provider.GetRequiredService<IInternalRequestContextScopeFactory>(),
            options,
            NullLogger<ABACPolicySeedingHostedService>.Instance);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain(RequestIdentityErrorCodes.UnknownServiceIdentity);
        ex.Message.ShouldNotContain("AddEncinaServiceIdentity", Case.Sensitive, "the error message never reaches the exception");
        await _pap.DidNotReceive().AddPolicyAsync(Arg.Any<Policy>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_InsideAUserScope_IsRefusedWithTheCode()
    {
        // A built-in identity never opens over a user's request.
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(CreatePolicy("over-user-p"));
        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        var factory = _provider.GetRequiredService<IRequestContextScopeFactory>();

        var outcome = await factory.RunAsPrincipalAsync(
            global::Encina.Testing.Identity.TestIdentity.Principal("alice"),
            async (_, ct) =>
            {
                var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(ct));
                return Right<EncinaError, string>(ex.Message);
            });

        outcome.Match(Right: message => message, Left: _ => "<left>").ShouldContain(RequestIdentityErrorCodes.ScopeConflict);
    }

    [Fact]
    public async Task StartAsync_WithNoSeedData_DoesNotCallPap()
    {
        var options = Options.Create(new ABACOptions());
        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await _pap.DidNotReceive().AddPolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>());
        await _pap.DidNotReceive().AddPolicyAsync(Arg.Any<Policy>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_SeedsPolicySets()
    {
        var policySet = CreatePolicySet("seed-ps-1");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(policySet);

        _pap.AddPolicySetAsync(policySet, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        await _pap.Received(1).AddPolicySetAsync(policySet, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_SeedsPolicies()
    {
        var policy = CreatePolicy("seed-p-1");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(policy);

        _pap.AddPolicyAsync(policy, null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        await _pap.Received(1).AddPolicyAsync(policy, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenPolicySetAlreadyExists_SkipsItAndContinues()
    {
        var policySet1 = CreatePolicySet("fail-ps");
        var policySet2 = CreatePolicySet("success-ps");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(policySet1);
        options.Value.SeedPolicySets.Add(policySet2);

        _pap.AddPolicySetAsync(policySet1, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(ABACErrors.DuplicatePolicySet("fail-ps"))));
        _pap.AddPolicySetAsync(policySet2, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        // Both should have been attempted
        await _pap.Received(2).AddPolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenStandalonePolicyAlreadyExists_WarnsWithDuplicateCodeAndSeedsTheNext()
    {
        var dup = CreatePolicy("dup-p");
        var next = CreatePolicy("next-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(dup);
        options.Value.SeedPolicies.Add(next);
        _pap.AddPolicyAsync(dup, null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(ABACErrors.DuplicatePolicy("dup-p"))));
        _pap.AddPolicyAsync(next, null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
        var logger = new CapturingLogger();

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, logger);

        await sut.StartAsync(CancellationToken.None);

        logger.Messages.ShouldContain(m =>
            m.Level == Microsoft.Extensions.Logging.LogLevel.Warning && m.Text.Contains(ABACErrors.DuplicatePolicyCode));
        await _pap.Received(1).AddPolicyAsync(next, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_PolicySetDuplicateCodeFromTheSetPath_IsSkipped()
    {
        var policySet = CreatePolicySet("dup-ps");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(policySet);
        _pap.AddPolicySetAsync(policySet, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(ABACErrors.DuplicatePolicySet("dup-ps"))));
        var logger = new CapturingLogger();

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, logger);

        await sut.StartAsync(CancellationToken.None);

        logger.Messages.ShouldContain(m =>
            m.Level == Microsoft.Extensions.Logging.LogLevel.Warning && m.Text.Contains(ABACErrors.DuplicatePolicySetCode));
    }

    [Fact]
    public async Task StartAsync_PolicySetDuplicateCodeFromThePolicyPath_FailsStartup()
    {
        // The skip rule is per kind: a standalone policy answered with the policy-set duplicate code is not a duplicate.
        var policy = CreatePolicy("cross-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(policy);
        _pap.AddPolicyAsync(policy, null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(ABACErrors.DuplicatePolicySet("cross-p"))));

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));
        ex.Message.ShouldContain(ABACErrors.DuplicatePolicySetCode);
    }

    [Fact]
    public async Task StartAsync_WhenPapThrowsCancellationButTokenIsNotCancelled_FailsStartup()
    {
        var policy = CreatePolicy("oce-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(policy);
        _pap.AddPolicyAsync(policy, null, Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new OperationCanceledException());

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));
        ex.Message.ShouldContain("oce-p");
        ex.Message.ShouldContain(nameof(OperationCanceledException));
    }

    private sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger<ABACPolicySeedingHostedService>
    {
        public List<(Microsoft.Extensions.Logging.LogLevel Level, string Text)> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add((logLevel, formatter(state, exception)));
    }

    [Fact]
    public async Task StartAsync_WhenAuditFails_FailsStartupNamingIdAndCodeOnly()
    {
        var policy = CreatePolicy("audited-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(policy);
        _pap.AddPolicyAsync(policy, null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(ABACErrors.PolicyChangeAuditFailed("audit.down"))));

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));
        ex.Message.ShouldContain("audited-p");
        ex.Message.ShouldContain(ABACErrors.PolicyChangeAuditFailedCode);
    }

    [Fact]
    public async Task StartAsync_WhenStoreFailsForPolicySet_FailsStartup()
    {
        var policySet = CreatePolicySet("store-ps");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(policySet);
        _pap.AddPolicySetAsync(policySet, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(EncinaErrors.Create("store.unavailable", "secret detail"))));

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));
        ex.Message.ShouldContain("store-ps");
        ex.Message.ShouldContain("store.unavailable");
        ex.Message.ShouldNotContain("secret detail");
    }

    [Fact]
    public async Task StartAsync_WhenPapThrows_FailsStartupNamingIdAndExceptionType()
    {
        var policy = CreatePolicy("throws-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(policy);
        _pap.AddPolicyAsync(policy, null, Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new TimeoutException("secret detail"));

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));
        ex.Message.ShouldContain("throws-p");
        ex.Message.ShouldContain(nameof(TimeoutException));
        ex.Message.ShouldNotContain("secret detail");
    }

    [Fact]
    public async Task StartAsync_WhenStartTokenIsCancelled_PropagatesOperationCanceledException()
    {
        var policy = CreatePolicy("cancel-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicies.Add(policy);
        using var cts = new CancellationTokenSource();
        _pap.AddPolicyAsync(policy, null, Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() => sut.StartAsync(cts.Token));
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        var options = Options.Create(new ABACOptions());
        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        await sut.StopAsync(CancellationToken.None); // Should not throw
    }

    [Fact]
    public async Task StartAsync_SeedsBothPolicySetsAndPolicies()
    {
        var policySet = CreatePolicySet("mixed-ps");
        var policy = CreatePolicy("mixed-p");
        var options = Options.Create(new ABACOptions());
        options.Value.SeedPolicySets.Add(policySet);
        options.Value.SeedPolicies.Add(policy);

        _pap.AddPolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
        _pap.AddPolicyAsync(Arg.Any<Policy>(), null, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ABACPolicySeedingHostedService(_pap, _scopes, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
        await sut.StartAsync(CancellationToken.None);

        await _pap.Received(1).AddPolicySetAsync(policySet, Arg.Any<CancellationToken>());
        await _pap.Received(1).AddPolicyAsync(policy, null, Arg.Any<CancellationToken>());
    }

    private static PolicySet CreatePolicySet(string id) => new()
    {
        Id = id,
        Policies = [],
        PolicySets = [],
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Obligations = [],
        Advice = []
    };

    private static Policy CreatePolicy(string id) => new()
    {
        Id = id,
        Rules = [],
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };
}
