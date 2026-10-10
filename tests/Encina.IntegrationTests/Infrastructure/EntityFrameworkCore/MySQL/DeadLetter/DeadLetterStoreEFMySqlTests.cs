using Encina.ContractTests.Messaging.DeadLetter;
using Encina.EntityFrameworkCore.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreEF"/> on a real MySQL database
/// via Testcontainers. <see cref="EFCoreMySqlFixture"/> reports the whole MySQL EF Core suite as skipped until
/// Pomelo.EntityFrameworkCore.MySql ships EF Core 10 support; this class needs no change when it does.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class DeadLetterStoreEFMySqlTests : DeadLetterStoreContract
{
    private readonly EFCoreMySqlFixture _fixture;
    private readonly List<TestEFDbContext> _contexts = [];

    public DeadLetterStoreEFMySqlTests(EFCoreMySqlFixture fixture)
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
