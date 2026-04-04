using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataResidency;

/// <summary>
/// Unit tests for <see cref="DataResidencyAutoRegistrationHostedService"/> verifying
/// attribute scanning and fluent policy creation at startup.
/// </summary>
public class DataResidencyAutoRegistrationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_NoAssembliesAndNoFluentPolicies_ShouldSkip()
    {
        // Arrange
        var descriptor = new DataResidencyAutoRegistrationDescriptor([]);
        var options = Options.Create(new DataResidencyOptions { AutoRegisterFromAttributes = false });
        var scopeFactory = CreateScopeFactory();
        var logger = NullLogger<DataResidencyAutoRegistrationHostedService>.Instance;

        var sut = new DataResidencyAutoRegistrationHostedService(
            descriptor, options, scopeFactory, logger);

        // Act — should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithFluentPolicies_ShouldCreatePolicies()
    {
        // Arrange
        var policyService = Substitute.For<IResidencyPolicyService>();
        policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ResidencyPolicyReadModel>(
                EncinaErrors.Create(code: "nf", message: "Not found")));
        policyService.CreatePolicyAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<bool>(), Arg.Any<IReadOnlyList<TransferLegalBasis>>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var scopeFactory = CreateScopeFactory(policyService);

        var fluentEntry = new DataResidencyFluentPolicyEntry(
            "healthcare-data",
            [RegionRegistry.DE, RegionRegistry.FR],
            true,
            [TransferLegalBasis.AdequacyDecision]);

        var fluentDescriptor = new DataResidencyFluentPolicyDescriptor([fluentEntry]);

        var descriptor = new DataResidencyAutoRegistrationDescriptor([]);
        var options = Options.Create(new DataResidencyOptions { AutoRegisterFromAttributes = false });
        var logger = NullLogger<DataResidencyAutoRegistrationHostedService>.Instance;

        var sut = new DataResidencyAutoRegistrationHostedService(
            descriptor, options, scopeFactory, logger, fluentDescriptor);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        await policyService.Received(1).CreatePolicyAsync(
            "healthcare-data", Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<bool>(), Arg.Any<IReadOnlyList<TransferLegalBasis>>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WithExistingPolicy_ShouldSkipCreation()
    {
        // Arrange
        var policyService = Substitute.For<IResidencyPolicyService>();
        policyService.GetPolicyByCategoryAsync("healthcare-data", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ResidencyPolicyReadModel>(new ResidencyPolicyReadModel()));

        var scopeFactory = CreateScopeFactory(policyService);

        var fluentEntry = new DataResidencyFluentPolicyEntry(
            "healthcare-data",
            [RegionRegistry.DE],
            true,
            []);

        var fluentDescriptor = new DataResidencyFluentPolicyDescriptor([fluentEntry]);
        var descriptor = new DataResidencyAutoRegistrationDescriptor([]);
        var options = Options.Create(new DataResidencyOptions { AutoRegisterFromAttributes = false });
        var logger = NullLogger<DataResidencyAutoRegistrationHostedService>.Instance;

        var sut = new DataResidencyAutoRegistrationHostedService(
            descriptor, options, scopeFactory, logger, fluentDescriptor);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert — CreatePolicyAsync should NOT be called
        await policyService.DidNotReceive().CreatePolicyAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<bool>(), Arg.Any<IReadOnlyList<TransferLegalBasis>>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteImmediately()
    {
        // Arrange
        var descriptor = new DataResidencyAutoRegistrationDescriptor([]);
        var options = Options.Create(new DataResidencyOptions());
        var scopeFactory = CreateScopeFactory();
        var logger = NullLogger<DataResidencyAutoRegistrationHostedService>.Instance;

        var sut = new DataResidencyAutoRegistrationHostedService(
            descriptor, options, scopeFactory, logger);

        // Act — should not throw
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithAssemblyScanning_ShouldScanForAttributes()
    {
        // Arrange
        var policyService = Substitute.For<IResidencyPolicyService>();
        // No policies exist for any category
        policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ResidencyPolicyReadModel>(
                EncinaErrors.Create(code: "nf", message: "Not found")));
        policyService.CreatePolicyAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<bool>(), Arg.Any<IReadOnlyList<TransferLegalBasis>>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var scopeFactory = CreateScopeFactory(policyService);

        // Scan current test assembly which should NOT have DataResidency attributes
        var descriptor = new DataResidencyAutoRegistrationDescriptor([typeof(DataResidencyAutoRegistrationHostedServiceTests).Assembly]);
        var options = Options.Create(new DataResidencyOptions { AutoRegisterFromAttributes = true });
        var logger = NullLogger<DataResidencyAutoRegistrationHostedService>.Instance;

        var sut = new DataResidencyAutoRegistrationHostedService(
            descriptor, options, scopeFactory, logger);

        // Act — should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    private static IServiceScopeFactory CreateScopeFactory(IResidencyPolicyService? policyService = null)
    {
        var services = new ServiceCollection();
        if (policyService is not null)
        {
            services.AddSingleton(policyService);
        }
        else
        {
            services.AddSingleton(Substitute.For<IResidencyPolicyService>());
        }

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }
}
