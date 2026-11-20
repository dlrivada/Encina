using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Security.Sanitization;

/// <summary>
/// Guard tests for <see cref="InputSanitizationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class InputSanitizationPipelineBehaviorGuardTests
{
    private sealed record Request(string Text) : IRequest<string>;

    private static readonly IOptions<SanitizationOptions> Options = Microsoft.Extensions.Options.Options.Create(new SanitizationOptions());

    private static SanitizationOrchestrator CreateOrchestrator() =>
        new(Substitute.For<ISanitizer>(), Options, NullLogger<SanitizationOrchestrator>.Instance);

    private static InputSanitizationPipelineBehavior<Request, string> CreateSut() =>
        new(CreateOrchestrator(), Options, NullLogger<InputSanitizationPipelineBehavior<Request, string>>.Instance);

    private static ValueTask<Either<EncinaError, string>> Next() =>
        new(Prelude.Right<EncinaError, string>("ok"));

    [Fact]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new InputSanitizationPipelineBehavior<Request, string>(
            null!, Options, NullLogger<InputSanitizationPipelineBehavior<Request, string>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("orchestrator");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new InputSanitizationPipelineBehavior<Request, string>(
            CreateOrchestrator(), null!, NullLogger<InputSanitizationPipelineBehavior<Request, string>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InputSanitizationPipelineBehavior<Request, string>(CreateOrchestrator(), Options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.Handle(null!, Substitute.For<IRequestContext>(), Next, CancellationToken.None));

        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.Handle(new Request("x"), null!, Next, CancellationToken.None));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Handle_ValidArguments_ReturnsNextResult()
    {
        var sut = CreateSut();

        var result = await sut.Handle(new Request("x"), Substitute.For<IRequestContext>(), Next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }
}
