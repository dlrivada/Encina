using Encina.Cdc;
using Encina.Cdc.DeadLetter;
using Encina.Cdc.Health;
using Encina.Messaging.Health;

using LanguageExt;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

using HealthCheckResult = Encina.Messaging.Health.HealthCheckResult;
using HealthStatus = Encina.Messaging.Health.HealthStatus;

#pragma warning disable CA1859 // Use concrete types for improved performance

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="CdcDeadLetterHealthCheck"/>.
/// </summary>
public sealed class CdcDeadLetterHealthCheckTests
{
    private readonly ICdcDeadLetterStore _store = Substitute.For<ICdcDeadLetterStore>();

    private CdcDeadLetterHealthCheck CreateHealthCheck(
        int warningThreshold = 10,
        int criticalThreshold = 100)
    {
        var options = new CdcDeadLetterHealthCheckOptions
        {
            WarningThreshold = warningThreshold,
            CriticalThreshold = criticalThreshold
        };
        return new CdcDeadLetterHealthCheck(_store, options);
    }

    #region Constructor

    [Fact]
    public void Constructor_NullStore_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => new CdcDeadLetterHealthCheck(null!, new CdcDeadLetterHealthCheckOptions()));
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => new CdcDeadLetterHealthCheck(_store, null!));
    }

    #endregion

    #region CheckHealthAsync - Healthy

    [Fact]
    public async Task CheckHealthAsync_PendingBelowWarning_ShouldReturnHealthy()
    {
        // Arrange
        var entries = CreateEntries(5);
        _store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>>(
                Right<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>(entries)));

        var healthCheck = CreateHealthCheck(warningThreshold: 10, criticalThreshold: 100);

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Healthy);
        result.Data.ShouldContainKey("pending_count");
        ((int)result.Data["pending_count"]).ShouldBe(5);
    }

    [Fact]
    public async Task CheckHealthAsync_ZeroPending_ShouldReturnHealthy()
    {
        // Arrange
        _store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>>(
                Right<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>(
                    System.Array.Empty<CdcDeadLetterEntry>())));

        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Healthy);
    }

    #endregion

    #region CheckHealthAsync - Degraded

    [Fact]
    public async Task CheckHealthAsync_PendingAboveWarning_BelowCritical_ShouldReturnDegraded()
    {
        // Arrange
        var entries = CreateEntries(15);
        _store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>>(
                Right<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>(entries)));

        var healthCheck = CreateHealthCheck(warningThreshold: 10, criticalThreshold: 100);

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Degraded);
        result.Description!.ShouldContain("15");
        result.Data.ShouldContainKey("warning_threshold");
    }

    #endregion

    #region CheckHealthAsync - Unhealthy

    [Fact]
    public async Task CheckHealthAsync_PendingAboveCritical_ShouldReturnUnhealthy()
    {
        // Arrange
        var entries = CreateEntries(101);
        _store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>>(
                Right<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>(entries)));

        var healthCheck = CreateHealthCheck(warningThreshold: 10, criticalThreshold: 100);

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("101");
        result.Data.ShouldContainKey("critical_threshold");
    }

    #endregion

    #region CheckHealthAsync - Store Error

    [Fact]
    public async Task CheckHealthAsync_StoreReturnsError_ShouldReturnUnhealthy()
    {
        // Arrange
        _store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>>(
                Left<EncinaError, IReadOnlyList<CdcDeadLetterEntry>>(
                    EncinaError.New("Database connection failed"))));

        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Failed to query");
        result.Data.ShouldContainKey("store_error");
    }

    #endregion

    #region Health Check Metadata

    [Fact]
    public void Name_ShouldBeExpected()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Name.ShouldBe("encina-cdc-dead-letter");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("cdc");
        healthCheck.Tags.ShouldContain("dead-letter");
        healthCheck.Tags.ShouldContain("ready");
    }

    #endregion

    #region Helpers

    private static IReadOnlyList<CdcDeadLetterEntry> CreateEntries(int count)
    {
        var entries = new List<CdcDeadLetterEntry>();
        var testPosition = new TestCdcPosition(0);
        var metadata = new ChangeMetadata(testPosition, DateTime.UtcNow, null, null, null);
        var changeEvent = new ChangeEvent("test_table", ChangeOperation.Insert, null, new { Id = 1 }, metadata);

        for (int i = 0; i < count; i++)
        {
            entries.Add(new CdcDeadLetterEntry(
                Id: Guid.NewGuid(),
                OriginalEvent: changeEvent,
                ErrorMessage: "Test error",
                StackTrace: "at Test",
                RetryCount: 3,
                FailedAtUtc: DateTime.UtcNow,
                ConnectorId: "test-connector",
                Status: CdcDeadLetterStatus.Pending));
        }

        return entries;
    }

    #endregion
}
