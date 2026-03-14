#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Pipeline;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="TransferBlockingPipelineBehavior{TRequest, TResponse}"/>
/// to verify null parameter handling.
/// </summary>
public class TransferBlockingPipelineBehaviorGuardTests
{
    private readonly ITransferValidator _validator = Substitute.For<ITransferValidator>();
    private readonly IOptions<CrossBorderTransferOptions> _options = Options.Create(new CrossBorderTransferOptions());
    private readonly ILogger<TransferBlockingPipelineBehavior<TestRequest, TestResponse>> _logger =
        NullLogger<TransferBlockingPipelineBehavior<TestRequest, TestResponse>>.Instance;
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when validator is null.
    /// </summary>
    [Fact]
    public void Constructor_NullValidator_ThrowsArgumentNullException()
    {
        var act = () => new TransferBlockingPipelineBehavior<TestRequest, TestResponse>(
            null!, _options, _logger, _serviceProvider);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("validator");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new TransferBlockingPipelineBehavior<TestRequest, TestResponse>(
            _validator, null!, _logger, _serviceProvider);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new TransferBlockingPipelineBehavior<TestRequest, TestResponse>(
            _validator, _options, null!, _serviceProvider);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when serviceProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new TransferBlockingPipelineBehavior<TestRequest, TestResponse>(
            _validator, _options, _logger, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    #endregion

    #region Test Types

    /// <summary>
    /// Test request type for pipeline behavior guard tests.
    /// </summary>
    public sealed record TestRequest : IRequest<TestResponse>;

    /// <summary>
    /// Test response type for pipeline behavior guard tests.
    /// </summary>
    public sealed record TestResponse;

    #endregion
}
