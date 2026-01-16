using System.Data;
using Encina.Messaging;
using LanguageExt;

namespace Encina.UnitTests.Messaging.Behaviors;

/// <summary>
/// Unit tests for TransactionPipelineBehavior.
/// </summary>
// Primary constructor not used due to mock setup order.
public sealed class TransactionPipelineBehaviorTests
{
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();
    private readonly TransactionPipelineBehavior<TestRequest, string> _behavior;

    public TransactionPipelineBehaviorTests()
    {
        _connection.BeginTransaction().Returns(_transaction);
        _connection.State.Returns(ConnectionState.Open);
        _behavior = new TransactionPipelineBehavior<TestRequest, string>(_connection);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TransactionPipelineBehavior<TestRequest, string>(null!);

        // Assert
        var exception = act.ShouldThrow<ArgumentNullException>();
        exception.ParamName.ShouldBe("connection");
    }

    #endregion

    #region Handle Tests - Success Path

    [Fact]
    public async Task Handle_HandlerReturnsSuccess_CommitsTransaction()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        var expectedResult = "Success";

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>(expectedResult);

        // Act
        var result = await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: value => value.ShouldBe(expectedResult),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        _transaction.Received(1).Commit();
        _transaction.DidNotReceive().Rollback();
    }

    [Fact]
    public async Task Handle_ConnectionClosed_OpensConnection()
    {
        // Arrange
        var closedConnection = Substitute.For<IDbConnection>();
        closedConnection.State.Returns(ConnectionState.Closed);
        closedConnection.BeginTransaction().Returns(_transaction);

        var behavior = new TransactionPipelineBehavior<TestRequest, string>(closedConnection);
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        closedConnection.Received(1).Open();
    }

    [Fact]
    public async Task Handle_ConnectionAlreadyOpen_DoesNotOpenAgain()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        _connection.DidNotReceive().Open();
    }

    #endregion

    #region Handle Tests - Error Path

    [Fact]
    public async Task Handle_HandlerReturnsError_RollsBackTransaction()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        var error = EncinaErrors.Create("test.error", "Test error");

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>(error);

        // Act
        var result = await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.ShouldBe("Test error"));

        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
    }

    [Fact]
    public async Task Handle_HandlerThrows_RollsBackTransactionAndRethrows()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            throw new InvalidOperationException("Handler failed");

        // Act
        var act = async () => await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        var exception = await act.ShouldThrowAsync<InvalidOperationException>();
        exception.Message.ShouldBe("Handler failed");
        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
    }

    #endregion

    #region Handle Tests - Guard Clauses

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateTestContext();
        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        var act = async () => await _behavior.Handle(null!, context, nextStep, CancellationToken.None);

        // Assert
        var exception = await act.ShouldThrowAsync<ArgumentNullException>();
        exception.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        var act = async () => await _behavior.Handle(request, null!, nextStep, CancellationToken.None);

        // Assert
        var exception = await act.ShouldThrowAsync<ArgumentNullException>();
        exception.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        // Act
        var act = async () => await _behavior.Handle(request, context, null!, CancellationToken.None);

        // Assert
        var exception = await act.ShouldThrowAsync<ArgumentNullException>();
        exception.ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Handle Tests - Transaction Disposal

    [Fact]
    public async Task Handle_WhenCompleted_DisposesTransaction()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        _transaction.Received(1).Dispose();
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_DisposesTransaction()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            throw new InvalidOperationException("Handler failed");

        // Act
        var act = async () => await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        _transaction.Received(1).Dispose();
    }

    #endregion

    #region Helpers

    private static IRequestContext CreateTestContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns(Guid.NewGuid().ToString());
        context.UserId.Returns("test-user");
        context.Timestamp.Returns(DateTimeOffset.UtcNow);
        return context;
    }

    #endregion
}

/// <summary>
/// Test request for behavior tests.
/// </summary>
internal sealed record TestRequest(Guid Id) : IRequest<string>;
