using Encina.ContractTests.Messaging.DeadLetter;
using Encina.Dapper.PostgreSQL.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.Dapper.PostgreSQL.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreDapper"/> on a real PostgreSQL
/// database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.PostgreSQL")]
[Collection("Dapper-PostgreSQL")]
public sealed class DeadLetterStoreDapperTests : DeadLetterStoreContract
{
    private readonly PostgreSqlFixture _fixture;

    public DeadLetterStoreDapperTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await _fixture.ClearAllDataAsync();
        return new DeadLetterStoreDapper(_fixture.CreateConnection(), timeProvider: timeProvider);
    }

    protected override IDeadLetterStore CreateSecondStore()
        => new DeadLetterStoreDapper(_fixture.CreateConnection(), timeProvider: Clock);

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);
}
