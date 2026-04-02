using Encina.ADO.SqlServer.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.SqlServer.ReadWriteSeparation;

/// <summary>
/// Guard tests for <see cref="ReadWriteConnectionFactory"/> to verify null parameter handling.
/// </summary>
public class ReadWriteConnectionFactoryGuardTests
{
    [Fact]
    public void Constructor_NullConnectionSelector_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReadWriteConnectionFactory(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectionSelector");
    }

    [Fact]
    public void Constructor_ValidSelector_DoesNotThrow()
    {
        // Arrange
        var selector = Substitute.For<IReadWriteConnectionSelector>();

        // Act & Assert
        Should.NotThrow(() => new ReadWriteConnectionFactory(selector));
    }
}
