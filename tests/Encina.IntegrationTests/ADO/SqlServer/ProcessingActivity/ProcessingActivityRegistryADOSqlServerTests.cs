using Encina.ADO.SqlServer.ProcessingActivity;
using Encina.Compliance.GDPR;
using Encina.TestInfrastructure.Fixtures;
using FluentAssertions;
using LanguageExt;
using Microsoft.Data.SqlClient;

namespace Encina.IntegrationTests.ADO.SqlServer.ProcessingActivity;

[Collection("ADO-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.SqlServer")]
public sealed class ProcessingActivityRegistryADOSqlServerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private ProcessingActivityRegistryADO _store = null!;

    public ProcessingActivityRegistryADOSqlServerTests(SqlServerFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        // Create table if not exists using a fresh connection
        await using var conn = new SqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessingActivities')
            CREATE TABLE [ProcessingActivities] (
                [Id] UNIQUEIDENTIFIER PRIMARY KEY,
                [Name] NVARCHAR(500) NOT NULL,
                [Purpose] NVARCHAR(1000) NOT NULL,
                [LawfulBasis] INT NOT NULL,
                [CategoriesOfDataSubjects] NVARCHAR(MAX) NOT NULL,
                [CategoriesOfPersonalData] NVARCHAR(MAX) NOT NULL,
                [Recipients] NVARCHAR(MAX) NOT NULL,
                [RetentionPeriod] BIGINT NOT NULL,
                [SecurityMeasures] NVARCHAR(2000) NOT NULL,
                [RequestType] NVARCHAR(1000) NOT NULL,
                [CreatedAtUtc] DATETIMEOFFSET NOT NULL,
                [LastUpdatedAtUtc] DATETIMEOFFSET NOT NULL,
                [TenantId] NVARCHAR(256) NULL,
                [ModuleId] NVARCHAR(256) NULL
            )
            """;
        await cmd.ExecuteNonQueryAsync();
        await using var delCmd = conn.CreateCommand();
        delCmd.CommandText = "DELETE FROM [ProcessingActivities]";
        await delCmd.ExecuteNonQueryAsync();
        _store = new ProcessingActivityRegistryADO(_fixture.ConnectionString);
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
        RequestType = requestType ?? typeof(ProcessingActivityRegistryADOSqlServerTests),
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
