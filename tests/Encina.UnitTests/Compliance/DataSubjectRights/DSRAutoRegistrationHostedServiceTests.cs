using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="DSRAutoRegistrationHostedService"/>.
/// </summary>
public class DSRAutoRegistrationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_AutoRegisterDisabledWithAssemblies_SkipsDiscovery()
    {
        // Arrange
        var descriptor = new DSRAutoRegistrationDescriptor([typeof(DSRAutoRegistrationHostedServiceTests).Assembly]);
        var options = Options.Create(new DataSubjectRightsOptions { AutoRegisterFromAttributes = false });
        var sut = new DSRAutoRegistrationHostedService(descriptor, options, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        // Act & Assert - should skip without error
        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_AutoRegisterEnabledEmptyAssemblies_SkipsDiscovery()
    {
        // Arrange
        var descriptor = new DSRAutoRegistrationDescriptor([]);
        var options = Options.Create(new DataSubjectRightsOptions { AutoRegisterFromAttributes = true });
        var sut = new DSRAutoRegistrationHostedService(descriptor, options, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        // Act & Assert
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithAssembly_DiscoversAndCompletes()
    {
        // Arrange - scan an assembly; exercises DiscoverPersonalDataFields
        var descriptor = new DSRAutoRegistrationDescriptor([typeof(DataSubjectRightsOptions).Assembly]);
        var options = Options.Create(new DataSubjectRightsOptions { AutoRegisterFromAttributes = true });
        var sut = new DSRAutoRegistrationHostedService(descriptor, options, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert - completes without throwing (logged output only)
    }
}
