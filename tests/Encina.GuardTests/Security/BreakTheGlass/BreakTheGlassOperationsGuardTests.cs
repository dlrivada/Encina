using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Security.BreakTheGlass.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.BreakTheGlass;

/// <summary>Guard tests of the Phase 4 types: null dependencies and arguments name their parameter.</summary>
public sealed class BreakTheGlassOperationsGuardTests
{
    private static readonly IOptions<BreakTheGlassOptions> Opts = Options.Create(new BreakTheGlassOptions());
    private static readonly IServiceScopeFactory Scopes = Substitute.For<IServiceScopeFactory>();
    private static readonly IInternalRequestContextScopeFactory Internal = Substitute.For<IInternalRequestContextScopeFactory>();

    private static void Param(Func<object?> act, string name) => Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(name);

    [Fact]
    public void Gate_NullLogger() => Param(() => new BreakTheGlassAccessGate(
        new InMemoryBreakTheGlassStore(), new InMemoryOperationAuditStore(), new ReadAuditContext(), new BreakTheGlassRequestGrants(), Opts, TimeProvider.System, null!), "logger");

    [Fact]
    public void OptionsValidator_NullOptions() => Param(() => new BreakTheGlassOptionsValidator().Validate(null, null!), "options");

    [Fact]
    public void ExpiryService_RejectsEachNullDependency()
    {
        var state = new BreakTheGlassSweepState();
        var logger = NullLogger<BreakTheGlassExpiryService>.Instance;
        Param(() => new BreakTheGlassExpiryService(null!, Internal, Opts, state, TimeProvider.System, logger), "scopeFactory");
        Param(() => new BreakTheGlassExpiryService(Scopes, null!, Opts, state, TimeProvider.System, logger), "scopes");
        Param(() => new BreakTheGlassExpiryService(Scopes, Internal, null!, state, TimeProvider.System, logger), "options");
        Param(() => new BreakTheGlassExpiryService(Scopes, Internal, Opts, null!, TimeProvider.System, logger), "state");
        Param(() => new BreakTheGlassExpiryService(Scopes, Internal, Opts, state, null!, logger), "timeProvider");
        Param(() => new BreakTheGlassExpiryService(Scopes, Internal, Opts, state, TimeProvider.System, null!), "logger");
    }

    [Fact]
    public void PurgeService_RejectsEachNullDependency()
    {
        var logger = NullLogger<BreakTheGlassPurgeService>.Instance;
        Param(() => new BreakTheGlassPurgeService(null!, Opts, TimeProvider.System, logger), "scopeFactory");
        Param(() => new BreakTheGlassPurgeService(Scopes, null!, TimeProvider.System, logger), "options");
        Param(() => new BreakTheGlassPurgeService(Scopes, Opts, null!, logger), "timeProvider");
        Param(() => new BreakTheGlassPurgeService(Scopes, Opts, TimeProvider.System, null!), "logger");
    }

    [Fact]
    public void StartupValidator_RejectsEachNullDependency()
    {
        var provider = Substitute.For<IServiceProvider>();
        var targets = new BreakTheGlassScanTargets([]);
        var logger = NullLogger<BreakTheGlassStartupValidator>.Instance;
        Param(() => new BreakTheGlassStartupValidator(null!, Scopes, Opts, targets, logger), "serviceProvider");
        Param(() => new BreakTheGlassStartupValidator(provider, null!, Opts, targets, logger), "scopeFactory");
        Param(() => new BreakTheGlassStartupValidator(provider, Scopes, null!, targets, logger), "options");
        Param(() => new BreakTheGlassStartupValidator(provider, Scopes, Opts, null!, logger), "targets");
        Param(() => new BreakTheGlassStartupValidator(provider, Scopes, Opts, targets, null!), "logger");
    }

    [Fact]
    public void HealthCheck_RejectsEachNullDependency()
    {
        var provider = Substitute.For<IServiceProvider>();
        Param(() => new BreakTheGlassHealthCheck(null!, Opts, TimeProvider.System), "serviceProvider");
        Param(() => new BreakTheGlassHealthCheck(provider, null!, TimeProvider.System), "options");
        Param(() => new BreakTheGlassHealthCheck(provider, Opts, null!), "timeProvider");
    }

    [Fact]
    public void AddEncinaBreakTheGlass_RejectsNullArguments()
    {
        Param(() => ((IServiceCollection)null!).AddEncinaBreakTheGlass(_ => { }), "services");
        Param(() => new ServiceCollection().AddEncinaBreakTheGlass(null!), "configure");
        Param(() => new ServiceCollection().AddEncinaBreakTheGlass(_ => { }, null!), "assembliesToScan");
    }
}
