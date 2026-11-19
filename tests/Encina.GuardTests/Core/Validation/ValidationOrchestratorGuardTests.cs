using Encina.Validation;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Core.Validation;

/// <summary>
/// Guard tests for <see cref="ValidationOrchestrator"/>.
/// </summary>
public sealed class ValidationOrchestratorGuardTests
{
    private sealed record TestRequest(string Name);

    [Fact]
    public void Constructor_NullValidationProvider_ThrowsArgumentNullException()
    {
        var act = () => new ValidationOrchestrator(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("validationProvider");
    }

    [Fact]
    public async Task ValidateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var orchestrator = new ValidationOrchestrator(Substitute.For<IValidationProvider>());

        var act = () => orchestrator.ValidateAsync<TestRequest, string>(null!, RequestContext.Create(), CancellationToken.None).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ValidateAsync_NullContext_ThrowsArgumentNullException()
    {
        var orchestrator = new ValidationOrchestrator(Substitute.For<IValidationProvider>());

        var act = () => orchestrator.ValidateAsync<TestRequest, string>(new TestRequest("valid"), null!, CancellationToken.None).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }
}
