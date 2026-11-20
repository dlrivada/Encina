using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Security.Sanitization;

/// <summary>
/// Guard tests for <see cref="OutputEncodingPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class OutputEncodingPipelineBehaviorGuardTests
{
    private sealed record Request : IRequest<string>;

    private static readonly IOptions<SanitizationOptions> Options = Microsoft.Extensions.Options.Options.Create(new SanitizationOptions());

    [Fact]
    public void Constructor_NullEncoder_ThrowsArgumentNullException()
    {
        var act = () => new OutputEncodingPipelineBehavior<Request, string>(
            null!, Options, NullLogger<OutputEncodingPipelineBehavior<Request, string>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("encoder");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new OutputEncodingPipelineBehavior<Request, string>(
            Substitute.For<IOutputEncoder>(), null!, NullLogger<OutputEncodingPipelineBehavior<Request, string>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new OutputEncodingPipelineBehavior<Request, string>(
            Substitute.For<IOutputEncoder>(), Options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        Should.NotThrow(() => new OutputEncodingPipelineBehavior<Request, string>(
            Substitute.For<IOutputEncoder>(), Options, NullLogger<OutputEncodingPipelineBehavior<Request, string>>.Instance));
    }
}
