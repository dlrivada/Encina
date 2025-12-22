using System.Data;
using Microsoft.Extensions.Logging;
using Encina.Dapper.Sqlite.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.Sqlite.GuardTests;

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
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<InboxPipelineTestRequest, InboxPipelineTestResponse>>>();

        // Act & Assert
        var act = () => new InboxPipelineBehavior<InboxPipelineTestRequest, InboxPipelineTestResponse>(connection, options, logger);
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
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<InboxPipelineTestRequest, InboxPipelineTestResponse>>>();

        // Act & Assert
        var act = () => new InboxPipelineBehavior<InboxPipelineTestRequest, InboxPipelineTestResponse>(connection, options, logger);
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
        ILogger<InboxPipelineBehavior<InboxPipelineTestRequest, InboxPipelineTestResponse>> logger = null!;

        // Act & Assert
        var act = () => new InboxPipelineBehavior<InboxPipelineTestRequest, InboxPipelineTestResponse>(connection, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}

/// <summary>
/// Test request for InboxPipelineBehavior guard tests.
/// </summary>
public sealed record InboxPipelineTestRequest : IRequest<InboxPipelineTestResponse>;

/// <summary>
/// Test response for InboxPipelineBehavior guard tests.
/// </summary>
public sealed record InboxPipelineTestResponse;
