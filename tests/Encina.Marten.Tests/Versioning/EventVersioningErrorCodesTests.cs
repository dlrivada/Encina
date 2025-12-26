using Encina.Marten.Versioning;

namespace Encina.Marten.Tests.Versioning;

public sealed class EventVersioningErrorCodesTests
{
    [Fact]
    public void UpcastFailed_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.UpcastFailed.Should().Be("event.versioning.upcast_failed");
    }

    [Fact]
    public void UpcasterNotFound_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.UpcasterNotFound.Should().Be("event.versioning.upcaster_not_found");
    }

    [Fact]
    public void RegistrationFailed_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.RegistrationFailed.Should().Be("event.versioning.registration_failed");
    }

    [Fact]
    public void DuplicateUpcaster_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.DuplicateUpcaster.Should().Be("event.versioning.duplicate_upcaster");
    }

    [Fact]
    public void InvalidConfiguration_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.InvalidConfiguration.Should().Be("event.versioning.invalid_configuration");
    }

    [Fact]
    public void AllErrorCodes_HaveCommonPrefix()
    {
        // Arrange
        var allCodes = new[]
        {
            EventVersioningErrorCodes.UpcastFailed,
            EventVersioningErrorCodes.UpcasterNotFound,
            EventVersioningErrorCodes.RegistrationFailed,
            EventVersioningErrorCodes.DuplicateUpcaster,
            EventVersioningErrorCodes.InvalidConfiguration
        };

        // Act & Assert
        foreach (var code in allCodes)
        {
            code.Should().StartWith("event.versioning.");
        }
    }

    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        // Arrange
        var allCodes = new[]
        {
            EventVersioningErrorCodes.UpcastFailed,
            EventVersioningErrorCodes.UpcasterNotFound,
            EventVersioningErrorCodes.RegistrationFailed,
            EventVersioningErrorCodes.DuplicateUpcaster,
            EventVersioningErrorCodes.InvalidConfiguration
        };

        // Act
        var distinctCount = allCodes.Distinct().Count();

        // Assert
        distinctCount.Should().Be(allCodes.Length);
    }
}
