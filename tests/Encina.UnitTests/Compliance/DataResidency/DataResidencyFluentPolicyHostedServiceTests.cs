using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataResidency;

/// <summary>
/// Unit tests for <see cref="DataResidencyFluentPolicyHostedService"/> verifying
/// fluent policy creation at startup when auto-registration is disabled.
/// </summary>
public class DataResidencyFluentPolicyHostedServiceTests
{
    [Fact]
    public async Task StartAsync_NoPolicies_ShouldReturnImmediately()
    {
        // Arrange
        var descriptor = new DataResidencyFluentPolicyDescriptor([]);
        var scopeFactory = CreateScopeFactory();
        var logger = NullLogger<DataResidencyFluentPolicyHostedService>.Instance;

        var sut = new DataResidencyFluentPolicyHostedService(descriptor, scopeFactory, logger);

        // Act — should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithNewPolicy_ShouldCreatePolicy()
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

        var entry = new DataResidencyFluentPolicyEntry(
            "financial-data", [RegionRegistry.DE], false, [TransferLegalBasis.StandardContractualClauses]);
        var descriptor = new DataResidencyFluentPolicyDescriptor([entry]);
        var logger = NullLogger<DataResidencyFluentPolicyHostedService>.Instance;

        var sut = new DataResidencyFluentPolicyHostedService(descriptor, scopeFactory, logger);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        await policyService.Received(1).CreatePolicyAsync(
            "financial-data", Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<bool>(), Arg.Any<IReadOnlyList<TransferLegalBasis>>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WithExistingPolicy_ShouldSkipCreation()
    {
        // Arrange
        var policyService = Substitute.For<IResidencyPolicyService>();
        policyService.GetPolicyByCategoryAsync("financial-data", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ResidencyPolicyReadModel>(new ResidencyPolicyReadModel()));

        var scopeFactory = CreateScopeFactory(policyService);

        var entry = new DataResidencyFluentPolicyEntry(
            "financial-data", [RegionRegistry.DE], false, []);
        var descriptor = new DataResidencyFluentPolicyDescriptor([entry]);
        var logger = NullLogger<DataResidencyFluentPolicyHostedService>.Instance;

        var sut = new DataResidencyFluentPolicyHostedService(descriptor, scopeFactory, logger);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        await policyService.DidNotReceive().CreatePolicyAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<bool>(), Arg.Any<IReadOnlyList<TransferLegalBasis>>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_CreatePolicyFails_ShouldNotThrow()
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
            .Returns(Left<EncinaError, Guid>(EncinaErrors.Create(code: "err", message: "Failed")));

        var scopeFactory = CreateScopeFactory(policyService);

        var entry = new DataResidencyFluentPolicyEntry(
            "data", [RegionRegistry.DE], false, []);
        var descriptor = new DataResidencyFluentPolicyDescriptor([entry]);
        var logger = NullLogger<DataResidencyFluentPolicyHostedService>.Instance;

        var sut = new DataResidencyFluentPolicyHostedService(descriptor, scopeFactory, logger);

        // Act — should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_CreatePolicyThrows_ShouldNotThrow()
    {
        // Arrange
        var policyService = Substitute.For<IResidencyPolicyService>();
        policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ResidencyPolicyReadModel>(
                EncinaErrors.Create(code: "nf", message: "Not found")));
        policyService.When(x => x.CreatePolicyAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<bool>(), Arg.Any<IReadOnlyList<TransferLegalBasis>>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new TimeoutException("Unexpected"));

        var scopeFactory = CreateScopeFactory(policyService);

        var entry = new DataResidencyFluentPolicyEntry(
            "data", [RegionRegistry.DE], false, []);
        var descriptor = new DataResidencyFluentPolicyDescriptor([entry]);
        var logger = NullLogger<DataResidencyFluentPolicyHostedService>.Instance;

        var sut = new DataResidencyFluentPolicyHostedService(descriptor, scopeFactory, logger);

        // Act — should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_ShouldComplete()
    {
        // Arrange
        var descriptor = new DataResidencyFluentPolicyDescriptor([]);
        var scopeFactory = CreateScopeFactory();
        var logger = NullLogger<DataResidencyFluentPolicyHostedService>.Instance;

        var sut = new DataResidencyFluentPolicyHostedService(descriptor, scopeFactory, logger);

        // Act & Assert
        await sut.StopAsync(CancellationToken.None);
    }

    private static IServiceScopeFactory CreateScopeFactory(IResidencyPolicyService? policyService = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(policyService ?? Substitute.For<IResidencyPolicyService>());
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }
}
