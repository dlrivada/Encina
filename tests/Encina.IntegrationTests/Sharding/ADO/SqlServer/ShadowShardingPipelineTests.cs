using Encina.ADO.SqlServer.Sharding;
using Encina.Sharding;
using Encina.Sharding.Shadow;
using Encina.TestInfrastructure.Fixtures.Sharding;

using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Sharding.ADO.SqlServer;

/// <summary>
/// Integration tests proving that, with shadow sharding enabled, the shadow write and the shadow
/// read comparison actually run in the Encina pipeline of a command and a query that use real
/// sharded SQL Server databases.
/// </summary>
[Collection("Sharding-ADO-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class ShadowShardingPipelineTests : IAsyncLifetime
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(15);

    private readonly ShardedSqlServerFixture _fixture;
    private readonly TaskCompletionSource<bool> _shadowRouted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<ShadowComparisonResult> _discrepancy =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private ServiceProvider _serviceProvider = null!;

    public ShadowShardingPipelineTests(ShardedSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped<IRequestHandler<AddShardedEntity, string>, AddShardedEntityHandler>();
        services.AddScoped<IRequestHandler<GetShardedEntityName, string>, GetShardedEntityNameHandler>();

        services.AddEncinaSharding<ShardedTestEntity>(options =>
        {
            options.UseHashRouting()
                .AddShard("shard-1", _fixture.Shard1ConnectionString)
                .AddShard("shard-2", _fixture.Shard2ConnectionString)
                .AddShard("shard-3", _fixture.Shard3ConnectionString);

            options.WithShadowSharding(shadow =>
            {
                shadow.ShadowTopology = new ShardTopology(
                [
                    new ShardInfo("shadow-1", _fixture.Shard1ConnectionString),
                    new ShardInfo("shadow-2", _fixture.Shard2ConnectionString),
                ]);
                shadow.DualWriteEnabled = true;
                shadow.ShadowReadPercentage = 100;
                shadow.CompareResults = true;

                // Always route to a shard id that production never uses, so every comparison
                // is a routing mismatch the discrepancy handler can observe.
                shadow.ShadowRouterFactory = _ =>
                {
                    var router = Substitute.For<IShardRouter>();
                    router.GetShardId(Arg.Any<string>()).Returns(_ =>
                    {
                        _shadowRouted.TrySetResult(true);
                        return Right<EncinaError, string>("shadow-only");
                    });
                    return router;
                };
                shadow.DiscrepancyHandler = (comparison, _, _) =>
                {
                    _discrepancy.TrySetResult(comparison);
                    return Task.CompletedTask;
                };
            });
        });

        services.AddEncinaADOSharding<ShardedTestEntity, string>(mapping =>
        {
            mapping.ToTable("ShardedEntities")
                .HasId(e => e.Id)
                .MapProperty(e => e.ShardKey, "ShardKey")
                .MapProperty(e => e.Name, "Name")
                .MapProperty(e => e.Value, "Value")
                .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc");
        });

        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    public ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Send_Command_PersistsToProductionAndFiresTheShadowWrite()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var id = Guid.NewGuid().ToString();

        // Act
        var result = await encina.Send(
            new AddShardedEntity(id, "shadow-write-key", "Shadow Write"),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsRight.ShouldBeTrue();
        (await _shadowRouted.Task.WaitAsync(SignalTimeout, TestContext.Current.CancellationToken)).ShouldBeTrue();

        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();
        (await repo.GetByIdAsync(id, "shadow-write-key")).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Send_Query_ComparesProductionAndShadowRouting()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();
        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "shadow-read-key",
            Name = "Shadow Read",
        };
        (await repo.AddAsync(entity)).IsRight.ShouldBeTrue();

        // Act
        var result = await encina.Send(
            new GetShardedEntityName(entity.Id, entity.ShardKey),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsRight.ShouldBeTrue();
        var comparison = await _discrepancy.Task.WaitAsync(SignalTimeout, TestContext.Current.CancellationToken);
        comparison.RoutingMatch.ShouldBeFalse();
        comparison.ShadowShardId.ShouldBe("shadow-only");
    }

    public sealed record AddShardedEntity(string Id, string ShardKey, string Name) : ICommand<string>;

    public sealed record GetShardedEntityName(string Id, string ShardKey) : IQuery<string>;

    private sealed class AddShardedEntityHandler(
        IFunctionalShardedRepository<ShardedTestEntity, string> repository)
        : IRequestHandler<AddShardedEntity, string>
    {
        public async Task<Either<EncinaError, string>> Handle(
            AddShardedEntity request, CancellationToken cancellationToken)
        {
            var entity = new ShardedTestEntity
            {
                Id = request.Id,
                ShardKey = request.ShardKey,
                Name = request.Name,
            };

            var added = await repository.AddAsync(entity, cancellationToken);
            return added.Map(_ => request.Id);
        }
    }

    private sealed class GetShardedEntityNameHandler(
        IFunctionalShardedRepository<ShardedTestEntity, string> repository)
        : IRequestHandler<GetShardedEntityName, string>
    {
        public async Task<Either<EncinaError, string>> Handle(
            GetShardedEntityName request, CancellationToken cancellationToken)
        {
            var found = await repository.GetByIdAsync(request.Id, request.ShardKey, cancellationToken);
            return found.Map(entity => entity.Name);
        }
    }
}
