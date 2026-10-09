using Encina.ContractTests.Messaging.DeadLetter;
using Encina.Dapper.SqlServer.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.Dapper.SqlServer.DeadLetter;

/// <summary>
/// Runs the dead letter store contract against <see cref="DeadLetterStoreDapper"/> on a real SQL Server
/// database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.SqlServer")]
[Collection("Dapper-SqlServer")]
public sealed class DeadLetterStoreDapperTests : DeadLetterStoreContract
{
    private readonly SqlServerFixture _fixture;

    public DeadLetterStoreDapperTests(SqlServerFixture fixture)
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
