using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;

namespace Encina.GuardTests.IdGeneration;

/// <summary>
/// Guard tests for <see cref="SnowflakeIdGenerator"/> to verify null parameter handling.
/// </summary>
public sealed class SnowflakeIdGeneratorGuardTests
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        SnowflakeOptions options = null!;

        // Act & Assert
        var act = () => new SnowflakeIdGenerator(options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }
}
