using Encina.Validation;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute .Returns() pattern with ValueTask is safe

namespace Encina.GuardTests.Core.Validation;

/// <summary>
/// Guard tests for <see cref="ValidationPipelineBehavior{TRequest, TResponse}"/>
/// and <see cref="ValidationOrchestrator"/> to verify constructor and method guards.
/// </summary>
public class ValidationPipelineBehaviorGuardTests
{
    // ---- ValidationPipelineBehavior constructor ----

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when orchestrator is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new ValidationPipelineBehavior<TestRequest, TestResponse>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("orchestrator");
    }

    // ---- ValidationPipelineBehavior.Handle ----

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();

        var act = async () => await behavior.Handle(
            null!,
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse())),
            CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();

        var act = async () => await behavior.Handle(
            new TestRequest(),
            null!,
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse())),
            CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();

        var act = async () => await behavior.Handle(
            new TestRequest(),
            Substitute.For<IRequestContext>(),
            null!,
            CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("nextStep");
    }

    /// <summary>
    /// Verifies that Handle calls nextStep when validation passes.
    /// </summary>
    [Fact]
    public async Task Handle_ValidationPasses_CallsNextStep()
    {
        var provider = Substitute.For<IValidationProvider>();
        provider.ValidateAsync(Arg.Any<TestRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(global::Encina.Validation.ValidationResult.Success));

        var orchestrator = new ValidationOrchestrator(provider);
        var behavior = new ValidationPipelineBehavior<TestRequest, TestResponse>(orchestrator);
        var nextCalled = false;

        var result = await behavior.Handle(
            new TestRequest(),
            Substitute.For<IRequestContext>(),
            () =>
            {
                nextCalled = true;
                return ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse()));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that Handle returns Left when validation fails.
    /// </summary>
    [Fact]
    public async Task Handle_ValidationFails_ReturnsLeft()
    {
        var provider = Substitute.For<IValidationProvider>();
        var failedResult = global::Encina.Validation.ValidationResult.Failure(
            [new ValidationError("Name", "Name is required")]);
        provider.ValidateAsync(Arg.Any<TestRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(failedResult));

        var orchestrator = new ValidationOrchestrator(provider);
        var behavior = new ValidationPipelineBehavior<TestRequest, TestResponse>(orchestrator);

        var result = await behavior.Handle(
            new TestRequest(),
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse())),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    // ---- ValidationOrchestrator constructor ----

    /// <summary>
    /// Verifies that the orchestrator constructor throws ArgumentNullException when provider is null.
    /// </summary>
    [Fact]
    public void Orchestrator_NullProvider_ThrowsArgumentNullException()
    {
        var act = () => new ValidationOrchestrator(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("validationProvider");
    }

    // ---- ValidationOrchestrator.ValidateAsync ----

    /// <summary>
    /// Verifies that ValidateAsync throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Orchestrator_ValidateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var orchestrator = new ValidationOrchestrator(Substitute.For<IValidationProvider>());

        var act = async () => await orchestrator.ValidateAsync<TestRequest, TestResponse>(
            null!,
            Substitute.For<IRequestContext>(),
            CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that ValidateAsync throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Orchestrator_ValidateAsync_NullContext_ThrowsArgumentNullException()
    {
        var orchestrator = new ValidationOrchestrator(Substitute.For<IValidationProvider>());

        var act = async () => await orchestrator.ValidateAsync<TestRequest, TestResponse>(
            new TestRequest(),
            null!,
            CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that ValidateAsync returns Left when the cancellation token is already cancelled.
    /// </summary>
    [Fact]
    public async Task Orchestrator_ValidateAsync_CancelledToken_ReturnsLeft()
    {
        var orchestrator = new ValidationOrchestrator(Substitute.For<IValidationProvider>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await orchestrator.ValidateAsync<TestRequest, TestResponse>(
            new TestRequest(),
            Substitute.For<IRequestContext>(),
            cts.Token);

        result.IsLeft.ShouldBeTrue();
    }

    private static ValidationPipelineBehavior<TestRequest, TestResponse> CreateBehavior()
    {
        var provider = Substitute.For<IValidationProvider>();
        provider.ValidateAsync(Arg.Any<TestRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(global::Encina.Validation.ValidationResult.Success));
        return new ValidationPipelineBehavior<TestRequest, TestResponse>(
            new ValidationOrchestrator(provider));
    }

    private sealed record TestRequest : IRequest<TestResponse>;

    private sealed record TestResponse;
}
