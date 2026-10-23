using System.Text.RegularExpressions;
using Encina.Compliance.GDPR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using AdoMySql = Encina.ADO.MySQL.ProcessingActivity.ProcessingActivityRegistryADO;
using AdoPostgreSql = Encina.ADO.PostgreSQL.ProcessingActivity.ProcessingActivityRegistryADO;
using AdoSqlServer = Encina.ADO.SqlServer.ProcessingActivity.ProcessingActivityRegistryADO;
using DapperMySql = Encina.Dapper.MySQL.ProcessingActivity.ProcessingActivityRegistryDapper;
using DapperPostgreSql = Encina.Dapper.PostgreSQL.ProcessingActivity.ProcessingActivityRegistryDapper;
using DapperSqlServer = Encina.Dapper.SqlServer.ProcessingActivity.ProcessingActivityRegistryDapper;
using EfRegistry = Encina.EntityFrameworkCore.ProcessingActivity.ProcessingActivityRegistryEF;
using GdprProcessingActivity = Encina.Compliance.GDPR.ProcessingActivity;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Proves that the exception branch of every ProcessingActivity registry writes only the exception
/// type to the activity status and tags, never the exception message (AGENTS.md section 3, #2134).
/// </summary>
/// <remarks>
/// The ADO.NET and Dapper registries fail without a database: a connection string with an unknown
/// keyword makes the driver throw while the connection object is built, and the driver puts the keyword
/// (here the secret) in the exception message. The MongoDB registry builds its client and index in the
/// constructor, so its failure branch needs a live server and is covered by its integration test.
/// </remarks>
[Collection("ActivityListenerIsolation")]
public sealed partial class ProcessingActivityRegistryActivityStatusTests
{
    [GeneratedRegex("^[A-Za-z]+Exception$")]
    private static partial Regex ExceptionTypeName();

    public static TheoryData<string> AdoAndDapperProviders =>
    [
        "ado-sqlserver", "ado-postgresql", "ado-mysql", "dapper-sqlserver", "dapper-postgresql", "dapper-mysql"
    ];

    [Theory]
    [MemberData(nameof(AdoAndDapperProviders))]
    public async Task AdoAndDapperRegistries_WhenTheDriverThrows_StatusIsTheExceptionType(string provider)
    {
        using var capture = new ActivityStatusCapture();
        var secret = "secret-" + Guid.NewGuid().ToString("N");
        var registry = CreateRegistry(provider, secret);

        await ExerciseAllOperationsAsync(registry, secret);

        AssertOnlyExceptionTypesReachedTheActivities(capture, secret, expectedFailures: 4);
    }

    [Fact]
    public async Task EfRegistry_WhenTheContextThrows_StatusIsTheExceptionType()
    {
        using var capture = new ActivityStatusCapture();
        var secret = "secret-" + Guid.NewGuid().ToString("N");
        var context = Substitute.For<DbContext>();
        context.Set<ProcessingActivityEntity>().Returns(_ => throw new InvalidOperationException(secret));
        IProcessingActivityRegistry registry = new EfRegistry(context);

        await ExerciseReadAndUpdateOperationsAsync(registry, secret);

        AssertOnlyExceptionTypesReachedTheActivities(capture, secret, expectedFailures: 3);
    }

    private static IProcessingActivityRegistry CreateRegistry(string provider, string secret)
    {
        var connectionString = secret + "=1";
        return provider switch
        {
            "ado-sqlserver" => new AdoSqlServer(connectionString),
            "ado-postgresql" => new AdoPostgreSql(connectionString),
            "ado-mysql" => new AdoMySql(connectionString),
            "dapper-sqlserver" => new DapperSqlServer(connectionString),
            "dapper-postgresql" => new DapperPostgreSql(connectionString),
            "dapper-mysql" => new DapperMySql(connectionString),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider.")
        };
    }

    private static async Task ExerciseAllOperationsAsync(IProcessingActivityRegistry registry, string secret)
    {
        var registered = await registry.RegisterActivityAsync(NewActivity());
        registered.IsLeft.ShouldBeTrue();
        registered.IfLeft(error => error.Message.ShouldNotContain(secret));

        await ExerciseReadAndUpdateOperationsAsync(registry, secret);
    }

    private static async Task ExerciseReadAndUpdateOperationsAsync(IProcessingActivityRegistry registry, string secret)
    {
        var all = await registry.GetAllActivitiesAsync();
        all.IsLeft.ShouldBeTrue();
        all.IfLeft(error => error.Message.ShouldNotContain(secret));

        var byType = await registry.GetActivityByRequestTypeAsync(typeof(RegistryTestRequest));
        byType.IsLeft.ShouldBeTrue();
        byType.IfLeft(error => error.Message.ShouldNotContain(secret));

        var updated = await registry.UpdateActivityAsync(NewActivity());
        updated.IsLeft.ShouldBeTrue();
        updated.IfLeft(error => error.Message.ShouldNotContain(secret));
    }

    private static void AssertOnlyExceptionTypesReachedTheActivities(
        ActivityStatusCapture capture, string secret, int expectedFailures)
    {
        // The listener sees every Encina activity of the process, so unrelated tests running in
        // parallel can add error activities (for example "duplicate"); count only exception type names.
        var exceptionTypeDescriptions = capture.ErrorStatusDescriptions()
            .Count(description => ExceptionTypeName().IsMatch(description ?? string.Empty));
        exceptionTypeDescriptions.ShouldBeGreaterThanOrEqualTo(expectedFailures);

        capture.ShouldNeverCarry(secret);
    }

    private static GdprProcessingActivity NewActivity() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Registry status test",
        Purpose = "Proves the failure status carries no message",
        LawfulBasis = LawfulBasis.LegitimateInterests,
        CategoriesOfDataSubjects = ["Test subjects"],
        CategoriesOfPersonalData = ["Test data"],
        Recipients = ["Internal"],
        ThirdCountryTransfers = null,
        Safeguards = null,
        RetentionPeriod = TimeSpan.FromDays(365),
        SecurityMeasures = "Encryption at rest",
        RequestType = typeof(RegistryTestRequest),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };

    private sealed class RegistryTestRequest;
}
