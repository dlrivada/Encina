using Encina.ADO.PostgreSQL.DeadLetter;
using Encina.ContractTests.Messaging.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.ADO.PostgreSQL.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreADO"/> on a real PostgreSQL
/// database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class DeadLetterStoreADOTests : DeadLetterStoreContract
{
    private readonly PostgreSqlFixture _fixture;

    public DeadLetterStoreADOTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await _fixture.ClearAllDataAsync();
        return new DeadLetterStoreADO(_fixture.CreateConnection(), timeProvider: timeProvider);
    }

    protected override IDeadLetterStore CreateSecondStore()
        => new DeadLetterStoreADO(_fixture.CreateConnection(), timeProvider: Clock);

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);
}
