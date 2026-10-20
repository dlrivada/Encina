using System.Diagnostics;

using Encina.Messaging.DeadLetter;
using Encina.OpenTelemetry.MessagingStores;
using Encina.Testing.Shouldly;

using LanguageExt;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.OpenTelemetry.MessagingStores;

/// <summary>
/// Unit tests for <see cref="InstrumentedDeadLetterStore"/>: delegation, the span names and tags of each
/// operation, and that no tag carries a payload, a tenant id or the text of an error (SPEC-002 REQ-062, #1788).
/// </summary>
public sealed class InstrumentedDeadLetterStoreTests : IDisposable
{
    private const string SourceName = "Encina.Messaging.DeadLetter";

    private readonly IDeadLetterStore _inner = Substitute.For<IDeadLetterStore>();
    private readonly InstrumentedDeadLetterStore _sut;
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];
    private readonly object _gate = new();

    public InstrumentedDeadLetterStoreTests()
    {
        _sut = new InstrumentedDeadLetterStore(_inner);
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (_gate)
                {
                    _activities.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public async Task AddAsync_DelegatesAndTagsTheSourcePatternAndTheMessageId()
    {
        var message = Message("Outbox", tenantId: "tenant-secret-42");
        _inner.AddAsync(message, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));

        var result = await _sut.AddAsync(message);

        result.ShouldBeRight().ShouldBeTrue();
        var activity = Single("encina.dlq.add", message.Id);
        activity.GetTagItem("dlq.source_pattern").ShouldBe("Outbox");
        activity.GetTagItem("dlq.message_id").ShouldBe(message.Id.ToString());
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task AddAsync_WhenTheStoreFails_TagsTheErrorCodeAndNeverTheMessageText()
    {
        var message = Message("Inbox", tenantId: "tenant-secret-42");
        _inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("dlq.store_failed", "patient Jane Doe 123")));

        var result = await _sut.AddAsync(message);

        result.ShouldBeErrorWithCode("dlq.store_failed");
        var activity = Single("encina.dlq.add", message.Id);
        activity.GetTagItem("encina.error_code").ShouldBe("dlq.store_failed");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBeNull();
        AssertNoSensitiveValue(activity, "tenant-secret-42", "patient Jane Doe 123", "payload-secret");
    }

    [Fact]
    public async Task GetMessagesAsync_TagsTheSourcePatternAndTheResultCount()
    {
        var pattern = $"Saga-{Guid.NewGuid():N}";
        var rows = new[] { Message(pattern), Message(pattern) };
        _inner.GetMessagesAsync(Arg.Any<DeadLetterFilter?>(), 5, 10, true, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(rows));

        var result = await _sut.GetMessagesAsync(new DeadLetterFilter { SourcePattern = pattern, TenantId = "tenant-secret-42" }, 5, 10, true);

        result.ShouldBeRight().Count().ShouldBe(2);
        var activity = Single("encina.dlq.query", pattern);
        activity.GetTagItem("dlq.count").ShouldBe(2);
        AssertNoSensitiveValue(activity, "tenant-secret-42");
    }

    [Fact]
    public async Task GetCountAsync_TagsTheCount()
    {
        var pattern = $"Count-{Guid.NewGuid():N}";
        _inner.GetCountAsync(Arg.Any<DeadLetterFilter?>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, int>(7));

        (await _sut.GetCountAsync(new DeadLetterFilter { SourcePattern = pattern })).ShouldBeRight().ShouldBe(7);

        Single("encina.dlq.count", pattern).GetTagItem("dlq.count").ShouldBe(7);
    }

    [Fact]
    public async Task TryClaimForReplayAsync_And_MarkAsReplayedAsync_TagTheMessageId()
    {
        var id = Guid.NewGuid();
        _inner.TryClaimForReplayAsync(id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));
        _inner.MarkAsReplayedAsync(id, "replay.succeeded", Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));

        (await _sut.TryClaimForReplayAsync(id, DateTime.UnixEpoch)).ShouldBeRight().ShouldBeTrue();
        (await _sut.MarkAsReplayedAsync(id, "replay.succeeded")).ShouldBeRight().ShouldBeTrue();

        Single("encina.dlq.replay_claim", id).GetTagItem("dlq.message_id").ShouldBe(id.ToString());
        var mark = Single("encina.dlq.replay_mark", id);
        mark.GetTagItem("dlq.message_id").ShouldBe(id.ToString());
        AssertNoSensitiveValue(mark, "replay.succeeded");
    }

    [Fact]
    public async Task DeleteAsync_DeleteManyAsync_DeleteExpiredAsync_EmitTheirSpans()
    {
        var id = Guid.NewGuid();
        var pattern = $"Delete-{Guid.NewGuid():N}";
        _inner.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));
        _inner.DeleteManyAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, int>(4));
        _inner.DeleteExpiredAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, int>(2));

        (await _sut.DeleteAsync(id)).ShouldBeRight().ShouldBeTrue();
        (await _sut.DeleteManyAsync(new DeadLetterFilter { SourcePattern = pattern })).ShouldBeRight().ShouldBe(4);
        (await _sut.DeleteExpiredAsync()).ShouldBeRight().ShouldBe(2);

        Single("encina.dlq.delete", id).Status.ShouldBe(ActivityStatusCode.Ok);
        Single("encina.dlq.delete_many", pattern).GetTagItem("dlq.count").ShouldBe(4);
        Named("encina.dlq.delete_expired").ShouldContain(a => (int?)a.GetTagItem("dlq.count") == 2);
    }

    [Fact]
    public async Task GetAsync_And_SaveChangesAsync_DelegateWithoutASpan()
    {
        var id = Guid.NewGuid();
        _inner.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.None));
        _inner.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));

        (await _sut.GetAsync(id)).ShouldBeRight().IsNone.ShouldBeTrue();
        (await _sut.SaveChangesAsync()).ShouldBeRight();

        await _inner.Received(1).GetAsync(id, Arg.Any<CancellationToken>());
        await _inner.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteManyAsync_NullFilter_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _sut.DeleteManyAsync(null!));
    }

    [Fact]
    public async Task AddAsync_NullMessage_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _sut.AddAsync(null!));
    }

    private static IDeadLetterMessage Message(string sourcePattern, string? tenantId = null)
    {
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.SourcePattern.Returns(sourcePattern);
        message.TenantId.Returns(tenantId);
        message.RequestContent.Returns("payload-secret");
        return message;
    }

    private List<Activity> Named(string name)
    {
        lock (_gate)
        {
            return _activities.Where(a => a.OperationName == name).ToList();
        }
    }

    // The listener sees every test's spans (the source is static), so a span is matched by a value unique to the test.
    private Activity Single(string name, object key)
    {
        var text = key.ToString();
        return Named(name).Single(a => a.TagObjects.Any(t => string.Equals(t.Value?.ToString(), text, StringComparison.Ordinal)));
    }

    private static void AssertNoSensitiveValue(Activity activity, params string[] forbidden)
    {
        foreach (var tag in activity.TagObjects)
        {
            forbidden.ShouldNotContain(tag.Value?.ToString());
        }

        forbidden.ShouldNotContain(activity.StatusDescription);
    }
}
