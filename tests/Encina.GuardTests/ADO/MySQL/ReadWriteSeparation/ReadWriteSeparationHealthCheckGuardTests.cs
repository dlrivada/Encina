using Encina.ADO.MySQL.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.GuardTests.ADO.MySQL.ReadWriteSeparation;

/// <summary>
/// Guard tests for <see cref="ReadWriteSeparationHealthCheck"/> to verify null parameter handling.
/// </summary>
public class ReadWriteSeparationHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReadWriteSeparationHealthCheck(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_ValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions();

        // Act & Assert
        Should.NotThrow(() => new ReadWriteSeparationHealthCheck(options));
    }

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Assert
        ReadWriteSeparationHealthCheck.DefaultName.ShouldBe("encina-read-write-separation-ado-mysql");
    }
}
