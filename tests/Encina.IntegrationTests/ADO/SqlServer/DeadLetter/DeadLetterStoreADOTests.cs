using Encina.ADO.SqlServer.DeadLetter;
using Encina.ContractTests.Messaging.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.ADO.SqlServer.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreADO"/> on a real SQL Server
/// database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class DeadLetterStoreADOTests : DeadLetterStoreContract
{
    private readonly SqlServerFixture _fixture;

    public DeadLetterStoreADOTests(SqlServerFixture fixture)
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
