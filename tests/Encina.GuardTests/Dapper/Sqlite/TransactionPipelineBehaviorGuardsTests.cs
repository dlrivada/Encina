using System.Data;
using global::Encina.Dapper.Sqlite;
using global::Encina.Messaging;

namespace Encina.GuardTests.Dapper.Sqlite;

/// <summary>
/// Guard tests for <see cref="global::Encina.Messaging.TransactionPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// Dapper providers use the Messaging TransactionPipelineBehavior which works with IDbConnection.
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
        var act = () => new global::Encina.Messaging.TransactionPipelineBehavior<TransactionPipelineTestRequest, TransactionPipelineTestResponse>(connection);
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
