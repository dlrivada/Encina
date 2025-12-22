using System.Data;
using Microsoft.Extensions.Logging;
using Encina.Dapper.PostgreSQL.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.PostgreSQL.GuardTests;

/// <summary>
/// Guard tests for <see cref="InboxPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class InboxPipelineBehaviorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, TestResponse>>>();

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, TestResponse>(connection, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        InboxOptions options = null!;
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, TestResponse>>>();

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, TestResponse>(connection, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        ILogger<InboxPipelineBehavior<TestRequest, TestResponse>> logger = null!;

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, TestResponse>(connection, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, TestResponse>>>();
        string tableName = null!;

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, TestResponse>(connection, options, logger, tableName);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, TestResponse>>>();
        var tableName = string.Empty;

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, TestResponse>(connection, options, logger, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when tableName is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, TestResponse>>>();
        var tableName = "   ";

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, TestResponse>(connection, options, logger, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Test request type for guard clause testing.
    /// </summary>
    public sealed record TestRequest : IRequest<TestResponse>;

    /// <summary>
    /// Test response type for guard clause testing.
    /// </summary>
    public sealed record TestResponse;
}
