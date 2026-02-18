using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="GDPRErrors"/> and <see cref="RoPAExportErrors"/>.
/// </summary>
public class GDPRErrorsTests
{
    [Fact]
    public void UnregisteredActivity_ShouldContainRequestTypeName()
    {
        // Act
        var error = GDPRErrors.UnregisteredActivity(typeof(string));

        // Assert
        error.Message.Should().Contain("String");
        error.Message.Should().Contain("Article 30");
    }

    [Fact]
    public void ComplianceValidationFailed_ShouldContainErrors()
    {
        // Act
        var error = GDPRErrors.ComplianceValidationFailed(
            typeof(string), ["Missing consent", "No lawful basis"]);

        // Assert
        error.Message.Should().Contain("String");
        error.Message.Should().Contain("Missing consent");
        error.Message.Should().Contain("No lawful basis");
    }

    [Fact]
    public void RegistryLookupFailed_ShouldContainInnerError()
    {
        // Arrange
        var inner = EncinaError.New("Database connection failed");

        // Act
        var error = GDPRErrors.RegistryLookupFailed(typeof(string), inner);

        // Assert
        error.Message.Should().Contain("String");
        error.Message.Should().Contain("Database connection failed");
    }

    [Fact]
    public void RoPAExportErrors_SerializationFailed_ShouldContainFormatAndReason()
    {
        // Act
        var error = RoPAExportErrors.SerializationFailed("JSON", "Invalid character");

        // Assert
        error.Message.Should().Contain("JSON");
        error.Message.Should().Contain("Invalid character");
    }

    // -- Error codes --

    [Fact]
    public void ErrorCodes_ShouldFollowConvention()
    {
        GDPRErrors.UnregisteredActivityCode.Should().StartWith("gdpr.");
        GDPRErrors.ComplianceValidationFailedCode.Should().StartWith("gdpr.");
        GDPRErrors.RegistryLookupFailedCode.Should().StartWith("gdpr.");
        RoPAExportErrors.SerializationFailedCode.Should().StartWith("gdpr.");
    }
}
