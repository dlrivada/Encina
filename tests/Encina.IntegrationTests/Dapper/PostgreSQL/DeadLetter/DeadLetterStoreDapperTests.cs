using Encina.ContractTests.Messaging.DeadLetter;
using Encina.Dapper.PostgreSQL.DeadLetter;
using Encina.IntegrationTests.Messaging.DeadLetter;
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

    private System.Data.IDbConnection? _storeConnection;

    protected override async Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await _fixture.ClearAllDataAsync();
        _storeConnection = Track(_fixture.CreateConnection());
        return new DeadLetterStoreDapper(_storeConnection, timeProvider: timeProvider);
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
        => new DeadLetterStoreDapper(Track(_fixture.CreateConnection()), timeProvider: Clock);

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data)
        => new DeadLetterMessageFactory().Create(data);
}
