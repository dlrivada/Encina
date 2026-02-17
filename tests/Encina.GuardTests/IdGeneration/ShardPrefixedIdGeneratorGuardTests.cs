using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;

namespace Encina.GuardTests.IdGeneration;

/// <summary>
/// Guard tests for <see cref="ShardPrefixedIdGenerator"/> to verify null parameter handling.
/// </summary>
public sealed class ShardPrefixedIdGeneratorGuardTests
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        ShardPrefixedOptions options = null!;

        // Act & Assert
        var act = () => new ShardPrefixedIdGenerator(options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }
}
