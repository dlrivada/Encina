using System.Data;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Encina.Dapper.SqlServer.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.SqlServer.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="InboxPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class InboxPipelineBehaviorGuardTests
{
    private sealed record TestRequest(string Data) : IRequest<string>, IIdempotentRequest;

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();

        // Act
        var act = () => new InboxPipelineBehavior<TestRequest, string>(
            connection,
            options,
            logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        InboxOptions options = null!;
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();

        // Act
        var act = () => new InboxPipelineBehavior<TestRequest, string>(
            connection,
            options,
            logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        ILogger<InboxPipelineBehavior<TestRequest, string>> logger = null!;

        // Act
        var act = () => new InboxPipelineBehavior<TestRequest, string>(
            connection,
            options,
            logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        string tableName = null!;

        // Act
        var act = () => new InboxPipelineBehavior<TestRequest, string>(
            connection,
            options,
            logger,
            tableName);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        const string tableName = "";

        // Act
        var act = () => new InboxPipelineBehavior<TestRequest, string>(
            connection,
            options,
            logger,
            tableName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentException when tableName is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        const string tableName = "   ";

        // Act
        var act = () => new InboxPipelineBehavior<TestRequest, string>(
            connection,
            options,
            logger,
            tableName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(connection, options, logger);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult<Either<EncinaError, string>>("result");

        // Act
        var act = async () => await behavior.Handle(null!, context, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(connection, options, logger);
        var request = new TestRequest("test");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult<Either<EncinaError, string>>("result");

        // Act
        var act = async () => await behavior.Handle(request, null!, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when next is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(connection, options, logger);
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        // Act
        var act = async () => await behavior.Handle(request, context, null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("next");
    }
}
