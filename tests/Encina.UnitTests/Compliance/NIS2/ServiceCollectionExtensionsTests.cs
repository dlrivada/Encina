using Encina;
using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using Shouldly;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(Action<NIS2Options>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(configure ?? (o =>
        {
            o.Sector = NIS2Sector.Energy;
            o.CompetentAuthority = "test@authority.eu";
        }));
        return services.BuildServiceProvider();
    }

    #region AddEncinaNIS2_ShouldRegisterComplianceValidator

    [Fact]
    public void AddEncinaNIS2_ShouldRegisterComplianceValidator()
    {
        // Arrange & Act
        using var provider = BuildProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetService<INIS2ComplianceValidator>();
        validator.ShouldNotBeNull();
    }

    #endregion

    #region AddEncinaNIS2_ShouldRegisterIncidentHandler

    [Fact]
    public void AddEncinaNIS2_ShouldRegisterIncidentHandler()
    {
        // Arrange & Act
        using var provider = BuildProvider();

        // Assert
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetService<INIS2IncidentHandler>();
        handler.ShouldNotBeNull();
    }

    #endregion

    #region AddEncinaNIS2_ShouldRegisterSupplyChainValidator

    [Fact]
    public void AddEncinaNIS2_ShouldRegisterSupplyChainValidator()
    {
        // Arrange & Act
        using var provider = BuildProvider();

        // Assert
        var validator = provider.GetService<ISupplyChainSecurityValidator>();
        validator.ShouldNotBeNull();
    }

    #endregion

    #region AddEncinaNIS2_ShouldRegisterMFAEnforcer

    [Fact]
    public void AddEncinaNIS2_ShouldRegisterMFAEnforcer()
    {
        // Arrange & Act
        using var provider = BuildProvider();

        // Assert
        var enforcer = provider.GetService<IMFAEnforcer>();
        enforcer.ShouldNotBeNull();
    }

    #endregion

    #region AddEncinaNIS2_ShouldRegisterEncryptionValidator

    [Fact]
    public void AddEncinaNIS2_ShouldRegisterEncryptionValidator()
    {
        // Arrange & Act
        using var provider = BuildProvider();

        // Assert
        var validator = provider.GetService<IEncryptionValidator>();
        validator.ShouldNotBeNull();
    }

    #endregion

    #region AddEncinaNIS2_ShouldRegisterMeasureEvaluators

    [Fact]
    public void AddEncinaNIS2_ShouldRegisterMeasureEvaluators()
    {
        // Arrange & Act
        using var provider = BuildProvider();

        // Assert
        var evaluators = provider.GetServices<INIS2MeasureEvaluator>();
        evaluators.Count().ShouldBe(10);
    }

    #endregion

    #region AddEncinaNIS2_ShouldRegisterPipelineBehavior

    [Fact]
    public void AddEncinaNIS2_ShouldRegisterPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(o =>
        {
            o.Sector = NIS2Sector.Energy;
            o.CompetentAuthority = "test@authority.eu";
        });

        // Act & Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
        descriptor.ShouldNotBeNull();
    }

    #endregion
}
