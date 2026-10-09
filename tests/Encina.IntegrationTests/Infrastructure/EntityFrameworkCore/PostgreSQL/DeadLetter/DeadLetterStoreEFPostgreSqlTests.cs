using Encina.ContractTests.Messaging.DeadLetter;
using Encina.EntityFrameworkCore.DeadLetter;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreEF"/> on a real PostgreSQL
/// database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class DeadLetterStoreEFPostgreSqlTests : DeadLetterStoreContract
{
    private readonly EFCorePostgreSqlFixture _fixture;
    private readonly List<TestEFDbContext> _contexts = [];

    public DeadLetterStoreEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await _fixture.EnsureSchemaCreatedAsync<TestEFDbContext>();
        await _fixture.ClearAllDataAsync();
        return NewStore(timeProvider);
    }

    protected override IDeadLetterStore CreateSecondStore() => NewStore(Clock);

    // The store under test uses the first context created (CreateStoreAsync).
    protected override Task ApplyNonUtcSessionTimeZoneAsync()
        => PostgreSqlSessionTimeZone.ApplyAsync(_contexts[0].Database.GetDbConnection());

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);

    public override async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    private DeadLetterStoreEF NewStore(TimeProvider timeProvider)
    {
        var context = _fixture.CreateDbContext<TestEFDbContext>();
        _contexts.Add(context);
        return new DeadLetterStoreEF(context, timeProvider);
    }
}
