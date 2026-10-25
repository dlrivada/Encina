using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Security.BreakTheGlass.Health;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>
/// Registration completeness of <c>AddEncinaBreakTheGlass</c>: the provider builds with
/// <c>ValidateOnBuild</c> and <c>ValidateScopes</c>, the gate wins over the core default in any order
/// without overriding an application's gate, and a database store wins over the in-memory default.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassRegistrationTests
{
    private static readonly ServiceProviderOptions Validated = new() { ValidateOnBuild = true, ValidateScopes = true };

    /// <summary>A stand-in for a database store: only the type matters.</summary>
    private sealed class DatabaseStore : IBreakTheGlassStore
    {
        private readonly InMemoryBreakTheGlassStore _inner = new();

        public ValueTask<Either<EncinaError, Unit>> AddAsync(BreakTheGlassElevation elevation, CancellationToken cancellationToken = default) => _inner.AddAsync(elevation, cancellationToken);

        public ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> GetByIdAsync(string? tenantId, Guid elevationId, CancellationToken cancellationToken = default) => _inner.GetByIdAsync(tenantId, elevationId, cancellationToken);

        public ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> FindEffectiveAsync(string? tenantId, string userId, string subjectId, string scope, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => _inner.FindEffectiveAsync(tenantId, userId, subjectId, scope, nowUtc, cancellationToken);

        public ValueTask<Either<EncinaError, bool>> TryCloseAsync(string? tenantId, Guid elevationId, BreakTheGlassStatus newStatus, string reason, DateTimeOffset closedAtUtc, CancellationToken cancellationToken = default) => _inner.TryCloseAsync(tenantId, elevationId, newStatus, reason, closedAtUtc, cancellationToken);

        public ValueTask<Either<EncinaError, IReadOnlyList<BreakTheGlassElevation>>> GetExpiredUnrecordedAsync(DateTimeOffset nowUtc, int batchSize, CancellationToken cancellationToken = default) => _inner.GetExpiredUnrecordedAsync(nowUtc, batchSize, cancellationToken);

        public ValueTask<Either<EncinaError, Unit>> MarkExpiryRecordedAsync(string? tenantId, Guid elevationId, DateTimeOffset recordedAtUtc, CancellationToken cancellationToken = default) => _inner.MarkExpiryRecordedAsync(tenantId, elevationId, recordedAtUtc, cancellationToken);

        public ValueTask<Either<EncinaError, bool>> TryRecordReviewAsync(string? tenantId, Guid elevationId, BreakTheGlassReviewOutcome outcome, string reviewerUserId, string? note, DateTimeOffset reviewedAtUtc, CancellationToken cancellationToken = default) => _inner.TryRecordReviewAsync(tenantId, elevationId, outcome, reviewerUserId, note, reviewedAtUtc, cancellationToken);

        public ValueTask<Either<EncinaError, PagedResult<BreakTheGlassElevation>>> QueryReviewQueueAsync(string? tenantId, BreakTheGlassReviewQuery query, CancellationToken cancellationToken = default) => _inner.QueryReviewQueueAsync(tenantId, query, cancellationToken);

        public ValueTask<Either<EncinaError, int>> PurgeAsync(DateTimeOffset reviewedBeforeUtc, CancellationToken cancellationToken = default) => _inner.PurgeAsync(reviewedBeforeUtc, cancellationToken);

        public ValueTask<Either<EncinaError, Unit>> ProbeAsync(CancellationToken cancellationToken = default) => _inner.ProbeAsync(cancellationToken);
    }

    private sealed class ApplicationGate : IAccessElevationGate
    {
        public ValueTask<Either<EncinaError, Option<AccessElevation>>> TryElevateAsync<TRequest>(
            TRequest request, IRequestContext context, EncinaError denial, CancellationToken cancellationToken)
            where TRequest : notnull =>
            ValueTask.FromResult<Either<EncinaError, Option<AccessElevation>>>(Option<AccessElevation>.None);
    }

    private static void Configure(BreakTheGlassOptions options)
    {
        options.DeclareScope("clinical-record", scope => scope.OpenerRoles.Add("clinician"));
        options.ReviewerRoles.Add("reviewer");
    }

    private static ServiceCollection Base()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>();
        return services;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildsValidated_AndResolvesTheBreakTheGlassGate_InEitherOrderWithAddEncina(bool encinaFirst)
    {
        var services = Base();
        if (encinaFirst)
        {
            services.AddEncina();
            services.AddEncinaBreakTheGlass(Configure);
        }
        else
        {
            services.AddEncinaBreakTheGlass(Configure);
            services.AddEncina();
        }

        using var provider = services.BuildServiceProvider(Validated);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAccessElevationGate>().ShouldBeOfType<BreakTheGlassAccessGate>();
        scope.ServiceProvider.GetRequiredService<IBreakTheGlassService>().ShouldBeOfType<DefaultBreakTheGlassService>();
        scope.ServiceProvider.GetRequiredService<IBreakTheGlassStore>().ShouldBeOfType<InMemoryBreakTheGlassStore>();
        scope.ServiceProvider.GetRequiredService<IOptions<BreakTheGlassOptions>>().Value.Scopes.ShouldContainKey("clinical-record");
        services.Count(d => d.ServiceType == typeof(IAccessElevationGate)).ShouldBe(1);
    }

    [Fact]
    public void ApplicationGate_IsNeverOverridden()
    {
        var services = Base();
        services.AddEncina();
        services.AddSingleton<IAccessElevationGate, ApplicationGate>();
        services.AddEncinaBreakTheGlass(Configure);

        using var provider = services.BuildServiceProvider(Validated);

        provider.GetRequiredService<IAccessElevationGate>().ShouldBeOfType<ApplicationGate>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DatabaseStore_WinsOverTheInMemoryDefault_InAnyOrder(bool databaseFirst)
    {
        var services = Base();
        services.AddEncina();
        if (databaseFirst)
        {
            services.AddSingleton<IBreakTheGlassStore, DatabaseStore>();
            services.AddEncinaBreakTheGlass(Configure);
        }
        else
        {
            services.AddEncinaBreakTheGlass(Configure);
            // What a provider's RemoveInMemoryDefault helper does: drop the default by type, then add its store.
            services.RemoveAll<IBreakTheGlassStore>();
            services.AddSingleton<IBreakTheGlassStore, DatabaseStore>();
        }

        using var provider = services.BuildServiceProvider(Validated);

        provider.GetRequiredService<IBreakTheGlassStore>().ShouldBeOfType<DatabaseStore>();
    }

    [Fact]
    public void HostedServices_AreRegisteredOnce_PurgeOnlyWhenEnabled()
    {
        var services = Base();
        services.AddEncina();
        services.AddEncinaBreakTheGlass(Configure);
        services.AddEncinaBreakTheGlass(Configure);

        var hosted = services.Where(d => d.ServiceType == typeof(IHostedService)).Select(d => d.ImplementationType).ToList();

        hosted.Count(t => t == typeof(BreakTheGlassExpiryService)).ShouldBe(1);
        hosted.Count(t => t == typeof(BreakTheGlassStartupValidator)).ShouldBe(1);
        hosted.ShouldNotContain(typeof(BreakTheGlassPurgeService));
        hosted.IndexOf(typeof(BreakTheGlassStartupValidator)).ShouldBeLessThan(hosted.IndexOf(typeof(BreakTheGlassExpiryService)));

        var withPurge = Base();
        withPurge.AddEncina();
        withPurge.AddEncinaBreakTheGlass(o =>
        {
            Configure(o);
            o.EnableAutoPurge = true;
        });
        withPurge.Any(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(BreakTheGlassPurgeService)).ShouldBeTrue();
    }

    [Fact]
    public void HealthCheck_IsRegisteredOnlyWhenRequested_AndResolves()
    {
        var off = Base();
        off.AddEncina();
        off.AddEncinaBreakTheGlass(Configure);
        using (var provider = off.BuildServiceProvider(Validated))
        {
            provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.ShouldBeEmpty();
        }

        var on = Base();
        on.AddEncina();
        on.AddEncinaBreakTheGlass(o =>
        {
            Configure(o);
            o.AddHealthCheck = true;
        });
        using var withCheck = on.BuildServiceProvider(Validated);
        var registration = withCheck.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.ShouldHaveSingleItem();
        registration.Name.ShouldBe(BreakTheGlassHealthCheck.DefaultName);
        registration.Tags.ShouldContain("ready");
        registration.Factory(withCheck).ShouldBeOfType<BreakTheGlassHealthCheck>();
    }

    [Fact]
    public void NoAuditStoreIsRegistered_ByThePackage()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddEncinaBreakTheGlass(Configure);

        services.Any(d => d.ServiceType == typeof(IOperationAuditStore)).ShouldBeFalse();
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddEncinaBreakTheGlass(Configure));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddEncinaBreakTheGlass(null!));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddEncinaBreakTheGlass(Configure, null!));
    }

    [Fact]
    public void InvalidOptions_FailOnStartValidation()
    {
        var services = Base();
        services.AddEncina();
        services.AddEncinaBreakTheGlass(_ => { });

        using var provider = services.BuildServiceProvider(Validated);

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BreakTheGlassOptions>>().Value);
    }
}
