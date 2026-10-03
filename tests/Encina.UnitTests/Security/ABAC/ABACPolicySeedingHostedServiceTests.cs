#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute .Returns() pattern for ValueTask
using Encina.Security.ABAC;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Tests for <see cref="ABACPolicySeedingHostedService"/>.
/// </summary>
public sealed class ABACPolicySeedingHostedServiceTests
{
    private readonly IPolicyAdministrationPoint _pap = Substitute.For<IPolicyAdministrationPoint>();

    [Fact]
    public void Constructor_WithNullPap_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ABACPolicySeedingHostedService(
            null!,
            Options.Create(new ABACOptions()),
            NullLogger<ABACPolicySeedingHostedService>.Instance));
    }

    [Fact]
    public async Task StartAsync_WithNoSeedData_DoesNotCallPap()
    {
        var options = Options.Create(new ABACOptions());
        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
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

        var sut = new ABACPolicySeedingHostedService(_pap, options, logger);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, logger);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() => sut.StartAsync(cts.Token));
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        var options = Options.Create(new ABACOptions());
        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);

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

        var sut = new ABACPolicySeedingHostedService(_pap, options, NullLogger<ABACPolicySeedingHostedService>.Instance);
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
