using Encina.Compliance.GDPR;
using Encina.EntityFrameworkCore.ProcessingActivity;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.ProcessingActivity;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable EF1001 // Internal EF Core API usage - needed for CreateTablesAsync on shared fixture

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.ProcessingActivity;

[Collection("EFCore-PostgreSQL")]
[Trait("Category", "Integration")]
[Trait("Provider", "EFCore.PostgreSQL")]
public sealed class ProcessingActivityRegistryEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;

    public ProcessingActivityRegistryEFPostgreSqlTests(EFCorePostgreSqlFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        await using var context = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var creator = ((IInfrastructure<IServiceProvider>)context).Instance.GetRequiredService<IRelationalDatabaseCreator>();
        try { await creator.CreateTablesAsync(); } catch { /* Tables may already exist */ }
        try
        {
            context.ProcessingActivities.RemoveRange(context.ProcessingActivities);
            await context.SaveChangesAsync();
        }
        catch { /* Table might not exist yet if CreateTablesAsync failed */ }
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static global::Encina.Compliance.GDPR.ProcessingActivity CreateActivity(Type? requestType = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Processing Activity",
        Purpose = "Integration test purpose",
        LawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis.Contract,
        CategoriesOfDataSubjects = ["Customers"],
        CategoriesOfPersonalData = ["Name", "Email"],
        Recipients = ["Service Provider"],
        RetentionPeriod = TimeSpan.FromDays(365),
        SecurityMeasures = "Encryption at rest",
        RequestType = requestType ?? typeof(ProcessingActivityRegistryEFPostgreSqlTests),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task RegisterActivityAsync_ValidActivity_ShouldPersist()
    {
        await using var context = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store = new ProcessingActivityRegistryEF(context);

        var activity = CreateActivity();
        var result = await store.RegisterActivityAsync(activity);
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterActivityAsync_DuplicateRequestType_ShouldReturnError()
    {
        await using var context1 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store1 = new ProcessingActivityRegistryEF(context1);
        await store1.RegisterActivityAsync(CreateActivity(typeof(string)));

        await using var context2 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store2 = new ProcessingActivityRegistryEF(context2);
        var result = await store2.RegisterActivityAsync(CreateActivity(typeof(string)));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_Registered_ShouldReturnSome()
    {
        await using var context1 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store1 = new ProcessingActivityRegistryEF(context1);
        await store1.RegisterActivityAsync(CreateActivity(typeof(int)));

        await using var context2 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store2 = new ProcessingActivityRegistryEF(context2);
        var result = await store2.GetActivityByRequestTypeAsync(typeof(int));
        result.IsRight.ShouldBeTrue();
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)result;
        option.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NotRegistered_ShouldReturnNone()
    {
        await using var context = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store = new ProcessingActivityRegistryEF(context);

        var result = await store.GetActivityByRequestTypeAsync(typeof(double));
        result.IsRight.ShouldBeTrue();
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)result;
        option.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_WithActivities_ShouldReturnAll()
    {
        await using var context1 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store1 = new ProcessingActivityRegistryEF(context1);
        await store1.RegisterActivityAsync(CreateActivity(typeof(byte)));
        await store1.RegisterActivityAsync(CreateActivity(typeof(short)));

        await using var context2 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store2 = new ProcessingActivityRegistryEF(context2);
        var result = await store2.GetAllActivitiesAsync();
        result.IsRight.ShouldBeTrue();
        var activities = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<global::Encina.Compliance.GDPR.ProcessingActivity>)[]);
        activities.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateActivityAsync_Existing_ShouldSucceed()
    {
        await using var context1 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store1 = new ProcessingActivityRegistryEF(context1);
        var activity = CreateActivity(typeof(long));
        await store1.RegisterActivityAsync(activity);

        await using var context2 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store2 = new ProcessingActivityRegistryEF(context2);
        var updated = activity with { Name = "Updated Name", LastUpdatedAtUtc = DateTimeOffset.UtcNow };
        var result = await store2.UpdateActivityAsync(updated);
        result.IsRight.ShouldBeTrue();

        await using var context3 = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store3 = new ProcessingActivityRegistryEF(context3);
        var retrieved = await store3.GetActivityByRequestTypeAsync(typeof(long));
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)retrieved;
        option.IfSome(a => a.Name.ShouldBe("Updated Name"));
    }

    [Fact]
    public async Task UpdateActivityAsync_NotExisting_ShouldReturnError()
    {
        await using var context = _fixture.CreateDbContext<ProcessingActivityTestDbContext>();
        var store = new ProcessingActivityRegistryEF(context);

        var activity = CreateActivity(typeof(decimal));
        var result = await store.UpdateActivityAsync(activity);
        result.IsLeft.ShouldBeTrue();
    }
}
