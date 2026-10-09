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
        _storeConnection = Track(_fixture.CreateConnection());
        return new DeadLetterStoreADO(_storeConnection, timeProvider: timeProvider);
    }

    private bool _zoneApplied;

    protected override async Task ApplyNonUtcSessionTimeZoneAsync()
    {
        _zoneApplied = true;
        await PostgreSqlSessionTimeZone.ApplyAsync(_storeConnection!);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_zoneApplied && _storeConnection is not null)
        {
            await PostgreSqlSessionTimeZone.ResetAsync(_storeConnection);
        }

        await base.DisposeAsync();
    }

    protected override IDeadLetterStore CreateSecondStore()
        => new DeadLetterStoreADO(Track(_fixture.CreateConnection()), timeProvider: Clock);

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);
}
