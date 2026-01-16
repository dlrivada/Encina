using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Infrastructure.Polly;

/// <summary>
/// Guard clause tests for <see cref="BulkheadPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies null argument validation.
/// </summary>
public class BulkheadPipelineBehaviorGuardsTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<BulkheadPipelineBehavior<TestBulkheadRequest, string>> logger = null!;
        var bulkheadManager = Substitute.For<IBulkheadManager>();

        // Act & Assert
        var act = () => new BulkheadPipelineBehavior<TestBulkheadRequest, string>(logger, bulkheadManager);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullBulkheadManager_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<BulkheadPipelineBehavior<TestBulkheadRequest, string>>.Instance;
        IBulkheadManager bulkheadManager = null!;

        // Act & Assert
        var act = () => new BulkheadPipelineBehavior<TestBulkheadRequest, string>(logger, bulkheadManager);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("bulkheadManager");
    }
}

[Bulkhead]
public sealed record TestBulkheadRequest : IRequest<string>;
