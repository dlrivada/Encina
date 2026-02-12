using Encina.Sharding.Colocation;
using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ColocationViolationException"/>.
/// </summary>
public sealed class ColocationViolationExceptionGuardTests
{
    [Fact]
    public void Constructor_NullReason_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => new ColocationViolationException(typeof(string), typeof(int), null!));
        ex.ParamName.ShouldBe("reason");
    }
}
