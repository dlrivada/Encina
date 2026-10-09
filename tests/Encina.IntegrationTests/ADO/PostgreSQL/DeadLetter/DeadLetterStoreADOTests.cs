using Encina.ADO.PostgreSQL.DeadLetter;
using Encina.ContractTests.Messaging.DeadLetter;
using Encina.IntegrationTests.Messaging.DeadLetter;
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

    private System.Data.IDbConnection? _storeConnection;

    protected override async Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await _fixture.ClearAllDataAsync();
        _storeConnection = _fixture.CreateConnection();
        return new DeadLetterStoreADO(_storeConnection, timeProvider: timeProvider);
    }

    protected override Task ApplyNonUtcSessionTimeZoneAsync()
        => PostgreSqlSessionTimeZone.ApplyAsync(_storeConnection!);

    protected override IDeadLetterStore CreateSecondStore()
        => new DeadLetterStoreADO(_fixture.CreateConnection(), timeProvider: Clock);

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);
}
