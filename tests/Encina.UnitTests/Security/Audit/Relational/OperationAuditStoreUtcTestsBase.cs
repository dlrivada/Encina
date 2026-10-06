using System.Data.Common;
using Encina.Security.Audit;
using LanguageExt;

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
        await Settle(store.RecordAsync(entry, TestContext.Current.CancellationToken));

        // Assert
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
        await Settle(store.GetByUserAsync("user-1", InstantOfKind(kind), InstantOfKind(kind), TestContext.Current.CancellationToken));

        // Assert
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
        await Settle(store.QueryAsync(query, TestContext.Current.CancellationToken));

        // Assert
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
        await Settle(store.PurgeEntriesAsync(InstantOfKind(kind), TestContext.Current.CancellationToken));

        // Assert
        AssertBoundAsExpectedUtc(connection, "OlderThanUtc");
    }

    /// <summary>
    /// Gets a value indicating whether the store completes against the double. A store whose double aborts
    /// the command after capturing its parameters (EF Core) returns <see langword="false"/>.
    /// </summary>
    protected virtual bool ExpectsSuccess => true;

    private async Task Settle<T>(ValueTask<Either<EncinaError, T>> operation)
    {
        try
        {
            var result = await operation;
            if (ExpectsSuccess)
            {
                result.IsRight.ShouldBeTrue();
            }
        }
        catch (Exception) when (!ExpectsSuccess)
        {
            // The double aborted the command after capturing its parameters.
        }
    }

    private static DateTime InstantOfKind(DateTimeKind kind) => kind switch
    {
        DateTimeKind.Utc => ExpectedUtc,
        DateTimeKind.Local => ExpectedUtc.ToLocalTime(),
        _ => DateTime.SpecifyKind(ExpectedUtc, DateTimeKind.Unspecified)
    };

    /// <summary>
    /// Gets a value indicating whether the store names its date parameters after the criteria. EF Core generates
    /// its own parameter names, so for it the assertion covers every date value bound, by count.
    /// </summary>
    protected virtual bool BindsNamedParameters => true;

    private void AssertBoundAsExpectedUtc(RecordingDbConnection connection, params string[] parameterNames)
    {
        if (!BindsNamedParameters)
        {
            var dates = connection.BoundParameters.Where(p => p.Value is DateTime or DateTimeOffset).ToList();
            dates.Count.ShouldBe(parameterNames.Length, "number of date parameters bound");
            foreach (var parameter in dates)
            {
                AssertExpectedUtc(parameter.Name, parameter.Value);
            }

            return;
        }

        foreach (var name in parameterNames)
        {
            var bound = connection.BoundParameters.Where(p => p.Name == name).ToList();
            bound.ShouldNotBeEmpty($"parameter {name} was never bound");

            foreach (var parameter in bound)
            {
                AssertExpectedUtc(name, parameter.Value);
            }
        }
    }

    private static void AssertExpectedUtc(string name, object? value)
    {
        switch (value)
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
                Assert.Fail($"parameter {name} is not a date value: {value?.GetType().Name ?? "null"}");
                break;
        }
    }
}
