using Encina.Sharding.Resharding;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Sharding.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingServiceCollectionExtensions"/>.
/// Verifies guard clauses, option validation, and service registration.
/// </summary>
public sealed class ReshardingServiceCollectionExtensionsTests
{
    #region Guard Clauses

    [Fact]
    public void AddResharding_NullServices_ThrowsArgumentNullException()
    {
        var builder = new ReshardingBuilder();

        Should.Throw<ArgumentNullException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(null!, builder));
    }

    [Fact]
    public void AddResharding_NullBuilder_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(services, null!));
    }

    #endregion

    #region Option Validation - CopyBatchSize

    [Fact]
    public void AddResharding_CopyBatchSizeZero_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var builder = new ReshardingBuilder { CopyBatchSize = 0 };

        Should.Throw<InvalidOperationException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(services, builder));
    }

    [Fact]
    public void AddResharding_CopyBatchSizeNegative_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var builder = new ReshardingBuilder { CopyBatchSize = -1 };

        Should.Throw<InvalidOperationException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(services, builder));
    }

    #endregion

    #region Option Validation - CdcLagThreshold

    [Fact]
    public void AddResharding_CdcLagThresholdZero_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var builder = new ReshardingBuilder { CdcLagThreshold = TimeSpan.Zero };

        Should.Throw<InvalidOperationException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(services, builder));
    }

    [Fact]
    public void AddResharding_CdcLagThresholdNegative_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var builder = new ReshardingBuilder { CdcLagThreshold = TimeSpan.FromSeconds(-1) };

        Should.Throw<InvalidOperationException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(services, builder));
    }

    #endregion

    #region Option Validation - CutoverTimeout

    [Fact]
    public void AddResharding_CutoverTimeoutZero_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var builder = new ReshardingBuilder { CutoverTimeout = TimeSpan.Zero };

        Should.Throw<InvalidOperationException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(services, builder));
    }

    #endregion

    #region Option Validation - CleanupRetentionPeriod

    [Fact]
    public void AddResharding_CleanupRetentionPeriodZero_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var builder = new ReshardingBuilder { CleanupRetentionPeriod = TimeSpan.Zero };

        Should.Throw<InvalidOperationException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(services, builder));
    }

    #endregion

    #region Service Registration

    [Fact]
    public void AddResharding_ValidBuilder_RegistersReshardingOptions()
    {
        var services = new ServiceCollection();
        var builder = new ReshardingBuilder();

        ReshardingServiceCollectionExtensions.AddResharding(services, builder);

        services.ShouldContain(sd =>
            sd.ServiceType == typeof(ReshardingOptions) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddResharding_ValidBuilder_RegistersIReshardingOrchestrator()
    {
        var services = new ServiceCollection();
        var builder = new ReshardingBuilder();

        ReshardingServiceCollectionExtensions.AddResharding(services, builder);

        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IReshardingOrchestrator) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    #endregion
}
