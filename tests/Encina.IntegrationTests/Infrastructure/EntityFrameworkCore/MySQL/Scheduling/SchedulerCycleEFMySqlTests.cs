using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Scheduling;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Scheduling;

/// <summary>
/// Regression tests for #1970: the scheduler cycle must persist its outcomes on EF Core (MySQL).
/// Skipped until Pomelo.EntityFrameworkCore.MySql supports EF Core 10, like the rest of the EF Core MySQL suite.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class SchedulerCycleEFMySqlTests : IAsyncLifetime
{
    private const string SkipReason =
        "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility";

    private readonly EFCoreMySqlFixture _fixture;

    public SchedulerCycleEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            await _fixture.EnsureSchemaCreatedAsync<TestEFDbContext>();
            await _fixture.ClearAllDataAsync();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task DueOneOffMessage_RunsOnceAcrossThreeCycles_AndIsPersistedAsProcessed()
    {
        Assert.SkipUnless(_fixture.IsAvailable, SkipReason);
        return SchedulerCycleScenarios.DueOneOffMessage_RunsOnceAcrossThreeCycles_AndIsPersistedAsProcessed(
            _fixture.CreateDbContext<TestEFDbContext>);
    }

    [Fact]
    public Task FailingHandler_IncrementsRetryCountAndSetsNextRetry()
    {
        Assert.SkipUnless(_fixture.IsAvailable, SkipReason);
        return SchedulerCycleScenarios.FailingHandler_IncrementsRetryCountAndSetsNextRetry(
            _fixture.CreateDbContext<TestEFDbContext>);
    }

    [Fact]
    public Task RecurringMessage_GetsItsNextScheduledAt()
    {
        Assert.SkipUnless(_fixture.IsAvailable, SkipReason);
        return SchedulerCycleScenarios.RecurringMessage_GetsItsNextScheduledAt(
            _fixture.CreateDbContext<TestEFDbContext>);
    }
}
