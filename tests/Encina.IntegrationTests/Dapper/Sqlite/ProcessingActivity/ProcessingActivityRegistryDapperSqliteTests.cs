using Encina.Compliance.GDPR;
using Encina.Dapper.Sqlite.ProcessingActivity;
using Encina.TestInfrastructure.Fixtures;
using FluentAssertions;
using LanguageExt;

namespace Encina.IntegrationTests.Dapper.Sqlite.ProcessingActivity;

[Collection("Dapper-Sqlite")]
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.Sqlite")]
public sealed class ProcessingActivityRegistryDapperSqliteTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;
    private ProcessingActivityRegistryDapper _store = null!;

    public ProcessingActivityRegistryDapperSqliteTests(SqliteFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        _store = new ProcessingActivityRegistryDapper(_fixture.ConnectionString);
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
        RequestType = requestType ?? typeof(ProcessingActivityRegistryDapperSqliteTests),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task RegisterActivityAsync_ValidActivity_ShouldPersist()
    {
        var activity = CreateActivity();
        var result = await _store.RegisterActivityAsync(activity);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterActivityAsync_DuplicateRequestType_ShouldReturnError()
    {
        var activity1 = CreateActivity(typeof(string));
        var activity2 = CreateActivity(typeof(string));
        await _store.RegisterActivityAsync(activity1);

        var result = await _store.RegisterActivityAsync(activity2);
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_Registered_ShouldReturnSome()
    {
        var activity = CreateActivity(typeof(int));
        await _store.RegisterActivityAsync(activity);

        var result = await _store.GetActivityByRequestTypeAsync(typeof(int));
        result.IsRight.Should().BeTrue();
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)result;
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NotRegistered_ShouldReturnNone()
    {
        var result = await _store.GetActivityByRequestTypeAsync(typeof(double));
        result.IsRight.Should().BeTrue();
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_WithActivities_ShouldReturnAll()
    {
        await _store.RegisterActivityAsync(CreateActivity(typeof(byte)));
        await _store.RegisterActivityAsync(CreateActivity(typeof(short)));

        var result = await _store.GetAllActivitiesAsync();
        result.IsRight.Should().BeTrue();
        var activities = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<global::Encina.Compliance.GDPR.ProcessingActivity>)[]);
        activities.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateActivityAsync_Existing_ShouldSucceed()
    {
        var activity = CreateActivity(typeof(long));
        await _store.RegisterActivityAsync(activity);

        var updated = activity with { Name = "Updated Name", LastUpdatedAtUtc = DateTimeOffset.UtcNow };
        var result = await _store.UpdateActivityAsync(updated);
        result.IsRight.Should().BeTrue();

        var retrieved = await _store.GetActivityByRequestTypeAsync(typeof(long));
        var option = (Option<global::Encina.Compliance.GDPR.ProcessingActivity>)retrieved;
        option.IfSome(a => a.Name.Should().Be("Updated Name"));
    }

    [Fact]
    public async Task UpdateActivityAsync_NotExisting_ShouldReturnError()
    {
        var activity = CreateActivity(typeof(decimal));
        var result = await _store.UpdateActivityAsync(activity);
        result.IsLeft.Should().BeTrue();
    }
}
