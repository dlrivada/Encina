using Encina.Marten.Versioning;
using Shouldly;

namespace Encina.Marten.Tests.Versioning;

public sealed class EventVersioningErrorCodesTests
{
    [Fact]
    public void UpcastFailed_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.UpcastFailed.ShouldBe("event.versioning.upcast_failed");
    }

    [Fact]
    public void UpcasterNotFound_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.UpcasterNotFound.ShouldBe("event.versioning.upcaster_not_found");
    }

    [Fact]
    public void RegistrationFailed_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.RegistrationFailed.ShouldBe("event.versioning.registration_failed");
    }

    [Fact]
    public void DuplicateUpcaster_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.DuplicateUpcaster.ShouldBe("event.versioning.duplicate_upcaster");
    }

    [Fact]
    public void InvalidConfiguration_HasCorrectValue()
    {
        // Act & Assert
        EventVersioningErrorCodes.InvalidConfiguration.ShouldBe("event.versioning.invalid_configuration");
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
            code.ShouldStartWith("event.versioning.");
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
        distinctCount.ShouldBe(allCodes.Length);
    }
}
