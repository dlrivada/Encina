using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Security.PII;

/// <summary>
/// Guard clause tests for <see cref="PIIMaskingPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies null argument validation on constructor and Handle method.
/// </summary>
/// <remarks>
/// Uses <see cref="NullLogger{T}"/> instead of NSubstitute for the logger parameter
/// because Castle DynamicProxy cannot create proxies for <c>ILogger&lt;T&gt;</c> when
/// <c>T</c> is a private nested type within a strong-named assembly.
/// </remarks>
public sealed class PIIMaskingPipelineBehaviorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullMasker_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new PIIOptions());
        var logger = NullLogger<PIIMaskingPipelineBehavior<TestRequest, TestResponse>>.Instance;

        // Act & Assert
        var act = () => new PIIMaskingPipelineBehavior<TestRequest, TestResponse>(null!, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("masker");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var masker = Substitute.For<IPIIMasker>();
        var logger = NullLogger<PIIMaskingPipelineBehavior<TestRequest, TestResponse>>.Instance;

        // Act & Assert
        var act = () => new PIIMaskingPipelineBehavior<TestRequest, TestResponse>(masker, null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var masker = Substitute.For<IPIIMasker>();
        var options = Options.Create(new PIIOptions());

        // Act & Assert
        var act = () => new PIIMaskingPipelineBehavior<TestRequest, TestResponse>(masker, options, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Guards

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();
        var context = RequestContext.CreateForTest();
        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult<LanguageExt.Either<EncinaError, TestResponse>>(new TestResponse("ok"));

        // Act & Assert
        var act = async () => await sut.Handle(null!, context, nextStep, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();
        var request = new TestRequest();
        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult<LanguageExt.Either<EncinaError, TestResponse>>(new TestResponse("ok"));

        // Act & Assert
        var act = async () => await sut.Handle(request, null!, nextStep, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    #endregion

    #region Helpers

    private static PIIMaskingPipelineBehavior<TestRequest, TestResponse> CreateSut()
    {
        var masker = Substitute.For<IPIIMasker>();
        var options = Options.Create(new PIIOptions());
        var logger = NullLogger<PIIMaskingPipelineBehavior<TestRequest, TestResponse>>.Instance;
        return new PIIMaskingPipelineBehavior<TestRequest, TestResponse>(masker, options, logger);
    }

    #endregion

    #region Test Types

    private sealed record TestRequest : IRequest<TestResponse>;

    private sealed record TestResponse(string Value);

    #endregion
}
