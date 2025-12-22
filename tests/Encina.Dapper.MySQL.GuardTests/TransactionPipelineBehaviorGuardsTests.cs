using System.Data;
using Encina.Dapper.MySQL;

namespace Encina.Dapper.MySQL.GuardTests;

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
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    public sealed record TestRequest : IRequest<TestResponse>;
    public sealed record TestResponse;
}
