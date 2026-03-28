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
        // Drop and recreate to ensure schema is correct (EF Core test may have created with different schema)
        await using var dropCmd = conn.CreateCommand();
        dropCmd.CommandText = "IF OBJECT_ID('ProcessingActivities', 'U') IS NOT NULL DROP TABLE [ProcessingActivities]";
        await dropCmd.ExecuteNonQueryAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE [ProcessingActivities] (
                [Id] NVARCHAR(36) PRIMARY KEY,
                [RequestTypeName] NVARCHAR(512) NOT NULL,
                [Name] NVARCHAR(256) NOT NULL,
                [Purpose] NVARCHAR(1024) NOT NULL,
                [LawfulBasisValue] INT NOT NULL,
                [CategoriesOfDataSubjectsJson] NVARCHAR(4000) NOT NULL,
                [CategoriesOfPersonalDataJson] NVARCHAR(4000) NOT NULL,
                [RecipientsJson] NVARCHAR(4000) NOT NULL,
                [ThirdCountryTransfers] NVARCHAR(2000) NULL,
                [Safeguards] NVARCHAR(2000) NULL,
                [RetentionPeriodTicks] BIGINT NOT NULL,
                [SecurityMeasures] NVARCHAR(2000) NOT NULL,
                [CreatedAtUtc] DATETIMEOFFSET NOT NULL,
                [LastUpdatedAtUtc] DATETIMEOFFSET NOT NULL
            )
            """;
        await cmd.ExecuteNonQueryAsync();
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
        var registerResult = await _store.RegisterActivityAsync(activity);
        registerResult.Match(Right: _ => { }, Left: err => Assert.Fail($"RegisterActivityAsync failed: {err}"));

        var result = await _store.GetActivityByRequestTypeAsync(typeof(int));
        result.Match(Right: _ => { }, Left: err => Assert.Fail($"GetActivityByRequestTypeAsync failed: {err}"));
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
