using System.Data;
using Encina.Dapper.Sqlite;

namespace Encina.Dapper.Sqlite.GuardTests;

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
        var act = () => new TransactionPipelineBehavior<TransactionPipelineTestRequest, TransactionPipelineTestResponse>(connection);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }
}

/// <summary>
/// Test request for TransactionPipelineBehavior guard tests.
/// </summary>
public sealed record TransactionPipelineTestRequest : IRequest<TransactionPipelineTestResponse>;

/// <summary>
/// Test response for TransactionPipelineBehavior guard tests.
/// </summary>
public sealed record TransactionPipelineTestResponse;
