using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/> verifying that
/// <see cref="ServiceCollectionExtensions.AddEncinaCrossBorderTransfer"/> registers a default
/// <see cref="IAdequacyDecisionProvider"/>, so <see cref="ITransferValidator"/> no longer needs
/// the caller to also call <c>AddEncinaDataResidency()</c> just to satisfy that one dependency
/// (see #1285). It does not cover <c>AddCrossBorderTransferAggregates()</c>, the separate,
/// documented call that supplies the Marten-backed <c>ITIAService</c>/<c>ISCCService</c>/
/// <c>IApprovedTransferService</c> collaborators <see cref="ITransferValidator"/> also needs.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaCrossBorderTransfer_WithoutDataResidency_RegistersAdequacyDecisionProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCrossBorderTransfer();

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(IAdequacyDecisionProvider));
    }

    [Fact]
    public void AddEncinaCrossBorderTransfer_WithoutDataResidencyButWithAggregateCollaboratorsSupplied_ResolvesTransferValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // DefaultTransferValidator also needs ITIAService/ISCCService/IApprovedTransferService.
        // Their Marten-backed defaults require IAggregateRepository<T>, which only
        // AddCrossBorderTransferAggregates() (a separate, documented call — see
        // ServiceCollectionExtensions' <remarks>) registers; that requirement is orthogonal to
        // #1285, which is only about IAdequacyDecisionProvider. This test does NOT prove that
        // AddEncinaCrossBorderTransfer() alone (with no other calls) can build with
        // ValidateOnBuild — it still can't, because of the Marten aggregates gap, not because of
        // this bug. Pre-registering substitutes for the three aggregate-backed collaborators lets
        // TryAdd skip the Marten-backed defaults, isolating the assertion to the fix under test:
        // once IAdequacyDecisionProvider is supplied (by this fix) and the aggregate-backed
        // collaborators are supplied (by the caller, here via substitutes), ITransferValidator
        // resolves without ever calling AddEncinaDataResidency().
        services.AddSingleton(Substitute.For<ITIAService>());
        services.AddSingleton(Substitute.For<ISCCService>());
        services.AddSingleton(Substitute.For<IApprovedTransferService>());

        services.AddEncinaCrossBorderTransfer();

        // Act
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = serviceProvider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<ITransferValidator>();

        // Assert
        validator.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDataResidencyThenAddEncinaCrossBorderTransfer_RegistersExactlyOneAdequacyDecisionProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDataResidency();
        services.AddEncinaCrossBorderTransfer();

        // Assert
        services.Count(sd => sd.ServiceType == typeof(IAdequacyDecisionProvider)).ShouldBe(1);
    }

    [Fact]
    public void AddEncinaCrossBorderTransferThenAddEncinaDataResidency_RegistersExactlyOneAdequacyDecisionProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCrossBorderTransfer();
        services.AddEncinaDataResidency();

        // Assert
        services.Count(sd => sd.ServiceType == typeof(IAdequacyDecisionProvider)).ShouldBe(1);
    }
}
