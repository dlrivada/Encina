using Encina.Sharding.Colocation;
using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ColocatedWithAttribute"/>.
/// </summary>
public sealed class ColocatedWithAttributeGuardTests
{
    [Fact]
    public void Constructor_NullRootEntityType_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new ColocatedWithAttribute(null!));
        ex.ParamName.ShouldBe("rootEntityType");
    }
}
