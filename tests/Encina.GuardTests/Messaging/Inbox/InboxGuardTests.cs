using Encina.Messaging.Inbox;
using Encina.Messaging.Serialization;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.Inbox;

/// <summary>
/// Guard clause tests for InboxOrchestrator and InboxPipelineBehavior.
/// </summary>
public class InboxGuardTests
{
    #region InboxOrchestrator Constructor

    [Fact]
    public void InboxOrchestrator_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new InboxOrchestrator(
            null!,
            new InboxOptions(),
            NullLogger<InboxOrchestrator>.Instance,
            Substitute.For<IInboxMessageFactory>(),
            Substitute.For<IMessageSerializer>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public void InboxOrchestrator_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new InboxOrchestrator(
            Substitute.For<IInboxStore>(),
            null!,
            NullLogger<InboxOrchestrator>.Instance,
            Substitute.For<IInboxMessageFactory>(),
            Substitute.For<IMessageSerializer>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void InboxOrchestrator_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InboxOrchestrator(
            Substitute.For<IInboxStore>(),
            new InboxOptions(),
            null!,
            Substitute.For<IInboxMessageFactory>(),
            Substitute.For<IMessageSerializer>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void InboxOrchestrator_NullMessageFactory_ThrowsArgumentNullException()
    {
        var act = () => new InboxOrchestrator(
            Substitute.For<IInboxStore>(),
            new InboxOptions(),
            NullLogger<InboxOrchestrator>.Instance,
            null!,
            Substitute.For<IMessageSerializer>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("messageFactory");
    }

    [Fact]
    public void InboxOrchestrator_NullMessageSerializer_ThrowsArgumentNullException()
    {
        var act = () => new InboxOrchestrator(
            Substitute.For<IInboxStore>(),
            new InboxOptions(),
            NullLogger<InboxOrchestrator>.Instance,
            Substitute.For<IInboxMessageFactory>(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("messageSerializer");
    }

    #endregion

    #region InboxOrchestrator.ProcessAsync

    [Fact]
    public async Task ProcessAsync_NullMessageId_ThrowsArgumentException()
    {
        var orchestrator = CreateOrchestrator();

        var act = async () => await orchestrator.ProcessAsync<string>(
            null!, "type", "corr", null, () => default);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    [Fact]
    public async Task ProcessAsync_EmptyMessageId_ThrowsArgumentException()
    {
        var orchestrator = CreateOrchestrator();

        var act = async () => await orchestrator.ProcessAsync<string>(
            "", "type", "corr", null, () => default);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    [Fact]
    public async Task ProcessAsync_WhitespaceMessageId_ThrowsArgumentException()
    {
        var orchestrator = CreateOrchestrator();

        var act = async () => await orchestrator.ProcessAsync<string>(
            "   ", "type", "corr", null, () => default);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    [Fact]
    public async Task ProcessAsync_NullRequestType_ThrowsArgumentException()
    {
        var orchestrator = CreateOrchestrator();

        var act = async () => await orchestrator.ProcessAsync<string>(
            "msg-1", null!, "corr", null, () => default);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("requestType");
    }

    [Fact]
    public async Task ProcessAsync_EmptyRequestType_ThrowsArgumentException()
    {
        var orchestrator = CreateOrchestrator();

        var act = async () => await orchestrator.ProcessAsync<string>(
            "msg-1", "", "corr", null, () => default);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("requestType");
    }

    [Fact]
    public async Task ProcessAsync_NullProcessCallback_ThrowsArgumentNullException()
    {
        var orchestrator = CreateOrchestrator();

        var act = async () => await orchestrator.ProcessAsync<string>(
            "msg-1", "type", "corr", null, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("processCallback");
    }

    #endregion

    #region InboxPipelineBehavior Constructor

    [Fact]
    public void InboxPipelineBehavior_NullOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new InboxPipelineBehavior<TestRequest, string>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("orchestrator");
    }

    #endregion

    #region Helpers

    private static InboxOrchestrator CreateOrchestrator()
    {
        return new InboxOrchestrator(
            Substitute.For<IInboxStore>(),
            new InboxOptions(),
            NullLogger<InboxOrchestrator>.Instance,
            Substitute.For<IInboxMessageFactory>(),
            Substitute.For<IMessageSerializer>());
    }

    private sealed class TestRequest : IRequest<string>
    {
    }

    #endregion
}
