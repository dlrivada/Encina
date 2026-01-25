using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Outbox;

/// <summary>
/// MySQL-specific integration tests for OutboxStoreEF.
/// </summary>
/// <remarks>
/// MySQL/MariaDB support via Pomelo.EntityFrameworkCore.MySql is pending v10.0.0 release
/// which adds EF Core 10 compatibility. These tests will be skipped until then.
/// See: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class OutboxStoreEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public OutboxStoreEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [SkippableFact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistMessage()
    {
        // Skip until Pomelo v10 is released
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 (EF Core 10 compatible). See: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019");

        // This code will execute once Pomelo v10 is available
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await Task.CompletedTask; // Placeholder
    }

    [SkippableFact]
    public async Task GetPendingMessagesAsync_WithMultipleMessages_ShouldReturnUnprocessedOnly()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 (EF Core 10 compatible).");
        await Task.CompletedTask;
    }

    [SkippableFact]
    public async Task MarkAsProcessedAsync_ShouldUpdateTimestampAndClearError()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 (EF Core 10 compatible).");
        await Task.CompletedTask;
    }
}
