using Encina.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.Tests;

public sealed class FunctionalFailureDetectorTests
{
    [Fact]
    public void NullFunctionalFailureDetector_DoesNotReportFailures()
    {
        using var provider = BuildProvider();
        var detector = provider.GetRequiredService<IFunctionalFailureDetector>();

        detector.TryExtractFailure(new object(), out var reason, out var error).ShouldBeFalse();
        reason.ShouldBe(string.Empty);
        error.ShouldBeNull();
        detector.TryGetErrorCode(new object()).ShouldBeNull();
        detector.TryGetErrorMessage(new object()).ShouldBeNull();
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddEncina(typeof(PingCommand).Assembly);
        return services.BuildServiceProvider();
    }
}
