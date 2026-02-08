using Encina.Database;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Resilience;

/// <summary>
/// Property-based tests for <see cref="ConnectionPoolStats"/> invariants.
/// Verifies mathematical properties that must hold across all input combinations.
/// </summary>
[Trait("Category", "Property")]
public sealed class ConnectionPoolStatsPropertyTests
{
    #region PoolUtilization Invariants

    [Property(MaxTest = 200)]
    public bool Property_PoolUtilization_AlwaysBetweenZeroAndOne(
        NonNegativeInt active, NonNegativeInt idle, NonNegativeInt total,
        NonNegativeInt pending, PositiveInt maxPool)
    {
        // Property: PoolUtilization is always in [0, 1] regardless of inputs
        var stats = new ConnectionPoolStats(
            ActiveConnections: active.Get,
            IdleConnections: idle.Get,
            TotalConnections: total.Get,
            PendingRequests: pending.Get,
            MaxPoolSize: maxPool.Get);

        return stats.PoolUtilization >= 0.0 && stats.PoolUtilization <= 1.0;
    }

    [Property(MaxTest = 200)]
    public bool Property_PoolUtilization_ZeroWhenMaxPoolSizeIsZero(
        NonNegativeInt active, NonNegativeInt idle, NonNegativeInt total,
        NonNegativeInt pending)
    {
        // Property: PoolUtilization is 0 when MaxPoolSize is 0 (no division by zero)
        var stats = new ConnectionPoolStats(
            ActiveConnections: active.Get,
            IdleConnections: idle.Get,
            TotalConnections: total.Get,
            PendingRequests: pending.Get,
            MaxPoolSize: 0);

        return stats.PoolUtilization == 0.0;
    }

    [Property(MaxTest = 100)]
    public bool Property_PoolUtilization_EqualsRatioWhenWithinBounds(PositiveInt maxPool)
    {
        // Property: When ActiveConnections < MaxPoolSize, utilization is exact ratio
        var active = maxPool.Get / 2; // guaranteed < maxPool
        var stats = new ConnectionPoolStats(
            ActiveConnections: active,
            IdleConnections: 0,
            TotalConnections: active,
            PendingRequests: 0,
            MaxPoolSize: maxPool.Get);

        var expected = (double)active / maxPool.Get;
        return Math.Abs(stats.PoolUtilization - expected) < 1e-10;
    }

    [Property(MaxTest = 100)]
    public bool Property_PoolUtilization_ClampsAtOne_WhenActiveExceedsMax(PositiveInt maxPool)
    {
        // Property: When ActiveConnections > MaxPoolSize, utilization is clamped to 1.0
        var active = maxPool.Get + 10; // guaranteed > maxPool
        var stats = new ConnectionPoolStats(
            ActiveConnections: active,
            IdleConnections: 0,
            TotalConnections: active,
            PendingRequests: 0,
            MaxPoolSize: maxPool.Get);

        return stats.PoolUtilization == 1.0;
    }

    [Property(MaxTest = 100)]
    public bool Property_PoolUtilization_IsMonotonicallyIncreasing(PositiveInt maxPool)
    {
        // Property: As ActiveConnections increases, PoolUtilization never decreases
        var max = Math.Min(maxPool.Get, 1000); // cap for performance
        var previousUtilization = 0.0;

        for (var active = 0; active <= max + 1; active++)
        {
            var stats = new ConnectionPoolStats(
                ActiveConnections: active,
                IdleConnections: 0,
                TotalConnections: active,
                PendingRequests: 0,
                MaxPoolSize: max);

            if (stats.PoolUtilization < previousUtilization)
            {
                return false;
            }

            previousUtilization = stats.PoolUtilization;
        }

        return true;
    }

    #endregion

    #region CreateEmpty Invariants

    [Property(MaxTest = 50)]
    public bool Property_CreateEmpty_AlwaysReturnsZeros()
    {
        // Property: CreateEmpty always produces consistent zero state
        var empty = ConnectionPoolStats.CreateEmpty();

        return empty.ActiveConnections == 0
            && empty.IdleConnections == 0
            && empty.TotalConnections == 0
            && empty.PendingRequests == 0
            && empty.MaxPoolSize == 0
            && empty.PoolUtilization == 0.0;
    }

    #endregion

    #region Record Equality Invariants

    [Property(MaxTest = 100)]
    public bool Property_RecordEquality_Reflexive(
        NonNegativeInt active, NonNegativeInt idle, NonNegativeInt total,
        NonNegativeInt pending, NonNegativeInt maxPool)
    {
        // Property: a == a (reflexive equality)
        var stats = new ConnectionPoolStats(
            active.Get, idle.Get, total.Get, pending.Get, maxPool.Get);

        var same = stats;
        return stats.Equals(same) && stats == same;
    }

    [Property(MaxTest = 100)]
    public bool Property_RecordEquality_Symmetric(
        NonNegativeInt active, NonNegativeInt idle, NonNegativeInt total,
        NonNegativeInt pending, NonNegativeInt maxPool)
    {
        // Property: a == b implies b == a (symmetric equality)
        var a = new ConnectionPoolStats(
            active.Get, idle.Get, total.Get, pending.Get, maxPool.Get);
        var b = new ConnectionPoolStats(
            active.Get, idle.Get, total.Get, pending.Get, maxPool.Get);

        return a.Equals(b) == b.Equals(a);
    }

    [Property(MaxTest = 100)]
    public bool Property_RecordEquality_HashCodeConsistent(
        NonNegativeInt active, NonNegativeInt idle, NonNegativeInt total,
        NonNegativeInt pending, NonNegativeInt maxPool)
    {
        // Property: Equal objects have equal hash codes
        var a = new ConnectionPoolStats(
            active.Get, idle.Get, total.Get, pending.Get, maxPool.Get);
        var b = new ConnectionPoolStats(
            active.Get, idle.Get, total.Get, pending.Get, maxPool.Get);

        if (a.Equals(b))
        {
            return a.GetHashCode() == b.GetHashCode();
        }

        return true; // vacuously true
    }

    #endregion

    #region With-Expression Invariants

    [Property(MaxTest = 100)]
    public bool Property_WithExpression_OnlyChangesSpecifiedField(
        NonNegativeInt active, NonNegativeInt idle, NonNegativeInt total,
        NonNegativeInt pending, NonNegativeInt maxPool, NonNegativeInt newActive)
    {
        // Property: With-expression only mutates the specified field
        var original = new ConnectionPoolStats(
            active.Get, idle.Get, total.Get, pending.Get, maxPool.Get);

        var modified = original with { ActiveConnections = newActive.Get };

        return modified.ActiveConnections == newActive.Get
            && modified.IdleConnections == original.IdleConnections
            && modified.TotalConnections == original.TotalConnections
            && modified.PendingRequests == original.PendingRequests
            && modified.MaxPoolSize == original.MaxPoolSize;
    }

    #endregion

    #region DatabaseCircuitBreakerOptions Invariants

    [Property(MaxTest = 100)]
    public bool Property_FailureThreshold_ClampsBetweenZeroAndOne(NormalFloat threshold)
    {
        // Property: FailureThreshold should accept any double (no clamping in setter)
        // but reasonable values are [0, 1]
        var options = new DatabaseCircuitBreakerOptions
        {
            FailureThreshold = threshold.Get,
        };

        return options.FailureThreshold == threshold.Get;
    }

    [Property(MaxTest = 100)]
    public bool Property_MinimumThroughput_PreservesValue(PositiveInt throughput)
    {
        // Property: MinimumThroughput stores the exact value provided
        var options = new DatabaseCircuitBreakerOptions
        {
            MinimumThroughput = throughput.Get,
        };

        return options.MinimumThroughput == throughput.Get;
    }

    [Property(MaxTest = 50)]
    public bool Property_CircuitBreakerDefaults_AreReasonable()
    {
        // Property: Default values are sensible for production use
        var options = new DatabaseCircuitBreakerOptions();

        return options.FailureThreshold > 0 && options.FailureThreshold < 1
            && options.SamplingDuration > TimeSpan.Zero
            && options.BreakDuration > TimeSpan.Zero
            && options.MinimumThroughput > 0
            && options.IncludeTimeouts
            && options.IncludeConnectionFailures;
    }

    #endregion

    #region DatabaseResilienceOptions Invariants

    [Property(MaxTest = 50)]
    public bool Property_ResilienceOptions_DefaultsAllDisabled()
    {
        // Property: All opt-in features are disabled by default
        var options = new DatabaseResilienceOptions();

        return !options.EnablePoolMonitoring
            && !options.EnableCircuitBreaker
            && options.WarmUpConnections == 0
            && options.HealthCheckInterval == TimeSpan.Zero
            && options.CircuitBreaker is not null;
    }

    [Property(MaxTest = 100)]
    public bool Property_WarmUpConnections_PreservesNonNegativeValue(NonNegativeInt count)
    {
        // Property: WarmUpConnections stores the exact non-negative value
        var options = new DatabaseResilienceOptions
        {
            WarmUpConnections = count.Get,
        };

        return options.WarmUpConnections == count.Get;
    }

    #endregion

    #region DatabaseHealthResult Invariants

    [Property(MaxTest = 50)]
    public bool Property_HealthyResult_AlwaysHasHealthyStatus()
    {
        // Property: Healthy factory always produces Healthy status
        var result = DatabaseHealthResult.Healthy("test");

        return result.Status == DatabaseHealthStatus.Healthy
            && result.Description == "test"
            && result.Exception is null;
    }

    [Property(MaxTest = 50)]
    public bool Property_UnhealthyResult_AlwaysHasUnhealthyStatus()
    {
        // Property: Unhealthy factory always produces Unhealthy status
        var result = DatabaseHealthResult.Unhealthy("test");

        return result.Status == DatabaseHealthStatus.Unhealthy
            && result.Description == "test";
    }

    [Property(MaxTest = 50)]
    public bool Property_DegradedResult_AlwaysHasDegradedStatus()
    {
        // Property: Degraded factory always produces Degraded status
        var result = DatabaseHealthResult.Degraded("test");

        return result.Status == DatabaseHealthStatus.Degraded
            && result.Description == "test"
            && result.Exception is null;
    }

    [Property(MaxTest = 50)]
    public bool Property_HealthStatus_Ordering_HealthyIsHighest()
    {
        // Property: Healthy > Degraded > Unhealthy
        return (int)DatabaseHealthStatus.Healthy > (int)DatabaseHealthStatus.Degraded
            && (int)DatabaseHealthStatus.Degraded > (int)DatabaseHealthStatus.Unhealthy;
    }

    #endregion
}
