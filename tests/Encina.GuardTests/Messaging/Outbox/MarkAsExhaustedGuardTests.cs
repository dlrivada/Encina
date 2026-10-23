using System.Data;
using Encina.Messaging.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.GuardTests.Messaging.Outbox;

/// <summary>
/// Guards of <see cref="IOutboxStore.MarkAsExhaustedAsync"/> on every outbox store (#2588): a non-positive
/// <c>maxRetries</c> and a missing error message are rejected before any database call.
/// </summary>
[Trait("Category", "Guard")]
public sealed class MarkAsExhaustedGuardTests
{
    public static TheoryData<string> Providers =>
    [
        "ADO.SqlServer", "ADO.PostgreSQL", "ADO.MySQL",
        "Dapper.SqlServer", "Dapper.PostgreSQL", "Dapper.MySQL",
        "EntityFrameworkCore", "MongoDB", "Fake"
    ];

    private static IOutboxStore CreateStore(string provider)
    {
        var connection = Substitute.For<IDbConnection>();
        return provider switch
        {
            "ADO.SqlServer" => new global::Encina.ADO.SqlServer.Outbox.OutboxStoreADO(connection),
            "ADO.PostgreSQL" => new global::Encina.ADO.PostgreSQL.Outbox.OutboxStoreADO(connection),
            "ADO.MySQL" => new global::Encina.ADO.MySQL.Outbox.OutboxStoreADO(connection),
            "Dapper.SqlServer" => new global::Encina.Dapper.SqlServer.Outbox.OutboxStoreDapper(connection),
            "Dapper.PostgreSQL" => new global::Encina.Dapper.PostgreSQL.Outbox.OutboxStoreDapper(connection),
            "Dapper.MySQL" => new global::Encina.Dapper.MySQL.Outbox.OutboxStoreDapper(connection),
            "EntityFrameworkCore" => new global::Encina.EntityFrameworkCore.Outbox.OutboxStoreEF(
                new DbContext(new DbContextOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)),
            "MongoDB" => new global::Encina.MongoDB.Outbox.OutboxStoreMongoDB(
                Substitute.For<IMongoClient>(),
                Options.Create(new global::Encina.MongoDB.EncinaMongoDbOptions { DatabaseName = "guard" }),
                NullLogger<global::Encina.MongoDB.Outbox.OutboxStoreMongoDB>.Instance),
            _ => new global::Encina.Testing.Fakes.Stores.FakeOutboxStore()
        };
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task MarkAsExhaustedAsync_ZeroMaxRetries_ThrowsArgumentOutOfRange(string provider)
    {
        var store = CreateStore(provider);

        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => store.MarkAsExhaustedAsync(Guid.NewGuid(), "error", 0));

        ex.ParamName.ShouldBe("maxRetries");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task MarkAsExhaustedAsync_NegativeMaxRetries_ThrowsArgumentOutOfRange(string provider)
    {
        var store = CreateStore(provider);

        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => store.MarkAsExhaustedAsync(Guid.NewGuid(), "error", -3));

        ex.ParamName.ShouldBe("maxRetries");
    }

    [Theory]
    [InlineData("ADO.SqlServer")]
    [InlineData("ADO.PostgreSQL")]
    [InlineData("ADO.MySQL")]
    [InlineData("Dapper.SqlServer")]
    [InlineData("Dapper.PostgreSQL")]
    [InlineData("Dapper.MySQL")]
    [InlineData("EntityFrameworkCore")]
    [InlineData("MongoDB")]
    public async Task MarkAsExhaustedAsync_NullErrorMessage_ThrowsArgumentException(string provider)
    {
        var store = CreateStore(provider);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.MarkAsExhaustedAsync(Guid.NewGuid(), null!, 3));

        ex.ParamName.ShouldBe("errorMessage");
    }

    [Theory]
    [InlineData("ADO.SqlServer")]
    [InlineData("ADO.PostgreSQL")]
    [InlineData("ADO.MySQL")]
    [InlineData("Dapper.SqlServer")]
    [InlineData("Dapper.PostgreSQL")]
    [InlineData("Dapper.MySQL")]
    public async Task MarkAsExhaustedAsync_EmptyMessageId_ThrowsArgumentException(string provider)
    {
        var store = CreateStore(provider);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.MarkAsExhaustedAsync(Guid.Empty, "error", 3));

        ex.ParamName.ShouldBe("messageId");
    }
}
