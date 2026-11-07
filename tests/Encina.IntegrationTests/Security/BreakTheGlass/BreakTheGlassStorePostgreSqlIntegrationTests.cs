using System.Data;
using System.Data.Common;
using Encina.ADO.PostgreSQL.BreakTheGlass;
using Encina.ContractTests.Security.BreakTheGlass;
using Encina.Dapper.PostgreSQL.BreakTheGlass;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.IntegrationTests.Security.Audit;
using Encina.Messaging.Transactions;
using Encina.Security.BreakTheGlass;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.Security.BreakTheGlass;

/// <summary>Creates the break-the-glass table on the shared PostgreSQL fixture from the shipped script.</summary>
internal static class BreakTheGlassPostgreSqlSchema
{
    public const string Script = "030_CreateBreakTheGlassElevationsTable.sql";

    // Reports a business transaction on the connection, so every store call leases a connection of its own (ADR-048).
    public static IDbTransactionAccessor BusinessTransactionOn(IDbConnection connection)
    {
        var transaction = Substitute.For<IDbTransaction>();
        transaction.Connection.Returns(connection);
        var accessor = Substitute.For<IDbTransactionAccessor>();
        accessor.Current.Returns(transaction);
        return accessor;
    }

    public static async Task PrepareAsync(PostgreSqlFixture fixture, string package)
    {
        await fixture.ClearAllDataAsync();
        using var connection = (DbConnection)fixture.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var create = connection.CreateCommand();
        create.CommandText = ShippedSqlScript.Read(package, Script);
        await create.ExecuteNonQueryAsync();

        await using var clear = connection.CreateCommand();
        clear.CommandText = "DELETE FROM \"BreakTheGlassElevations\";";
        await clear.ExecuteNonQueryAsync();
    }

    // Writes and reads an elevation over a non-UTC session time zone; reports the row and the effectiveness around the expiry.
    public static async Task<(BreakTheGlassElevation? Stored, bool EffectiveBeforeExpiry, bool EffectiveAtExpiry)> RoundTripUnderNonUtcZoneAsync(
        IDbConnection connection, Func<IDbConnection, IBreakTheGlassStore> createStore)
    {
        await PostgreSqlSessionTimeZone.ApplyAsync(connection);
        try
        {
            var store = createStore(connection);
            var opened = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
            var expires = opened.AddHours(1);
            var elevation = new BreakTheGlassElevation
            {
                ElevationId = Guid.NewGuid(),
                TenantId = "tenant-zone",
                UserId = "clin-1",
                SubjectId = "patient-1",
                Scope = "clinical-record",
                Justification = "Emergency.",
                OpenedAtUtc = opened,
                ExpiresAtUtc = expires
            };

            (await store.AddAsync(elevation)).IsRight.ShouldBeTrue();
            var stored = (await store.GetByIdAsync("tenant-zone", elevation.ElevationId)).Match(Right: option => option.IfNoneUnsafe(() => null!), Left: _ => null!);
            var before = (await store.FindEffectiveAsync("tenant-zone", "clin-1", "patient-1", "clinical-record", expires.AddSeconds(-1))).Match(Right: o => o.IsSome, Left: _ => false);
            var atExpiry = (await store.FindEffectiveAsync("tenant-zone", "clin-1", "patient-1", "clinical-record", expires)).Match(Right: o => o.IsSome, Left: _ => true);
            return (stored, before, atExpiry);
        }
        finally
        {
            await PostgreSqlSessionTimeZone.ResetAsync(connection);
        }
    }
}

/// <summary>The shared <see cref="BreakTheGlassStoreContract"/> against <see cref="BreakTheGlassStoreADO"/> on PostgreSQL.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class BreakTheGlassStoreADOPostgreSqlIntegrationTests : BreakTheGlassStoreContract
{
    private readonly PostgreSqlFixture _fixture;

    public BreakTheGlassStoreADOPostgreSqlIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IBreakTheGlassStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await BreakTheGlassPostgreSqlSchema.PrepareAsync(_fixture, "Encina.ADO.PostgreSQL");
        return CreateSecondStore();
    }

    protected override IBreakTheGlassStore CreateSecondStore()
    {
        var connection = Track(_fixture.CreateConnection());
        return new BreakTheGlassStoreADO(connection, BreakTheGlassPostgreSqlSchema.BusinessTransactionOn(connection));
    }

    [Fact]
    public async Task TimestampsRoundTripAsUtc_UnderANonUtcSessionTimeZone()
    {
        var (stored, before, atExpiry) = await BreakTheGlassPostgreSqlSchema.RoundTripUnderNonUtcZoneAsync(
            Track(_fixture.CreateConnection()), connection => new BreakTheGlassStoreADO(connection));

        stored.ShouldNotBeNull();
        stored.OpenedAtUtc.ShouldBe(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));
        stored.OpenedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        before.ShouldBeTrue();
        atExpiry.ShouldBeFalse();
    }
}

/// <summary>The shared <see cref="BreakTheGlassStoreContract"/> against <see cref="BreakTheGlassStoreDapper"/> on PostgreSQL.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("Dapper-PostgreSQL")]
public sealed class BreakTheGlassStoreDapperPostgreSqlIntegrationTests : BreakTheGlassStoreContract
{
    private readonly PostgreSqlFixture _fixture;

    public BreakTheGlassStoreDapperPostgreSqlIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IBreakTheGlassStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await BreakTheGlassPostgreSqlSchema.PrepareAsync(_fixture, "Encina.Dapper.PostgreSQL");
        return CreateSecondStore();
    }

    protected override IBreakTheGlassStore CreateSecondStore()
    {
        var connection = Track(_fixture.CreateConnection());
        return new BreakTheGlassStoreDapper(connection, BreakTheGlassPostgreSqlSchema.BusinessTransactionOn(connection));
    }

    [Fact]
    public async Task TimestampsRoundTripAsUtc_UnderANonUtcSessionTimeZone()
    {
        var (stored, before, atExpiry) = await BreakTheGlassPostgreSqlSchema.RoundTripUnderNonUtcZoneAsync(
            Track(_fixture.CreateConnection()), connection => new BreakTheGlassStoreDapper(connection));

        stored.ShouldNotBeNull();
        stored.OpenedAtUtc.ShouldBe(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));
        stored.OpenedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        before.ShouldBeTrue();
        atExpiry.ShouldBeFalse();
    }
}
