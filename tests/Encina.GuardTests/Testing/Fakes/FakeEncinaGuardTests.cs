namespace Encina.GuardTests.Testing.Fakes;

/// <summary>
/// Guard tests for the explicit-context overloads of <see cref="FakeEncina"/> (issue #1147).
/// </summary>
public class FakeEncinaGuardTests
{
    private sealed class TestRequest : IRequest<string> { }

    private sealed class TestNotification : INotification { }

    private sealed class TestStreamRequest : IStreamRequest<int> { }

    [Fact]
    public async Task Send_ExplicitContext_NullContext_ThrowsArgumentNullException()
    {
        var sut = new FakeEncina();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await sut.Send(new TestRequest(), null!));

        ex.ParamName.ShouldBe("context");
        sut.SentRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_ExplicitContext_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new FakeEncina();
        IRequest<string> request = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await sut.Send(request, RequestContext.CreateForTest()));

        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Publish_ExplicitContext_NullContext_ThrowsArgumentNullException()
    {
        var sut = new FakeEncina();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await sut.Publish(new TestNotification(), null!));

        ex.ParamName.ShouldBe("context");
        sut.PublishedNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Publish_ExplicitContext_NullNotification_ThrowsArgumentNullException()
    {
        var sut = new FakeEncina();
        TestNotification notification = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await sut.Publish(notification, RequestContext.CreateForTest()));

        ex.ParamName.ShouldBe("notification");
    }

    [Fact]
    public void Stream_ExplicitContext_NullContext_ThrowsArgumentNullException()
    {
        var sut = new FakeEncina();

        Should.Throw<ArgumentNullException>(() => sut.Stream(new TestStreamRequest(), null!))
            .ParamName.ShouldBe("context");
        sut.StreamRequests.ShouldBeEmpty();
    }
}
