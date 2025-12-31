using System.Data;

namespace Encina.ADO.Oracle.GuardTests;

/// <summary>
/// Guard tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class TransactionPipelineBehaviorGuardsTests
{
    internal sealed class TestRequest : IRequest<string>;

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;

        // Act & Assert
        var act = () => new TransactionPipelineBehavior<TestRequest, string>(connection);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);

        TestRequest request = null!;
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult<LanguageExt.Either<EncinaError, string>>("result");

        // Act & Assert
        var act = () => behavior.Handle(request, context, next, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);

        var request = new TestRequest();
        IRequestContext context = null!;
        RequestHandlerCallback<string> next = () => ValueTask.FromResult<LanguageExt.Either<EncinaError, string>>("result");

        // Act & Assert
        var act = () => behavior.Handle(request, context, next, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when next is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNext_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);

        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = null!;

        // Act & Assert
        var act = () => behavior.Handle(request, context, next, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("next");
    }
}
