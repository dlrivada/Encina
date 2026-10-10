using Encina.ContractTests.Messaging.DeadLetter;
using Encina.Dapper.MySQL.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.Dapper.MySQL.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreDapper"/> on a real MySQL
/// database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.MySQL")]
[Collection("Dapper-MySQL")]
public sealed class DeadLetterStoreDapperTests : DeadLetterStoreContract
{
    private readonly MySqlFixture _fixture;

    public DeadLetterStoreDapperTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await _fixture.ClearAllDataAsync();
        return new DeadLetterStoreDapper(Track(_fixture.CreateConnection()), timeProvider: timeProvider);
    }

    protected override IDeadLetterStore CreateSecondStore()
        => new DeadLetterStoreDapper(Track(_fixture.CreateConnection()), timeProvider: Clock);

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);
}
