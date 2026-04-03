using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract tests for <see cref="RetentionValidationPipelineBehavior{TRequest, TResponse}"/>
/// verifying behavioral contracts across different enforcement modes and response types.
/// </summary>
[Trait("Category", "Contract")]
public class RetentionValidationPipelineBehaviorContractTests
{
    private readonly IRetentionRecordService _recordService = Substitute.For<IRetentionRecordService>();
    private readonly IRetentionPolicyService _policyService = Substitute.For<IRetentionPolicyService>();

    #region Constructor Guards

    /// <summary>
    /// Contract: Constructor must reject null recordService.
    /// </summary>
    [Fact]
    public void Constructor_NullRecordService_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestCommand, TestResponse>(
            null!,
            _policyService,
            Options.Create(new RetentionOptions()),
            NullLogger<RetentionValidationPipelineBehavior<TestCommand, TestResponse>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("recordService");
    }

    /// <summary>
    /// Contract: Constructor must reject null policyService.
    /// </summary>
    [Fact]
    public void Constructor_NullPolicyService_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestCommand, TestResponse>(
            _recordService,
            null!,
            Options.Create(new RetentionOptions()),
            NullLogger<RetentionValidationPipelineBehavior<TestCommand, TestResponse>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("policyService");
    }

    /// <summary>
    /// Contract: Constructor must reject null options.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestCommand, TestResponse>(
            _recordService,
            _policyService,
            null!,
            NullLogger<RetentionValidationPipelineBehavior<TestCommand, TestResponse>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Contract: Constructor must reject null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestCommand, TestResponse>(
            _recordService,
            _policyService,
            Options.Create(new RetentionOptions()),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Disabled Mode Contract

    /// <summary>
    /// Contract: When enforcement mode is Disabled, the pipeline must pass through
    /// to the next handler without any retention tracking, regardless of response type.
    /// </summary>
    [Fact]
    public async Task Handle_DisabledMode_PassesThroughWithoutTracking()
    {
        var options = new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Disabled };
        var sut = CreateBehavior<TestCommand, TestResponse>(options);
        var expectedResponse = new TestResponse { Id = "test-123" };

        var result = await sut.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, TestResponse>>(Right<EncinaError, TestResponse>(expectedResponse)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((TestResponse)result).Id.ShouldBe("test-123");

        // No retention tracking should have occurred
        await _recordService.DidNotReceive().TrackEntityAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region No-Attribute Response Contract

    /// <summary>
    /// Contract: When the response type has no [RetentionPeriod] attribute, the pipeline
    /// must pass through without any retention tracking, regardless of enforcement mode.
    /// </summary>
    [Theory]
    [InlineData(RetentionEnforcementMode.Block)]
    [InlineData(RetentionEnforcementMode.Warn)]
    public async Task Handle_NoRetentionAttribute_PassesThrough(RetentionEnforcementMode mode)
    {
        var options = new RetentionOptions { EnforcementMode = mode };
        var sut = CreateBehavior<PlainCommand, PlainResponse>(options);
        var expectedResponse = new PlainResponse { Value = "hello" };

        var result = await sut.Handle(
            new PlainCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, PlainResponse>>(Right<EncinaError, PlainResponse>(expectedResponse)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((PlainResponse)result).Value.ShouldBe("hello");
    }

    #endregion

    #region Handler Error Pass-Through Contract

    /// <summary>
    /// Contract: When the inner handler returns an error (Left), the pipeline must
    /// pass that error through unchanged, regardless of enforcement mode.
    /// </summary>
    [Theory]
    [InlineData(RetentionEnforcementMode.Block)]
    [InlineData(RetentionEnforcementMode.Warn)]
    public async Task Handle_InnerHandlerReturnsError_PassesThroughError(RetentionEnforcementMode mode)
    {
        var options = new RetentionOptions { EnforcementMode = mode };
        var sut = CreateBehavior<TestCommand, TestResponse>(options);
        var error = EncinaErrors.Create("test.error", "Test error message");

        var result = await sut.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, TestResponse>>(Left<EncinaError, TestResponse>(error)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).Message.ShouldContain("Test error message");
    }

    #endregion

    #region Handle Null Request Guard

    /// <summary>
    /// Contract: Handle must reject null request with ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var options = new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Warn };
        var sut = CreateBehavior<TestCommand, TestResponse>(options);

        var act = async () => await sut.Handle(
            null!,
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, TestResponse>>(Right<EncinaError, TestResponse>(new TestResponse())),
            CancellationToken.None);

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region Block Mode With Decorated Response

    /// <summary>
    /// Contract: In Block mode, when the response type has [RetentionPeriod] and TrackEntityAsync
    /// returns a successful result, Handle should return the response (Right).
    /// </summary>
    [Fact]
    public async Task Handle_BlockMode_SuccessfulTracking_ReturnsResponse()
    {
        var options = new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Block };
        var sut = CreateBehavior<DecoratedCommand, DecoratedResponse>(options);
        var response = new DecoratedResponse { Id = "entity-42" };

        _recordService.TrackEntityAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var result = await sut.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, DecoratedResponse>>(Right<EncinaError, DecoratedResponse>(response)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((DecoratedResponse)result).Id.ShouldBe("entity-42");
    }

    #endregion

    #region Warn Mode With Exception

    /// <summary>
    /// Contract: In Warn mode, when TrackEntityAsync throws an exception, the pipeline
    /// must allow the response through (Right) instead of blocking.
    /// </summary>
    [Fact]
    public async Task Handle_WarnMode_ExceptionDuringTracking_AllowsThrough()
    {
        var options = new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Warn };
        var sut = CreateBehavior<DecoratedCommand, DecoratedResponse>(options);
        var response = new DecoratedResponse { Id = "entity-99" };

#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _recordService.TrackEntityAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Guid>>>(
                _ => throw new InvalidOperationException("Simulated failure"));
#pragma warning restore CA2012

        var result = await sut.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, DecoratedResponse>>(Right<EncinaError, DecoratedResponse>(response)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Block Mode With Exception

    /// <summary>
    /// Contract: In Block mode, when TrackEntityAsync throws an exception, the pipeline
    /// must return a Left error, blocking the response.
    /// </summary>
    [Fact]
    public async Task Handle_BlockMode_ExceptionDuringTracking_ReturnsError()
    {
        var options = new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Block };
        var sut = CreateBehavior<DecoratedCommand, DecoratedResponse>(options);
        var response = new DecoratedResponse { Id = "entity-77" };

#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _recordService.TrackEntityAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Guid>>>(
                _ => throw new InvalidOperationException("Store failure"));
#pragma warning restore CA2012

        var result = await sut.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, DecoratedResponse>>(Right<EncinaError, DecoratedResponse>(response)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).Message.ShouldContain("pipeline");
    }

    #endregion

    #region Helper Methods and Test Types

    private RetentionValidationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        RetentionOptions options)
        where TRequest : IRequest<TResponse>
    {
        return new RetentionValidationPipelineBehavior<TRequest, TResponse>(
            _recordService,
            _policyService,
            Options.Create(options),
            NullLogger<RetentionValidationPipelineBehavior<TRequest, TResponse>>.Instance);
    }

    // --- Test types without [RetentionPeriod] attribute ---

    public sealed record PlainCommand : IRequest<PlainResponse>;

    public sealed record PlainResponse
    {
        public string Value { get; init; } = string.Empty;
    }

    // --- Test types with no retention attribute (to test pass-through with no attribute) ---

    public sealed record TestCommand : IRequest<TestResponse>;

    public sealed record TestResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    // --- Test types with [RetentionPeriod] attribute ---

    public sealed record DecoratedCommand : IRequest<DecoratedResponse>;

    [RetentionPeriod(Days = 365, DataCategory = "test-category", Reason = "Test")]
    public sealed record DecoratedResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    #endregion
}
