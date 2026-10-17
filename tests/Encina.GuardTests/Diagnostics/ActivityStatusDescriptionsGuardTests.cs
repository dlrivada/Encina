using Encina.Diagnostics;
using Shouldly;

namespace Encina.GuardTests.Diagnostics;

/// <summary>
/// Guard tests for <see cref="ActivityStatusDescriptions"/>.
/// </summary>
public sealed class ActivityStatusDescriptionsGuardTests
{
    [Fact]
    public void ForException_NullException_ThrowsArgumentNullException()
    {
        var act = () => ActivityStatusDescriptions.ForException(null!);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("exception");
    }

    [Fact]
    public void ForException_ValidException_ReturnsTypeNameOnly()
    {
        ActivityStatusDescriptions.ForException(new InvalidOperationException("secret"))
            .ShouldBe(nameof(InvalidOperationException));
    }
}
