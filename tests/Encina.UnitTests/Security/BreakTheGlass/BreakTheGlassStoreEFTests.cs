using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.BreakTheGlass;
using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>
/// Registration completeness, the model and the failure paths of the EF Core break-the-glass store: the database store wins
/// over the in-memory default in either registration order without overriding the application's own store, the provider builds
/// with <c>ValidateOnBuild</c> and <c>ValidateScopes</c> only when the context factory is registered, a provider failure is a
/// <c>Left</c> and a cancelled call still throws. The behavior against a real database is in the integration tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassStoreEFTests
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

    private sealed class BtgContext(DbContextOptions<BtgContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyBreakTheGlassConfiguration();
    }

    private sealed class ThrowingFactory(Exception exception) : IDbContextFactory<BtgContext>
    {
        public BtgContext CreateDbContext() => throw exception;

        public Task<BtgContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => throw exception;
    }

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

    private static void AddFactory(IServiceCollection services) =>
        services.AddDbContextFactory<BtgContext>(options => options.UseSqlServer("Server=127.0.0.1,1;Database=x;User Id=x;Password=x;Encrypt=False"));

    private static void AddProvider(IServiceCollection services, bool enabled = true) =>
        services.AddEncinaEntityFrameworkCore<BtgContext>(config => config.UseBreakTheGlassStore = enabled);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DatabaseStore_WinsOverTheInMemoryDefault_InEitherRegistrationOrder(bool breakTheGlassFirst)
    {
        var services = Base();
        AddFactory(services);
        if (breakTheGlassFirst)
        {
            AddBreakTheGlass(services);
            AddProvider(services);
        }
        else
        {
            AddProvider(services);
            AddBreakTheGlass(services);
        }

        using var serviceProvider = services.BuildServiceProvider(Validated);
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IBreakTheGlassStore>().ShouldBeOfType<BreakTheGlassStoreEF<BtgContext>>();
        services.Count(d => d.ServiceType == typeof(IBreakTheGlassStore)).ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplicationStore_IsNeverOverridden(bool applicationStoreFirst)
    {
        var services = Base();
        AddFactory(services);
        if (applicationStoreFirst)
        {
            services.AddScoped<IBreakTheGlassStore, ApplicationStore>();
            AddBreakTheGlass(services);
            AddProvider(services);
        }
        else
        {
            AddBreakTheGlass(services);
            AddProvider(services);
            services.AddScoped<IBreakTheGlassStore, ApplicationStore>();
        }

        using var serviceProvider = services.BuildServiceProvider(Validated);
        using var scope = serviceProvider.CreateScope();

        // A store registered by the application before the provider is kept; one registered after it is the last registration.
        scope.ServiceProvider.GetRequiredService<IBreakTheGlassStore>().ShouldBeOfType<ApplicationStore>();
    }

    [Fact]
    public void WithoutTheFlag_TheInMemoryDefaultStays()
    {
        var services = Base();
        AddFactory(services);
        AddBreakTheGlass(services);
        AddProvider(services, enabled: false);

        using var serviceProvider = services.BuildServiceProvider(Validated);

        serviceProvider.GetRequiredService<IBreakTheGlassStore>().ShouldBeOfType<InMemoryBreakTheGlassStore>();
    }

    [Fact]
    public void WithoutAContextFactory_TheProviderCannotBeBuilt()
    {
        var services = Base();
        services.AddDbContext<BtgContext>(options => options.UseSqlServer("Server=127.0.0.1,1;Database=x;User Id=x;Password=x;Encrypt=False"));
        AddBreakTheGlass(services);
        AddProvider(services);

        // The store's constructor requires IDbContextFactory<TContext>: ValidateOnBuild names the missing registration.
        Should.Throw<AggregateException>(() => services.BuildServiceProvider(Validated).Dispose())
            .ToString().ShouldContain(nameof(IDbContextFactory<BtgContext>));
    }

    [Fact]
    public async Task AFailingFactory_ReturnsStoreFailed_NeverThrows()
    {
        var store = new BreakTheGlassStoreEF<BtgContext>(new ThrowingFactory(new InvalidOperationException("boom")));

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

    [Fact]
    public async Task ACancelledCall_StillThrows()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var store = new BreakTheGlassStoreEF<BtgContext>(new ThrowingFactory(new OperationCanceledException(cancellation.Token)));

        await Should.ThrowAsync<OperationCanceledException>(async () => await store.ProbeAsync(cancellation.Token));
    }

    [Fact]
    public async Task ArgumentGuards_RejectNullsBeforeAnyDatabaseCall()
    {
        var store = new BreakTheGlassStoreEF<BtgContext>(new ThrowingFactory(new InvalidOperationException("never used")));

        Should.Throw<ArgumentNullException>(() => new BreakTheGlassStoreEF<BtgContext>(null!));
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.AddAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.FindEffectiveAsync(null, null!, "s", "c", DateTimeOffset.UnixEpoch));
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.TryCloseAsync(null, Guid.NewGuid(), BreakTheGlassStatus.Revoked, null!, DateTimeOffset.UnixEpoch));
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.TryRecordReviewAsync(null, Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, null!, null, DateTimeOffset.UnixEpoch));
        await Should.ThrowAsync<ArgumentNullException>(async () => await store.QueryReviewQueueAsync(null, null!));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await store.GetExpiredUnrecordedAsync(DateTimeOffset.UnixEpoch, 0));
    }

    [Fact]
    public void ActiveKeyOf_SeparatesTheKeyParts_AndTreatsANullTenantAsEmpty()
    {
        BreakTheGlassStoreEF<BtgContext>.ActiveKeyOf("t", "a", "bc", "s").ShouldNotBe(BreakTheGlassStoreEF<BtgContext>.ActiveKeyOf("t", "ab", "c", "s"));
        BreakTheGlassStoreEF<BtgContext>.ActiveKeyOf("t", "u", "s", "c").ShouldNotBe(BreakTheGlassStoreEF<BtgContext>.ActiveKeyOf("T", "u", "s", "c"));
        BreakTheGlassStoreEF<BtgContext>.ActiveKeyOf(null, "u", "s", "c").ShouldBe(BreakTheGlassStoreEF<BtgContext>.ActiveKeyOf(string.Empty, "u", "s", "c"));
        BreakTheGlassStoreEF<BtgContext>.ActiveKeyOf("t", "u", "s", "c").Length.ShouldBe(32);
    }

    [Fact]
    public void IsActiveKeyViolation_OnlyForASqlServerDuplicateKeyOnTheActiveKeyIndex()
    {
        BreakTheGlassStoreEF<BtgContext>.IsActiveKeyViolation(new DbUpdateException("x", new InvalidOperationException("y"))).ShouldBeFalse();
        BreakTheGlassStoreEF<BtgContext>.IsActiveKeyViolation(new DbUpdateException("x")).ShouldBeFalse();
    }

    [Fact]
    public void Model_MapsTheShippedTable()
    {
        using var context = new BtgContext(new DbContextOptionsBuilder<BtgContext>().UseSqlServer("Server=127.0.0.1,1;Database=x").Options);
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(BreakTheGlassElevationEntity))!;

        entity.GetTableName().ShouldBe(BreakTheGlassElevationEntityConfiguration.TableName);
        entity.FindProperty(nameof(BreakTheGlassElevationEntity.UserId))!.GetCollation().ShouldBe(BreakTheGlassElevationEntityConfiguration.KeyCollation);
        entity.FindProperty(nameof(BreakTheGlassElevationEntity.Justification))!.GetMaxLength().ShouldBe(BreakTheGlassLimits.JustificationMaxLength);
        var activeKey = entity.FindProperty(nameof(BreakTheGlassElevationEntity.ActiveKey))!;
        activeKey.GetComputedColumnSql().ShouldNotBeNullOrEmpty();
        activeKey.GetIsStored().ShouldBe(true);
        entity.GetIndexes().Single(i => i.GetDatabaseName() == BreakTheGlassElevationEntityConfiguration.ActiveKeyIndexName).IsUnique.ShouldBeTrue();
        entity.GetIndexes().Count().ShouldBe(3);
    }

    [Fact]
    public void ModelBuilderExtension_RejectsANullBuilder() =>
        Should.Throw<ArgumentNullException>(() => BreakTheGlassModelBuilderExtensions.ApplyBreakTheGlassConfiguration(null!));

    [Fact]
    public void Configuration_RejectsANullBuilder() =>
        Should.Throw<ArgumentNullException>(() => new BreakTheGlassElevationEntityConfiguration().Configure(null!));
}
