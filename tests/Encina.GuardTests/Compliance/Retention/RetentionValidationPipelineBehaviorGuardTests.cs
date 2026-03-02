using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionValidationPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class RetentionValidationPipelineBehaviorGuardTests
{
    private readonly IRetentionRecordStore _recordStore = Substitute.For<IRetentionRecordStore>();
    private readonly IOptions<RetentionOptions> _options;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<RetentionValidationPipelineBehavior<TestRequest, TestResponse>> _logger =
        NullLogger<RetentionValidationPipelineBehavior<TestRequest, TestResponse>>.Instance;

    public RetentionValidationPipelineBehaviorGuardTests()
    {
        _options = Substitute.For<IOptions<RetentionOptions>>();
        _options.Value.Returns(new RetentionOptions());
    }

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when recordStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRecordStore_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestRequest, TestResponse>(
            null!, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("recordStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestRequest, TestResponse>(
            _recordStore, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestRequest, TestResponse>(
            _recordStore, _options, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestRequest, TestResponse>(
            _recordStore, _options, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Guards

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

    #endregion

    private RetentionValidationPipelineBehavior<TestRequest, TestResponse> CreateBehavior() =>
        new(_recordStore, _options, _timeProvider, _logger);

    private interface ITestRequest : IRequest<TestResponse> { }

    private sealed record TestRequest : IRequest<TestResponse>
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed record TestResponse
    {
        public string Id { get; init; } = "test-id";
    }
}
