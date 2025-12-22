using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Encina.ADO.MySQL.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.ADO.MySQL.GuardTests;

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
        var logger = NullLogger<InboxPipelineBehavior<TestRequest, string>>.Instance;

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
        var logger = NullLogger<InboxPipelineBehavior<TestRequest, string>>.Instance;

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
    /// Test request for guard tests.
    /// </summary>
    private sealed record TestRequest : IRequest<string>;
}
