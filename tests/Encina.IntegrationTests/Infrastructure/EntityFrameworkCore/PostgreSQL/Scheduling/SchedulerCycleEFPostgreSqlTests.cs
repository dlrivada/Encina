using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Scheduling;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Scheduling;

/// <summary>
/// Regression tests for #1970: the scheduler cycle must persist its outcomes on EF Core (PostgreSQL).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class SchedulerCycleEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;

    public SchedulerCycleEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
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
