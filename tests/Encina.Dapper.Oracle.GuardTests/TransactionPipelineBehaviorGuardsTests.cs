using System.Data;
using Encina.Dapper.Oracle;
using Shouldly;

namespace Encina.Dapper.Oracle.GuardTests;

/// <summary>
/// Guard tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class TransactionPipelineBehaviorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;

        // Act & Assert
        var act = () => new TransactionPipelineBehavior<TestRequest, TestResponse>(connection);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    // Test request/response types
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse;
}
