using Encina.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Encina.GuardTests.AspNetCore;

/// <summary>
/// Guard tests for the null checks of <see cref="ProblemDetailsExtensions"/> and the startup warning service.
/// </summary>
public sealed class ProblemDetailsGuardTests
{
    [Fact]
    public void ToProblemDetails_NullHttpContext_Throws()
    {
        var error = EncinaErrors.Create("test.error", "message");

        Should.Throw<ArgumentNullException>(() => error.ToProblemDetails(null!))
            .ParamName.ShouldBe("httpContext");
    }

    [Fact]
    public void ToActionResult_NullHttpContext_Throws()
    {
        var error = EncinaErrors.Create("test.error", "message");

        Should.Throw<ArgumentNullException>(() => error.ToActionResult(null!))
            .ParamName.ShouldBe("httpContext");
    }

    [Fact]
    public void ToProblemDetails_ValidContext_DoesNotThrow()
    {
        var error = EncinaErrors.Create("test.error", "message");

        var context = new DefaultHttpContext
        {
            RequestServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider()
        };

        Should.NotThrow(() => error.ToProblemDetails(context));
    }

    [Fact]
    public void StartupWarningService_NullArguments_Throw()
    {
        var options = Options.Create(new EncinaAspNetCoreOptions());

        Should.Throw<ArgumentNullException>(() => new ProblemDetailsStartupWarningService(null!, null!, null!));
        Should.Throw<ArgumentNullException>(() => new ProblemDetailsStartupWarningService(options, null!, null!));
    }
}
