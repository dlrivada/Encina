using Encina.Compliance.Anonymization;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="AnonymizationAutoRegistrationHostedService"/> verifying
/// attribute scanning behavior at startup.
/// </summary>
public class AnonymizationAutoRegistrationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_AutoRegisterDisabled_ShouldCompleteWithoutError()
    {
        // Arrange
        var descriptor = new AnonymizationAutoRegistrationDescriptor([]);
        var options = Options.Create(new AnonymizationOptions { AutoRegisterFromAttributes = false });
        var logger = NullLogger<AnonymizationAutoRegistrationHostedService>.Instance;

        var sut = new AnonymizationAutoRegistrationHostedService(descriptor, options, logger);

        // Act & Assert — should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_EmptyAssemblies_ShouldCompleteWithoutError()
    {
        // Arrange
        var descriptor = new AnonymizationAutoRegistrationDescriptor([]);
        var options = Options.Create(new AnonymizationOptions { AutoRegisterFromAttributes = true });
        var logger = NullLogger<AnonymizationAutoRegistrationHostedService>.Instance;

        var sut = new AnonymizationAutoRegistrationHostedService(descriptor, options, logger);

        // Act & Assert — should not throw (0 assemblies => skip)
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithAssembly_ShouldScanWithoutError()
    {
        // Arrange — scan the current test assembly (no anonymization attributes expected)
        var descriptor = new AnonymizationAutoRegistrationDescriptor(
            [typeof(AnonymizationAutoRegistrationHostedServiceTests).Assembly]);
        var options = Options.Create(new AnonymizationOptions { AutoRegisterFromAttributes = true });
        var logger = NullLogger<AnonymizationAutoRegistrationHostedService>.Instance;

        var sut = new AnonymizationAutoRegistrationHostedService(descriptor, options, logger);

        // Act & Assert — should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteGracefully()
    {
        // Arrange
        var descriptor = new AnonymizationAutoRegistrationDescriptor([]);
        var options = Options.Create(new AnonymizationOptions());
        var logger = NullLogger<AnonymizationAutoRegistrationHostedService>.Instance;

        var sut = new AnonymizationAutoRegistrationHostedService(descriptor, options, logger);

        // Act
        var task = sut.StopAsync(CancellationToken.None);

        // Assert
        task.IsCompletedSuccessfully.ShouldBeTrue();
        await task;
    }
}
