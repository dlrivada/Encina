using LanguageExt;
using Microsoft.Extensions.Logging;
using Encina.ADO.PostgreSQL.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.ADO.PostgreSQL.GuardTests;

/// <summary>
/// Guard tests for <see cref="InboxPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class InboxPipelineBehaviorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when inboxStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullInboxStore_ThrowsArgumentNullException()
    {
        // Arrange
        IInboxStore inboxStore = null!;
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, string>(inboxStore, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("inboxStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var inboxStore = Substitute.For<IInboxStore>();
        InboxOptions options = null!;
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, string>(inboxStore, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var inboxStore = Substitute.For<IInboxStore>();
        var options = new InboxOptions();
        ILogger<InboxPipelineBehavior<TestRequest, string>> logger = null!;

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, string>(inboxStore, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var inboxStore = Substitute.For<IInboxStore>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(inboxStore, options, logger);

        TestRequest request = null!;
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult<Either<MediatorError, string>>("test");

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var inboxStore = Substitute.For<IInboxStore>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(inboxStore, options, logger);

        var request = new TestRequest();
        IRequestContext context = null!;
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult<Either<MediatorError, string>>("test");

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var inboxStore = Substitute.For<IInboxStore>();
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(inboxStore, options, logger);

        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = null!;

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("nextStep");
    }

    /// <summary>
    /// Test request for testing.
    /// </summary>
    public sealed record TestRequest : IRequest<string>;
}
