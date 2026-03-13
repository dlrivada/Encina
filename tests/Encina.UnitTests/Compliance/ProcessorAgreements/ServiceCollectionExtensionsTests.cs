#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Health;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Scheduling;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions.AddEncinaProcessorAgreements"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaProcessorAgreements_RegistersProcessorRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert
        var registry = provider.GetService<IProcessorRegistry>();
        registry.Should().NotBeNull();
        registry.Should().BeOfType<InMemoryProcessorRegistry>();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersDPAStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert
        var store = provider.GetService<IDPAStore>();
        store.Should().NotBeNull();
        store.Should().BeOfType<InMemoryDPAStore>();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersAuditStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert
        var auditStore = provider.GetService<IProcessorAuditStore>();
        auditStore.Should().NotBeNull();
        auditStore.Should().BeOfType<InMemoryProcessorAuditStore>();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetService<IDPAValidator>();
        validator.Should().NotBeNull();
        validator.Should().BeOfType<DefaultDPAValidator>();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(ProcessorValidationPipelineBehavior<,>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersExpirationHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommandHandler<CheckDPAExpirationCommand, Unit>) &&
            d.ImplementationType == typeof(CheckDPAExpirationHandler));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert
        var timeProvider = provider.GetService<TimeProvider>();
        timeProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaProcessorAgreements();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithConfigure_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
            options.MaxSubProcessorDepth = 5;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<ProcessorAgreementOptions>>().Value;
        options.EnforcementMode.Should().Be(ProcessorAgreementEnforcementMode.Block);
        options.MaxSubProcessorDepth.Should().Be(5);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithHealthCheck_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements(options =>
        {
            options.AddHealthCheck = true;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IHealthCheck) ||
            d.ServiceType == typeof(HealthCheckRegistration) ||
            (d.ImplementationType is not null &&
             d.ImplementationType == typeof(ProcessorAgreementHealthCheck)));

        // The health check is registered via AddHealthChecks().AddCheck<T>,
        // which uses IConfigureOptions<HealthCheckServiceOptions>.
        var healthCheckOptionDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<HealthCheckServiceOptions>));
        healthCheckOptionDescriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaProcessorAgreements(options =>
        {
            options.AddHealthCheck = false;
        });

        // Assert
        var healthCheckOptionDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<HealthCheckServiceOptions>));
        healthCheckOptionDescriptor.Should().BeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_CustomStoreBeforeCall_PreservesCustomStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDPAStore, CustomTestDPAStore>();

        // Act
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        // Assert — TryAdd should NOT overwrite the custom registration
        var store = provider.GetService<IDPAStore>();
        store.Should().NotBeNull();
        store.Should().BeOfType<CustomTestDPAStore>();
    }

    /// <summary>
    /// Custom IDPAStore for testing TryAdd behavior.
    /// </summary>
    private sealed class CustomTestDPAStore : IDPAStore
    {
        public ValueTask<Either<EncinaError, Unit>> AddAsync(
            DataProcessingAgreement agreement,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(default(Unit));

        public ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
            string processorId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, Option<DataProcessingAgreement>>>(
                Option<DataProcessingAgreement>.None);

        public ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
            string dpaId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, Option<DataProcessingAgreement>>>(
                Option<DataProcessingAgreement>.None);

        public ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
            string processorId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>>(
                new List<DataProcessingAgreement>());

        public ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByStatusAsync(
            DPAStatus status,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>>(
                new List<DataProcessingAgreement>());

        public ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetExpiringAsync(
            DateTimeOffset threshold,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>>(
                new List<DataProcessingAgreement>());

        public ValueTask<Either<EncinaError, Unit>> UpdateAsync(
            DataProcessingAgreement agreement,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(default(Unit));
    }
}
