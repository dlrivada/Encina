using Encina.Diagnostics;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="RedactedException"/> and <see cref="ExceptionLoggingExtensions"/>.
/// </summary>
public class RedactedExceptionGuardTests
{
    [Fact]
    public void ForLogging_NullException_ThrowsArgumentNullException()
    {
        Exception exception = null!;

        var act = () => exception.ForLogging();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("exception");
    }

    [Fact]
    public void From_NullException_ThrowsArgumentNullException()
    {
        var act = () => RedactedException.From(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("exception");
    }
}
