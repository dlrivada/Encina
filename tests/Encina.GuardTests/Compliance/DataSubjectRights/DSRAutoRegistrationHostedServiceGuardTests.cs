using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DSRAutoRegistrationHostedService"/> verifying null parameter handling and hosted service lifecycle.
/// </summary>
public class DSRAutoRegistrationHostedServiceGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDescriptor_ThrowsArgumentNullException()
    {
        var options = Options.Create(new DataSubjectRightsOptions());

        var act = () => new DSRAutoRegistrationHostedService(
            null!, options, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("descriptor");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var descriptor = new DSRAutoRegistrationDescriptor([]);

        var act = () => new DSRAutoRegistrationHostedService(
            descriptor, null!, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var descriptor = new DSRAutoRegistrationDescriptor([]);
        var options = Options.Create(new DataSubjectRightsOptions());

        var act = () => new DSRAutoRegistrationHostedService(
            descriptor, options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public async Task StartAsync_EmptyAssemblies_CompletesWithoutError()
    {
        // Arrange
        var descriptor = new DSRAutoRegistrationDescriptor([]);
        var options = Options.Create(new DataSubjectRightsOptions { AutoRegisterFromAttributes = true });
        var sut = new DSRAutoRegistrationHostedService(descriptor, options, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        // Act & Assert - should complete without throwing
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_AutoRegisterDisabled_CompletesWithoutError()
    {
        // Arrange
        var descriptor = new DSRAutoRegistrationDescriptor([typeof(DSRAutoRegistrationHostedServiceGuardTests).Assembly]);
        var options = Options.Create(new DataSubjectRightsOptions { AutoRegisterFromAttributes = false });
        var sut = new DSRAutoRegistrationHostedService(descriptor, options, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        // Act & Assert
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithAssembly_ScansAndCompletes()
    {
        // Arrange - scan current assembly (likely no PersonalDataAttribute, but exercises the code path)
        var descriptor = new DSRAutoRegistrationDescriptor([typeof(DSRAutoRegistrationHostedServiceGuardTests).Assembly]);
        var options = Options.Create(new DataSubjectRightsOptions { AutoRegisterFromAttributes = true });
        var sut = new DSRAutoRegistrationHostedService(descriptor, options, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        // Act & Assert
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutError()
    {
        // Arrange
        var descriptor = new DSRAutoRegistrationDescriptor([]);
        var options = Options.Create(new DataSubjectRightsOptions());
        var sut = new DSRAutoRegistrationHostedService(descriptor, options, NullLogger<DSRAutoRegistrationHostedService>.Instance);

        // Act & Assert
        await sut.StopAsync(CancellationToken.None);
    }

    #endregion
}
