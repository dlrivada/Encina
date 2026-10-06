using System.Data.Common;
using Encina.Security.Audit;

namespace Encina.UnitTests.Security.Audit.Relational;

/// <summary>
/// Zone-independent proof that a relational operation audit store hands only UTC instants to the driver.
/// </summary>
/// <remarks>
/// <para>
/// The store is given a <see cref="RecordingDbConnection"/> and every bound parameter is inspected, so the
/// assertion sits at the boundary where the store passes values to the driver and does not depend on the time
/// zone of the machine that runs the test: the inputs carry explicit instants (10:00 UTC), whatever
/// <see cref="TimeZoneInfo.Local"/> is. On a UTC runner a <see cref="DateTimeKind.Local"/> input equals its UTC
/// value, so the Local cases only detect a missing normalisation (the Kind stays Local) and a wrong
/// reinterpretation is caught only on a non-UTC machine; the Unspecified and offset cases discriminate everywhere.
/// </para>
/// <para>
/// A <see cref="DateTimeKind.Local"/> input is built from the UTC instant with <see cref="DateTime.ToLocalTime"/>,
/// an <see cref="DateTimeKind.Unspecified"/> input carries the UTC wall clock, and the offset inputs use
/// <c>+02:00</c> so that, unlike a UTC runner's local offset, they are never already normalised.
/// </para>
/// </remarks>
public abstract class OperationAuditStoreUtcTestsBase
{
    private static readonly DateTime ExpectedUtc = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset PlusTwoHours = new(2026, 3, 1, 12, 0, 0, TimeSpan.FromHours(2));

    /// <summary>Creates the store under test over <paramref name="connection"/>.</summary>
    protected abstract IOperationAuditStore CreateStore(DbConnection connection);

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task RecordAsync_AnyDateTimeKindAndOffset_BindsTheExpectedUtcInstants(DateTimeKind kind)
    {
        // Arrange
        var connection = new RecordingDbConnection();
        var store = CreateStore(connection);
        var entry = new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "correlation-1",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = InstantOfKind(kind),
            StartedAtUtc = PlusTwoHours,
            CompletedAtUtc = PlusTwoHours
        };

        // Act
        var result = await store.RecordAsync(entry, TestContext.Current.CancellationToken);

        // Assert
        result.IsRight.ShouldBeTrue();
        AssertBoundAsExpectedUtc(connection, "TimestampUtc", "StartedAtUtc", "CompletedAtUtc");
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task GetByUserAsync_AnyDateTimeKind_BindsTheExpectedUtcRange(DateTimeKind kind)
    {
        // Arrange
        var connection = new RecordingDbConnection();
        var store = CreateStore(connection);

        // Act
        var result = await store.GetByUserAsync("user-1", InstantOfKind(kind), InstantOfKind(kind), TestContext.Current.CancellationToken);

        // Assert
        result.IsRight.ShouldBeTrue();
        AssertBoundAsExpectedUtc(connection, "FromUtc", "ToUtc");
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task QueryAsync_AnyDateTimeKind_BindsTheExpectedUtcRange(DateTimeKind kind)
    {
        // Arrange
        var connection = new RecordingDbConnection();
        var store = CreateStore(connection);
        var query = new OperationAuditQuery { FromUtc = InstantOfKind(kind), ToUtc = InstantOfKind(kind) };

        // Act
        var result = await store.QueryAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsRight.ShouldBeTrue();
        AssertBoundAsExpectedUtc(connection, "FromUtc", "ToUtc");
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task PurgeEntriesAsync_AnyDateTimeKind_BindsTheExpectedUtcCutoff(DateTimeKind kind)
    {
        // Arrange
        var connection = new RecordingDbConnection();
        var store = CreateStore(connection);

        // Act
        var result = await store.PurgeEntriesAsync(InstantOfKind(kind), TestContext.Current.CancellationToken);

        // Assert
        result.IsRight.ShouldBeTrue();
        AssertBoundAsExpectedUtc(connection, "OlderThanUtc");
    }

    private static DateTime InstantOfKind(DateTimeKind kind) => kind switch
    {
        DateTimeKind.Utc => ExpectedUtc,
        DateTimeKind.Local => ExpectedUtc.ToLocalTime(),
        _ => DateTime.SpecifyKind(ExpectedUtc, DateTimeKind.Unspecified)
    };

    private static void AssertBoundAsExpectedUtc(RecordingDbConnection connection, params string[] parameterNames)
    {
        foreach (var name in parameterNames)
        {
            var bound = connection.BoundParameters.Where(p => p.Name == name).ToList();
            bound.ShouldNotBeEmpty($"parameter {name} was never bound");

            foreach (var parameter in bound)
            {
                switch (parameter.Value)
                {
                    case DateTime dateTime:
                        dateTime.Kind.ShouldBe(DateTimeKind.Utc, $"parameter {name}");
                        dateTime.Ticks.ShouldBe(ExpectedUtc.Ticks, $"parameter {name}");
                        break;
                    case DateTimeOffset offset:
                        offset.Offset.ShouldBe(TimeSpan.Zero, $"parameter {name}");
                        offset.UtcTicks.ShouldBe(ExpectedUtc.Ticks, $"parameter {name}");
                        break;
                    default:
                        Assert.Fail($"parameter {name} is not a date value: {parameter.Value?.GetType().Name ?? "null"}");
                        break;
                }
            }
        }
    }
}
