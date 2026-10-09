using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Projections;
using Encina.EntityFrameworkCore.Auditing;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.IntegrationTests.Security.Audit.EFCore;
using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

using JasperFx.Events.Daemon;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Encina.IntegrationTests.Security.ABAC.DecisionAudit;

[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class ABACDecisionAuditEFSqlServerRoundTripTests(EFCoreSqlServerFixture fixture) : ABACDecisionAuditRoundTripTestsBase, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await fixture.EnsureSchemaCreatedAsync<AuditTestDbContext>();
        await fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected override IOperationAuditStore CreateStore() => new OperationAuditStoreEF(fixture.CreateDbContext<AuditTestDbContext>());
}

[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class ABACDecisionAuditEFPostgreSqlRoundTripTests(EFCorePostgreSqlFixture fixture) : ABACDecisionAuditRoundTripTestsBase, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await fixture.EnsureSchemaCreatedAsync<AuditTestDbContext>();
        await fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected override IOperationAuditStore CreateStore() => new OperationAuditStoreEF(fixture.CreateDbContext<AuditTestDbContext>());
}

/// <summary>EF Core on MySQL: skipped by the fixture until Pomelo ships EF Core 10 (#2086), like the rest of the EF Core MySQL suite.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class ABACDecisionAuditEFMySqlRoundTripTests(EFCoreMySqlFixture fixture) : ABACDecisionAuditRoundTripTestsBase, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await fixture.EnsureSchemaCreatedAsync<AuditTestDbContext>();
        await fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected override bool IsAvailable => fixture.IsAvailable;

    protected override IOperationAuditStore CreateStore() => new OperationAuditStoreEF(fixture.CreateDbContext<AuditTestDbContext>());
}

[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ABACDecisionAuditMongoDBRoundTripTests(MongoDbFixture fixture) : ABACDecisionAuditRoundTripTestsBase, IAsyncLifetime
{
    private readonly IOptions<EncinaMongoDbOptions> _options = Options.Create(new EncinaMongoDbOptions { DatabaseName = MongoDbFixture.DatabaseName });

    public async ValueTask InitializeAsync()
    {
        if (fixture.IsAvailable)
        {
            await fixture.Database!.GetCollection<OperationAuditEntryDocument>(_options.Value.Collections.OperationAuditEntries)
                .DeleteManyAsync(Builders<OperationAuditEntryDocument>.Filter.Empty);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected override bool IsAvailable => fixture.IsAvailable;

    protected override IOperationAuditStore CreateStore() =>
        new OperationAuditStoreMongoDB(fixture.Client!, _options, NullLogger<OperationAuditStoreMongoDB>.Instance);
}

/// <summary>
/// Marten (outside the ten database providers): the store appends encrypted events and reads from an
/// asynchronous projection, so the test runs the projection daemon and waits for it before reading.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class ABACDecisionAuditMartenRoundTripTests(MartenFixture fixture) : ABACDecisionAuditRoundTripTestsBase, IAsyncLifetime
{
    private readonly IOptions<MartenOperationAuditOptions> _auditOptions = Options.Create(new MartenOperationAuditOptions());
    private DocumentStore? _store;
    private IProjectionDaemon? _daemon;

    public async ValueTask InitializeAsync()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        _store = DocumentStore.For(options =>
        {
            options.Connection(fixture.ConnectionString);
            options.DatabaseSchemaName = "abac_decision_audit";

            // The audit store appends to string stream ids ("audit:<type>:<id>").
            options.Events.StreamIdentity = JasperFx.Events.StreamIdentity.AsString;
            new ConfigureMartenOperationAuditProjections(_auditOptions, NullLoggerFactory.Instance).Configure(options);
        });

        // A clean schema with every table the store and the projection use created up front, so a
        // read never races the projection's lazy table creation.
        await _store.Advanced.Clean.CompletelyRemoveAllAsync();
        await _store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        _daemon = await _store.BuildProjectionDaemonAsync();
        await _daemon.StartAllAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_daemon is not null)
        {
            await _daemon.StopAllAsync();
            _daemon.Dispose();
        }

        _store?.Dispose();
    }

    protected override bool IsAvailable => fixture.IsAvailable;

    // As AddEncinaAuditMarten wires it: the temporal keys live in Marten, where the projection reads them.
    protected override IOperationAuditStore CreateStore()
    {
        var session = _store!.LightweightSession();
        var keys = new MartenTemporalKeyProvider(session, TimeProvider.System, NullLogger<MartenTemporalKeyProvider>.Instance);

        return new MartenOperationAuditStore(
            session,
            new AuditEventEncryptor(keys, _auditOptions, NullLogger<AuditEventEncryptor>.Instance),
            keys,
            _auditOptions,
            NullLogger<MartenOperationAuditStore>.Instance);
    }

    protected override Task WaitForReadsAsync() => _daemon!.WaitForNonStaleData(TimeSpan.FromSeconds(60));
}
