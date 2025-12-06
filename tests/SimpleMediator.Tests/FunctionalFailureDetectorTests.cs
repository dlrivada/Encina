using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SimpleMediator;
using SimpleMediator.Tests.Fixtures;

namespace SimpleMediator.Tests;

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
        services.AddSimpleMediator(typeof(PingCommand).Assembly);
        return services.BuildServiceProvider();
    }
}
