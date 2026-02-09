using FsCheck;
using FsCheck.Xunit;
using Encina.Cdc;

namespace Encina.PropertyTests.Cdc;

/// <summary>
/// Property-based tests for <see cref="CdcOptions"/> invariants.
/// Verifies configuration properties maintain expected constraints for all inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class CdcOptionsPropertyTests
{
    #region PollingInterval Invariants

    [Property(MaxTest = 100)]
    public bool Property_PollingInterval_AlwaysPreservesPositiveTimeSpan(PositiveInt ticks)
    {
        // Property: Setting PollingInterval to any positive TimeSpan always returns what was set
        var options = new CdcOptions();
        var interval = TimeSpan.FromTicks(ticks.Get);

        options.PollingInterval = interval;

        return options.PollingInterval == interval && options.PollingInterval >= TimeSpan.Zero;
    }

    [Property(MaxTest = 50)]
    public bool Property_PollingInterval_DefaultIsPositive()
    {
        // Property: Default PollingInterval is always >= TimeSpan.Zero
        var options = new CdcOptions();

        return options.PollingInterval >= TimeSpan.Zero;
    }

    [Property(MaxTest = 100)]
    public bool Property_PollingInterval_SetThenGet_RoundTrips(PositiveInt seconds)
    {
        // Property: Setting then getting PollingInterval always returns the same value
        var capped = seconds.Get % 3600 + 1; // Cap at 1 hour for reasonable values
        var options = new CdcOptions();
        var interval = TimeSpan.FromSeconds(capped);

        options.PollingInterval = interval;

        return options.PollingInterval == interval;
    }

    #endregion

    #region BatchSize Invariants

    [Property(MaxTest = 100)]
    public bool Property_BatchSize_PreservesPositiveValues(PositiveInt batchSize)
    {
        // Property: Setting BatchSize to any positive int always returns what was set
        var options = new CdcOptions { BatchSize = batchSize.Get };

        return options.BatchSize == batchSize.Get && options.BatchSize > 0;
    }

    [Property(MaxTest = 50)]
    public bool Property_BatchSize_DefaultIsPositive()
    {
        // Property: Default BatchSize is always > 0
        var options = new CdcOptions();

        return options.BatchSize > 0;
    }

    #endregion

    #region MaxRetries Invariants

    [Property(MaxTest = 100)]
    public bool Property_MaxRetries_PreservesNonNegativeValues(NonNegativeInt maxRetries)
    {
        // Property: Setting MaxRetries to any non-negative int always returns what was set
        var options = new CdcOptions { MaxRetries = maxRetries.Get };

        return options.MaxRetries == maxRetries.Get && options.MaxRetries >= 0;
    }

    [Property(MaxTest = 50)]
    public bool Property_MaxRetries_DefaultIsNonNegative()
    {
        // Property: Default MaxRetries is always >= 0
        var options = new CdcOptions();

        return options.MaxRetries >= 0;
    }

    #endregion

    #region BaseRetryDelay Invariants

    [Property(MaxTest = 100)]
    public bool Property_BaseRetryDelay_PreservesPositiveTimeSpan(PositiveInt milliseconds)
    {
        // Property: Setting BaseRetryDelay to any positive TimeSpan returns what was set
        var options = new CdcOptions();
        var delay = TimeSpan.FromMilliseconds(milliseconds.Get);

        options.BaseRetryDelay = delay;

        return options.BaseRetryDelay == delay;
    }

    [Property(MaxTest = 50)]
    public bool Property_BaseRetryDelay_DefaultIsPositive()
    {
        // Property: Default BaseRetryDelay is always positive
        var options = new CdcOptions();

        return options.BaseRetryDelay > TimeSpan.Zero;
    }

    #endregion

    #region Boolean Properties Invariants

    [Property(MaxTest = 100)]
    public bool Property_Enabled_SetThenGet_RoundTrips(bool value)
    {
        // Property: Setting Enabled always returns what was set
        var options = new CdcOptions { Enabled = value };

        return options.Enabled == value;
    }

    [Property(MaxTest = 100)]
    public bool Property_EnablePositionTracking_SetThenGet_RoundTrips(bool value)
    {
        // Property: Setting EnablePositionTracking always returns what was set
        var options = new CdcOptions { EnablePositionTracking = value };

        return options.EnablePositionTracking == value;
    }

    [Property(MaxTest = 100)]
    public bool Property_UseMessagingBridge_SetThenGet_RoundTrips(bool value)
    {
        // Property: Setting UseMessagingBridge always returns what was set
        var options = new CdcOptions { UseMessagingBridge = value };

        return options.UseMessagingBridge == value;
    }

    [Property(MaxTest = 100)]
    public bool Property_UseOutboxCdc_SetThenGet_RoundTrips(bool value)
    {
        // Property: Setting UseOutboxCdc always returns what was set
        var options = new CdcOptions { UseOutboxCdc = value };

        return options.UseOutboxCdc == value;
    }

    #endregion

    #region Defaults Invariants

    [Property(MaxTest = 50)]
    public bool Property_Defaults_AreConsistent()
    {
        // Property: Default options always have opt-in features disabled
        var options = new CdcOptions();

        return !options.Enabled
            && !options.UseMessagingBridge
            && !options.UseOutboxCdc
            && options.EnablePositionTracking
            && options.PollingInterval == TimeSpan.FromSeconds(5)
            && options.BatchSize == 100
            && options.MaxRetries == 3
            && options.BaseRetryDelay == TimeSpan.FromSeconds(1)
            && options.TableFilters.Length == 0;
    }

    #endregion

    #region TableFilters Invariants

    [Property(MaxTest = 100)]
    public bool Property_TableFilters_PreservesArray(List<NonEmptyString> filters)
    {
        // Property: Setting TableFilters always preserves the array content
        var input = (filters ?? []).Select(f => f.Get).ToArray();
        var options = new CdcOptions { TableFilters = input };

        return options.TableFilters.SequenceEqual(input);
    }

    #endregion

    #region Property Independence

    [Property(MaxTest = 100)]
    public bool Property_AllProperties_AreIndependent(
        bool enabled, PositiveInt batchSize, NonNegativeInt maxRetries,
        bool useMessagingBridge, bool useOutboxCdc)
    {
        // Property: Setting any property does not affect other properties
        var options = new CdcOptions
        {
            Enabled = enabled,
            BatchSize = batchSize.Get,
            MaxRetries = maxRetries.Get,
            UseMessagingBridge = useMessagingBridge,
            UseOutboxCdc = useOutboxCdc,
        };

        return options.Enabled == enabled
            && options.BatchSize == batchSize.Get
            && options.MaxRetries == maxRetries.Get
            && options.UseMessagingBridge == useMessagingBridge
            && options.UseOutboxCdc == useOutboxCdc;
    }

    #endregion
}
