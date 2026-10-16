using System.Data;

using Encina.EntityFrameworkCore.Auditing;
using Encina.IntegrationTests.Security.ABAC.DecisionAudit;
using Encina.IntegrationTests.Security.Audit.EFCore;
using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

using MongoDB.Driver;

using AdoSqlServerStore = Encina.ADO.SqlServer.Auditing.OperationAuditStoreADO;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// The audit write of <see cref="AuditPipelineBehavior{TRequest, TResponse}"/> is awaited and runs in a
/// child scope of its own (#1869): when Handle returns, the entry is already readable through a store
/// from another scope, on a real database. Each provider family runs the same test through its shared
/// collection.
/// </summary>
public abstract class AuditPipelineBehaviorAwaitedWriteTestsBase
{
    /// <summary>Registers the provider's scoped <see cref="IOperationAuditStore"/> (one instance per DI scope).</summary>
    protected abstract void RegisterScopedStore(IServiceCollection services);

    /// <summary>Whether the provider's database is reachable; an unavailable one skips the test.</summary>
    protected virtual bool IsAvailable => true;

    [Fact]
    public async Task Handle_Command_EntryIsPersistedWhenHandleReturns()
    {
        if (!IsAvailable)
        {
            Assert.Skip("The provider's database is not available.");
        }

        var entityType = $"Awaited{Guid.NewGuid():N}";
        var entityId = Guid.NewGuid().ToString();
        var factory = Substitute.For<IOperationAuditEntryFactory>();
        factory.Create(
                Arg.Any<object>(), Arg.Any<object?>(), Arg.Any<IRequestContext>(), Arg.Any<AuditOutcome>(),
                Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
            .Returns(_ => new OperationAuditEntry
            {
                Id = Guid.NewGuid(),
                CorrelationId = $"corr-{Guid.NewGuid():N}",
                UserId = "integration-user",
                Action = "Create",
                EntityType = entityType,
                EntityId = entityId,
                Outcome = AuditOutcome.Success,
                TimestampUtc = DateTime.UtcNow,
                StartedAtUtc = DateTimeOffset.UtcNow,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object?>()
            });

        var services = new ServiceCollection();
        RegisterScopedStore(services);
        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var sut = new AuditPipelineBehavior<AwaitedWriteCommand, LanguageExt.Unit>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            factory,
            Options.Create(new OperationAuditOptions { AuditAllCommands = true }),
            NullLogger<AuditPipelineBehavior<AwaitedWriteCommand, LanguageExt.Unit>>.Instance);

        var result = await sut.Handle(
            new AwaitedWriteCommand(),
            RequestContext.CreateForTest(),
            () => new ValueTask<Either<EncinaError, LanguageExt.Unit>>(LanguageExt.Unit.Default),
            TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();

        await using var readScope = provider.CreateAsyncScope();
        var stored = await readScope.ServiceProvider.GetRequiredService<IOperationAuditStore>()
            .GetByEntityAsync(entityType, entityId, TestContext.Current.CancellationToken);

        stored.Match(Right: entries => entries.Count, Left: _ => -1).ShouldBe(1);
    }

    public sealed record AwaitedWriteCommand : ICommand<LanguageExt.Unit>;
}

/// <summary>ADO.NET on SQL Server: a connection per DI scope, disposed with the scope.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class AuditPipelineBehaviorAwaitedWriteADOSqlServerTests(SqlServerFixture fixture)
    : AuditPipelineBehaviorAwaitedWriteTestsBase, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        using var connection = fixture.CreateConnection();
        await OperationAuditTable.PrepareAsync(
            connection, "Encina.ADO.SqlServer", splitBatches: true, "DELETE FROM OperationAuditEntries;");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected override void RegisterScopedStore(IServiceCollection services)
    {
        services.AddScoped<IDbConnection>(_ => fixture.CreateConnection());
        services.AddScoped<IOperationAuditStore>(sp => new AdoSqlServerStore(sp.GetRequiredService<IDbConnection>()));
    }
}

[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class AuditPipelineBehaviorAwaitedWriteEFSqlServerTests(EFCoreSqlServerFixture fixture)
    : AuditPipelineBehaviorAwaitedWriteTestsBase, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await fixture.EnsureSchemaCreatedAsync<AuditTestDbContext>();
        await fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected override void RegisterScopedStore(IServiceCollection services)
    {
        services.AddScoped(_ => fixture.CreateDbContext<AuditTestDbContext>());
        services.AddScoped<IOperationAuditStore>(sp => new OperationAuditStoreEF(sp.GetRequiredService<AuditTestDbContext>()));
    }
}

[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class AuditPipelineBehaviorAwaitedWriteMongoDBTests(MongoDbFixture fixture)
    : AuditPipelineBehaviorAwaitedWriteTestsBase, IAsyncLifetime
{
    private readonly IOptions<EncinaMongoDbOptions> _options =
        Options.Create(new EncinaMongoDbOptions { DatabaseName = MongoDbFixture.DatabaseName });

    protected override bool IsAvailable => fixture.IsAvailable;

    public async ValueTask InitializeAsync()
    {
        if (fixture.IsAvailable)
        {
            await fixture.Database!.GetCollection<OperationAuditEntryDocument>(_options.Value.Collections.OperationAuditEntries)
                .DeleteManyAsync(Builders<OperationAuditEntryDocument>.Filter.Empty);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected override void RegisterScopedStore(IServiceCollection services) =>
        services.AddScoped<IOperationAuditStore>(_ =>
            new OperationAuditStoreMongoDB(fixture.Client!, _options, NullLogger<OperationAuditStoreMongoDB>.Instance));
}
