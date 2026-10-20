using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Scheduling;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Scheduling;

/// <summary>
/// Regression tests for #1970: the scheduler cycle must persist its outcomes on EF Core (SQL Server).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class SchedulerCycleEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public SchedulerCycleEFSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.EnsureSchemaCreatedAsync<TestEFDbContext>();
        await _fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task DueOneOffMessage_RunsOnceAcrossThreeCycles_AndIsPersistedAsProcessed() =>
        SchedulerCycleScenarios.DueOneOffMessage_RunsOnceAcrossThreeCycles_AndIsPersistedAsProcessed(
            _fixture.CreateDbContext<TestEFDbContext>);

    [Fact]
    public Task FailingHandler_IncrementsRetryCountAndSetsNextRetry() =>
        SchedulerCycleScenarios.FailingHandler_IncrementsRetryCountAndSetsNextRetry(
            _fixture.CreateDbContext<TestEFDbContext>);

    [Fact]
    public Task RecurringMessage_GetsItsNextScheduledAt() =>
        SchedulerCycleScenarios.RecurringMessage_GetsItsNextScheduledAt(
            _fixture.CreateDbContext<TestEFDbContext>);
}
