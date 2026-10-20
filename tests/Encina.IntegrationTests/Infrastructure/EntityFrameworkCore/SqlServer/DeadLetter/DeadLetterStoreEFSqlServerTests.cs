using Encina.ContractTests.Messaging.DeadLetter;
using Encina.EntityFrameworkCore.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreEF"/> on a real SQL Server
/// database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class DeadLetterStoreEFSqlServerTests : DeadLetterStoreContract
{
    private readonly EFCoreSqlServerFixture _fixture;
    private readonly List<TestEFDbContext> _contexts = [];

    public DeadLetterStoreEFSqlServerTests(EFCoreSqlServerFixture fixture)
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
