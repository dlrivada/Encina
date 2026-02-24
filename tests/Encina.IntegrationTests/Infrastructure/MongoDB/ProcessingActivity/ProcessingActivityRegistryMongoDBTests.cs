using Encina.Compliance.GDPR;
using Encina.MongoDB.ProcessingActivity;
using Encina.TestInfrastructure.Fixtures;
using FluentAssertions;
using LanguageExt;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ProcessingActivity;

[Collection("MongoDB")]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ProcessingActivityRegistryMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;

    public ProcessingActivityRegistryMongoDBTests(MongoDbFixture fixture) => _fixture = fixture;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private ProcessingActivityRegistryMongoDB CreateStore() =>
        new(_fixture.ConnectionString, MongoDbFixture.DatabaseName,
            $"processing_activities_{Guid.NewGuid():N}");

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
        RequestType = requestType ?? typeof(ProcessingActivityRegistryMongoDBTests),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task RegisterActivityAsync_ValidActivity_ShouldPersist()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        var activity = CreateActivity();
        var result = await store.RegisterActivityAsync(activity);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterActivityAsync_DuplicateRequestType_ShouldReturnError()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        await store.RegisterActivityAsync(CreateActivity(typeof(string)));
        var result = await store.RegisterActivityAsync(CreateActivity(typeof(string)));
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_Registered_ShouldReturnSome()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        await store.RegisterActivityAsync(CreateActivity(typeof(int)));
        var result = await store.GetActivityByRequestTypeAsync(typeof(int));
        result.IsRight.Should().BeTrue();
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)result;
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NotRegistered_ShouldReturnNone()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        var result = await store.GetActivityByRequestTypeAsync(typeof(double));
        result.IsRight.Should().BeTrue();
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_WithActivities_ShouldReturnAll()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        await store.RegisterActivityAsync(CreateActivity(typeof(byte)));
        await store.RegisterActivityAsync(CreateActivity(typeof(short)));

        var result = await store.GetAllActivitiesAsync();
        result.IsRight.Should().BeTrue();
        var activities = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<global::Encina.Compliance.GDPR.ProcessingActivity>)[]);
        activities.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateActivityAsync_Existing_ShouldSucceed()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        var activity = CreateActivity(typeof(long));
        await store.RegisterActivityAsync(activity);

        var updated = activity with { Name = "Updated Name", LastUpdatedAtUtc = DateTimeOffset.UtcNow };
        var result = await store.UpdateActivityAsync(updated);
        result.IsRight.Should().BeTrue();

        var retrieved = await store.GetActivityByRequestTypeAsync(typeof(long));
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)retrieved;
        option.IfSome(a => a.Name.Should().Be("Updated Name"));
    }

    [Fact]
    public async Task UpdateActivityAsync_NotExisting_ShouldReturnError()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        var activity = CreateActivity(typeof(decimal));
        var result = await store.UpdateActivityAsync(activity);
        result.IsLeft.Should().BeTrue();
    }
}
