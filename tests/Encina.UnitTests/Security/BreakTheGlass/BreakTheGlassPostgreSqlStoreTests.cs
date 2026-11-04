using System.Data;
using Encina.ADO.PostgreSQL;
using Encina.ADO.PostgreSQL.BreakTheGlass;
using Encina.Dapper.PostgreSQL;
using Encina.Dapper.PostgreSQL.BreakTheGlass;
using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>
/// Registration completeness and failure handling of the PostgreSQL break-the-glass stores (ADO.NET and Dapper):
/// the database store wins over the in-memory default in either registration order without overriding the
/// application's own store, the provider builds with <c>ValidateOnBuild</c> and <c>ValidateScopes</c>, a provider
/// failure is a <c>Left</c> and never an exception. The behavior against a real database is in the integration tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassPostgreSqlStoreTests
{
    private static readonly ServiceProviderOptions Validated = new() { ValidateOnBuild = true, ValidateScopes = true };

    private static readonly BreakTheGlassElevation Elevation = new()
    {
        ElevationId = Guid.NewGuid(),
        UserId = "clin-1",
        SubjectId = "patient-1",
        Scope = "clinical-record",
        Justification = "Emergency.",
        OpenedAtUtc = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero),
        ExpiresAtUtc = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero)
    };

    private sealed class ApplicationStore : IBreakTheGlassStore
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

    public static TheoryData<string, bool> Providers => new()
    {
        { "ADO", true },
        { "ADO", false },
        { "Dapper", true },
        { "Dapper", false }
    };

    private static ServiceCollection Base()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>();
        services.AddEncina();
        return services;
    }

    private static void AddBreakTheGlass(IServiceCollection services) =>
        services.AddEncinaBreakTheGlass(options =>
        {
            options.DeclareScope("clinical-record", scope => scope.OpenerRoles.Add("clinician"));
            options.ReviewerRoles.Add("reviewer");
        });

    private static void AddProvider(IServiceCollection services, string provider, bool enabled)
    {
        const string ConnectionString = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x";
        Func<IServiceProvider, IDbConnection> factory = _ => new NpgsqlConnection(ConnectionString);
        if (provider == "ADO")
        {
            services.AddEncinaADO(factory, config => config.UseBreakTheGlassStore = enabled);
        }
        else
        {
            services.AddEncinaDapper(factory, config => config.UseBreakTheGlassStore = enabled);
        }
    }

    private static Type DatabaseStoreOf(string provider) =>
        provider == "ADO" ? typeof(BreakTheGlassStoreADO) : typeof(BreakTheGlassStoreDapper);

    private static IBreakTheGlassStore NewStore(string provider, IDbConnection connection) =>
        provider == "ADO" ? new BreakTheGlassStoreADO(connection) : new BreakTheGlassStoreDapper(connection);

    [Theory]
    [MemberData(nameof(Providers))]
    public void DatabaseStore_WinsOverTheInMemoryDefault_InEitherRegistrationOrder(string provider, bool breakTheGlassFirst)
    {
        var services = Base();
        if (breakTheGlassFirst)
        {
            AddBreakTheGlass(services);
            AddProvider(services, provider, enabled: true);
        }
        else
        {
            AddProvider(services, provider, enabled: true);
            AddBreakTheGlass(services);
        }

        using var serviceProvider = services.BuildServiceProvider(Validated);
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IBreakTheGlassStore>().ShouldBeOfType(DatabaseStoreOf(provider));
        services.Count(d => d.ServiceType == typeof(IBreakTheGlassStore)).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void ApplicationStore_IsNeverOverridden(string provider, bool applicationStoreFirst)
    {
        var services = Base();
        if (applicationStoreFirst)
        {
            services.AddScoped<IBreakTheGlassStore, ApplicationStore>();
            AddBreakTheGlass(services);
            AddProvider(services, provider, enabled: true);
        }
        else
        {
            AddBreakTheGlass(services);
            AddProvider(services, provider, enabled: true);
            services.AddScoped<IBreakTheGlassStore, ApplicationStore>();
        }

        using var serviceProvider = services.BuildServiceProvider(Validated);
        using var scope = serviceProvider.CreateScope();

        // A store registered by the application before the provider is kept; one registered after it is the last registration.
        scope.ServiceProvider.GetRequiredService<IBreakTheGlassStore>().ShouldBeOfType<ApplicationStore>();
    }

    [Theory]
    [InlineData("ADO")]
    [InlineData("Dapper")]
    public void WithoutTheFlag_TheInMemoryDefaultStays(string provider)
    {
        var services = Base();
        AddBreakTheGlass(services);
        AddProvider(services, provider, enabled: false);

        using var serviceProvider = services.BuildServiceProvider(Validated);

        serviceProvider.GetRequiredService<IBreakTheGlassStore>().ShouldBeOfType<InMemoryBreakTheGlassStore>();
    }

    [Theory]
    [InlineData("ADO")]
    [InlineData("Dapper")]
    public async Task AnUnusableConnection_ReturnsStoreFailed_NeverThrows(string provider)
    {
        // A closed connection that is not a DbConnection cannot be opened asynchronously.
        var store = NewStore(provider, Substitute.For<IDbConnection>());

        var results = new[]
        {
            (await store.AddAsync(Elevation)).Map(_ => 0),
            (await store.GetByIdAsync("t", Guid.NewGuid())).Map(_ => 0),
            (await store.FindEffectiveAsync(null, "u", "s", "c", DateTimeOffset.UnixEpoch)).Map(_ => 0),
            (await store.TryCloseAsync(null, Guid.NewGuid(), BreakTheGlassStatus.Revoked, "r", DateTimeOffset.UnixEpoch)).Map(_ => 0),
            (await store.GetExpiredUnrecordedAsync(DateTimeOffset.UnixEpoch, 5)).Map(_ => 0),
            (await store.MarkExpiryRecordedAsync(null, Guid.NewGuid(), DateTimeOffset.UnixEpoch)).Map(_ => 0),
            (await store.TryRecordReviewAsync(null, Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, "r", null, DateTimeOffset.UnixEpoch)).Map(_ => 0),
            (await store.QueryReviewQueueAsync(null, new BreakTheGlassReviewQuery { Scope = "c", Status = BreakTheGlassStatus.Active, OpenedFromUtc = DateTimeOffset.UnixEpoch, OpenedToUtc = DateTimeOffset.UnixEpoch })).Map(_ => 0),
            (await store.PurgeAsync(DateTimeOffset.UnixEpoch)).Map(_ => 0),
            (await store.ProbeAsync()).Map(_ => 0)
        };

        results.ShouldAllBe(result => result.IsLeft);
        results.ShouldAllBe(result => result.Match(Right: _ => string.Empty, Left: error => error.GetCode().IfNone(string.Empty)) == BreakTheGlassErrors.StoreFailedCode);
    }

    [Theory]
    [InlineData("ADO")]
    [InlineData("Dapper")]
    public async Task AFailureOfACancelledCall_Propagates_InsteadOfReturningStoreFailed(string provider)
    {
        var store = NewStore(provider, Substitute.For<IDbConnection>());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<Exception>(async () => await store.ProbeAsync(cancelled.Token));
    }
}
