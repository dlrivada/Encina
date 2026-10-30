using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Security.Audit;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Argument guards of the query methods of the Marten operation and read audit stores.
/// </summary>
public class AuditStoreQueryGuardTests
{
    private static (MartenOperationAuditStore Operation, MartenReadAuditStore Read) CreateStores()
    {
        var session = Substitute.For<IDocumentSession>();
        var options = Options.Create(new MartenOperationAuditOptions());
        var keyProvider = new InMemoryTemporalKeyProvider(TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);
        var encryptor = new AuditEventEncryptor(keyProvider, options, NullLogger<AuditEventEncryptor>.Instance);

        return (
            new MartenOperationAuditStore(session, encryptor, keyProvider, options,
                NullLogger<MartenOperationAuditStore>.Instance),
            new MartenReadAuditStore(session, encryptor, keyProvider, options,
                NullLogger<MartenReadAuditStore>.Instance));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task OperationStore_GetByEntityAsync_BlankEntityType_Throws(string? entityType)
    {
        var (store, _) = CreateStores();

        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByEntityAsync(entityType!, "id"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task OperationStore_GetByUserAsync_BlankUserId_Throws(string? userId)
    {
        var (store, _) = CreateStores();

        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByUserAsync(userId!, null, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task OperationStore_GetByCorrelationIdAsync_BlankCorrelationId_Throws(string? correlationId)
    {
        var (store, _) = CreateStores();

        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByCorrelationIdAsync(correlationId!));
    }

    [Fact]
    public async Task OperationStore_QueryAsync_NullQuery_Throws()
    {
        var (store, _) = CreateStores();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.QueryAsync(null!));
    }

    [Theory]
    [InlineData(null, "id")]
    [InlineData("", "id")]
    [InlineData("Order", null)]
    [InlineData("Order", " ")]
    public async Task ReadStore_GetAccessHistoryAsync_BlankArgument_Throws(string? entityType, string? entityId)
    {
        var (_, store) = CreateStores();

        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetAccessHistoryAsync(entityType!, entityId!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ReadStore_GetUserAccessHistoryAsync_BlankUserId_Throws(string? userId)
    {
        var (_, store) = CreateStores();

        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetUserAccessHistoryAsync(userId!, DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
    }

    [Fact]
    public async Task ReadStore_QueryAsync_NullQuery_Throws()
    {
        var (_, store) = CreateStores();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.QueryAsync(null!));
    }
}
