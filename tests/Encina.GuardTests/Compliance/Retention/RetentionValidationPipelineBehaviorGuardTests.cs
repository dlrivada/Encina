using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionValidationPipelineBehavior{TRequest, TResponse}"/>
/// constructor null parameter handling.
/// </summary>
public sealed class RetentionValidationPipelineBehaviorGuardTests
{
    private readonly IRetentionRecordService _recordService = Substitute.For<IRetentionRecordService>();
    private readonly IRetentionPolicyService _policyService = Substitute.For<IRetentionPolicyService>();
    private readonly IOptions<RetentionOptions> _options = Options.Create(new RetentionOptions());

    private readonly ILogger<RetentionValidationPipelineBehavior<TestRequest, TestResponse>> _logger =
        NullLogger<RetentionValidationPipelineBehavior<TestRequest, TestResponse>>.Instance;

    [Fact]
    public void Constructor_NullRecordService_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestRequest, TestResponse>(
            null!, _policyService, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("recordService");
    }

    [Fact]
    public void Constructor_NullPolicyService_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestRequest, TestResponse>(
            _recordService, null!, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("policyService");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestRequest, TestResponse>(
            _recordService, _policyService, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RetentionValidationPipelineBehavior<TestRequest, TestResponse>(
            _recordService, _policyService, _options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    // Test types for guard tests
    public sealed record TestRequest : IRequest<TestResponse>;
    public sealed record TestResponse;
}
