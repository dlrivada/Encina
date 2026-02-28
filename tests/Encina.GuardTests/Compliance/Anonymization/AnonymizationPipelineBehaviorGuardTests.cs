using Encina.Compliance.Anonymization;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="AnonymizationPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class AnonymizationPipelineBehaviorGuardTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when techniques is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTechniques_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationPipelineBehavior<TestRequest, TestResponse>(
            null!,
            Substitute.For<IPseudonymizer>(),
            Substitute.For<ITokenizer>(),
            Substitute.For<IKeyProvider>(),
            Options.Create(new AnonymizationOptions()),
            NullLogger<AnonymizationPipelineBehavior<TestRequest, TestResponse>>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("techniques");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when pseudonymizer is null.
    /// </summary>
    [Fact]
    public void Constructor_NullPseudonymizer_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationPipelineBehavior<TestRequest, TestResponse>(
            Enumerable.Empty<IAnonymizationTechnique>(),
            null!,
            Substitute.For<ITokenizer>(),
            Substitute.For<IKeyProvider>(),
            Options.Create(new AnonymizationOptions()),
            NullLogger<AnonymizationPipelineBehavior<TestRequest, TestResponse>>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("pseudonymizer");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when tokenizer is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTokenizer_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationPipelineBehavior<TestRequest, TestResponse>(
            Enumerable.Empty<IAnonymizationTechnique>(),
            Substitute.For<IPseudonymizer>(),
            null!,
            Substitute.For<IKeyProvider>(),
            Options.Create(new AnonymizationOptions()),
            NullLogger<AnonymizationPipelineBehavior<TestRequest, TestResponse>>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("tokenizer");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when keyProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationPipelineBehavior<TestRequest, TestResponse>(
            Enumerable.Empty<IAnonymizationTechnique>(),
            Substitute.For<IPseudonymizer>(),
            Substitute.For<ITokenizer>(),
            null!,
            Options.Create(new AnonymizationOptions()),
            NullLogger<AnonymizationPipelineBehavior<TestRequest, TestResponse>>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationPipelineBehavior<TestRequest, TestResponse>(
            Enumerable.Empty<IAnonymizationTechnique>(),
            Substitute.For<IPseudonymizer>(),
            Substitute.For<ITokenizer>(),
            Substitute.For<IKeyProvider>(),
            null!,
            NullLogger<AnonymizationPipelineBehavior<TestRequest, TestResponse>>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationPipelineBehavior<TestRequest, TestResponse>(
            Enumerable.Empty<IAnonymizationTechnique>(),
            Substitute.For<IPseudonymizer>(),
            Substitute.For<ITokenizer>(),
            Substitute.For<IKeyProvider>(),
            Options.Create(new AnonymizationOptions()),
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

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
            () => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, TestResponse>(new TestResponse())),
            CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    private static AnonymizationPipelineBehavior<TestRequest, TestResponse> CreateBehavior()
    {
        return new AnonymizationPipelineBehavior<TestRequest, TestResponse>(
            Enumerable.Empty<IAnonymizationTechnique>(),
            Substitute.For<IPseudonymizer>(),
            Substitute.For<ITokenizer>(),
            Substitute.For<IKeyProvider>(),
            Options.Create(new AnonymizationOptions()),
            NullLogger<AnonymizationPipelineBehavior<TestRequest, TestResponse>>.Instance);
    }

    private sealed record TestRequest : IRequest<TestResponse>
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed record TestResponse
    {
        public string Name { get; init; } = string.Empty;
    }
}
