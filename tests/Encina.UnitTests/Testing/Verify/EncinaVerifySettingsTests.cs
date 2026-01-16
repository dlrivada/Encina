using Encina.Testing.Verify;
using VerifyXunit;

namespace Encina.UnitTests.Testing.Verify;

/// <summary>
/// Unit tests for <see cref="EncinaVerifySettings"/>.
/// </summary>
public class EncinaVerifySettingsTests
{
    [Fact]
    public void Initialize_CanBeCalledMultipleTimes()
    {
        // Act - should not throw and remain initialized
        EncinaVerifySettings.Initialize();
        EncinaVerifySettings.Initialize();
        EncinaVerifySettings.Initialize();

        // Assert - settings should be initialized after multiple calls
        Assert.True(EncinaVerifySettings.IsInitialized);
    }

    [Fact]
    public async Task Initialize_ConfiguresScrubbers()
    {
        // Arrange
        EncinaVerifySettings.Initialize();
        var contentWithTimestamp = "Event at 2025-12-30T10:30:45.123Z was processed";

        // Act
        var result = contentWithTimestamp; // Content to be scrubbed by Verify

        // Assert - the scrubber should replace the timestamp with [TIMESTAMP]
        await Verifier.Verify(result);
    }
}
