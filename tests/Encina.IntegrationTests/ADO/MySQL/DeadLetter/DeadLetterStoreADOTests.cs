using Encina.ADO.MySQL.DeadLetter;
using Encina.ContractTests.Messaging.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.ADO.MySQL.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreADO"/> on a real MySQL
/// database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.MySQL")]
[Collection("ADO-MySQL")]
public sealed class DeadLetterStoreADOTests : DeadLetterStoreContract
{
    private readonly MySqlFixture _fixture;

    public DeadLetterStoreADOTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await _fixture.ClearAllDataAsync();
        return new DeadLetterStoreADO(Track(_fixture.CreateConnection()), timeProvider: timeProvider);
    }

    protected override IDeadLetterStore CreateSecondStore()
        => new DeadLetterStoreADO(Track(_fixture.CreateConnection()), timeProvider: Clock);

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);
}
