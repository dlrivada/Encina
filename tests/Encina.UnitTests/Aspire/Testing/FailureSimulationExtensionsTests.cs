using Encina.Aspire.Testing;
using Encina.Messaging.Sagas;
using Encina.Testing.Fakes.Models;
using Shouldly;

namespace Encina.UnitTests.Aspire.Testing;

/// <summary>
/// Unit tests for <see cref="FailureSimulationExtensions"/> run against a real
/// <c>DistributedApplication</c> over a host that is never started.
/// </summary>
public sealed class FailureSimulationExtensionsTests
{
    private sealed class PaymentSaga;

    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(2);

    private static FakeOutboxMessage NewOutbox() => new() { NotificationType = "Type.A", Content = "{}" };

    private static FakeInboxMessage NewInbox(string id) => new() { MessageId = id, RequestType = "Type.A" };

    // ─── Sagas ───

    [Fact]
    public void SimulateSagaTimeout_MarksSagaTimedOut()
    {
        using var host = AspireTestApp.Create();
        var saga = host.AddSaga<PaymentSaga>();
        var before = DateTime.UtcNow;

        host.App.SimulateSagaTimeout(saga.SagaId);

        var stored = host.Sagas.GetSaga(saga.SagaId)!;
        stored.Status.ShouldBe(SagaStatus.TimedOut);
        stored.TimeoutAtUtc.ShouldNotBeNull();
        stored.TimeoutAtUtc!.Value.ShouldBeGreaterThanOrEqualTo(before - Tolerance);
        stored.LastUpdatedAtUtc.ShouldBeGreaterThanOrEqualTo(before - Tolerance);
    }

    [Fact]
    public void SimulateSagaTimeout_UnknownSaga_ThrowsInvalidOperationException()
    {
        using var host = AspireTestApp.Create();
        var missing = Guid.NewGuid();

        var ex = Should.Throw<InvalidOperationException>(() => host.App.SimulateSagaTimeout(missing));

        ex.Message.ShouldContain(missing.ToString());
    }

    [Fact]
    public void SimulateSagaFailure_DefaultMessage_MarksSagaFailed()
    {
        using var host = AspireTestApp.Create();
        var saga = host.AddSaga<PaymentSaga>();

        host.App.SimulateSagaFailure(saga.SagaId);

        var stored = host.Sagas.GetSaga(saga.SagaId)!;
        stored.Status.ShouldBe(SagaStatus.Failed);
        stored.ErrorMessage.ShouldBe("Simulated failure for testing");
        stored.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void SimulateSagaFailure_CustomMessage_IsStored()
    {
        using var host = AspireTestApp.Create();
        var saga = host.AddSaga<PaymentSaga>();

        host.App.SimulateSagaFailure(saga.SagaId, "boom");

        host.Sagas.GetSaga(saga.SagaId)!.ErrorMessage.ShouldBe("boom");
    }

    [Fact]
    public void SimulateSagaFailure_UnknownSaga_ThrowsInvalidOperationException()
    {
        using var host = AspireTestApp.Create();

        Should.Throw<InvalidOperationException>(() => host.App.SimulateSagaFailure(Guid.NewGuid()));
    }

    // ─── Outbox ───

    [Fact]
    public async Task SimulateOutboxMessageFailure_DefaultRetry_IncrementsRetryAndSchedulesRetry()
    {
        using var host = AspireTestApp.Create();
        var message = NewOutbox();
        await host.Outbox.AddAsync(message);
        var before = DateTime.UtcNow;

        host.App.SimulateOutboxMessageFailure(message.Id);

        var stored = host.Outbox.GetMessage(message.Id)!;
        stored.ErrorMessage.ShouldBe("Simulated failure for testing");
        stored.RetryCount.ShouldBe(1);
        stored.NextRetryAtUtc.ShouldNotBeNull();
        stored.NextRetryAtUtc!.Value.ShouldBeGreaterThan(before);
    }

    [Fact]
    public async Task SimulateOutboxMessageFailure_ExplicitRetryTime_IsUsedAndCountAccumulates()
    {
        using var host = AspireTestApp.Create();
        var message = NewOutbox();
        await host.Outbox.AddAsync(message);
        var retryAt = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        host.App.SimulateOutboxMessageFailure(message.Id, "first", retryAt);
        host.App.SimulateOutboxMessageFailure(message.Id, "second", retryAt);

        var stored = host.Outbox.GetMessage(message.Id)!;
        stored.ErrorMessage.ShouldBe("second");
        stored.RetryCount.ShouldBe(2);
        stored.NextRetryAtUtc.ShouldBe(retryAt);
    }

    [Fact]
    public void SimulateOutboxMessageFailure_EmptyGuid_ThrowsArgumentException()
    {
        using var host = AspireTestApp.Create();

        var ex = Should.Throw<ArgumentException>(() => host.App.SimulateOutboxMessageFailure(Guid.Empty));

        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public void SimulateOutboxMessageFailure_UnknownMessage_ThrowsInvalidOperationException()
    {
        using var host = AspireTestApp.Create();

        Should.Throw<InvalidOperationException>(() => host.App.SimulateOutboxMessageFailure(Guid.NewGuid()));
    }

    [Fact]
    public async Task SimulateOutboxDeadLetter_DefaultMaxRetries_ExceedsRetriesAndClearsNextRetry()
    {
        using var host = AspireTestApp.Create();
        var message = NewOutbox();
        message.NextRetryAtUtc = DateTime.UtcNow;
        await host.Outbox.AddAsync(message);

        host.App.SimulateOutboxDeadLetter(message.Id);

        var stored = host.Outbox.GetMessage(message.Id)!;
        stored.ErrorMessage.ShouldBe("Max retries exceeded");
        stored.RetryCount.ShouldBe(4);
        stored.NextRetryAtUtc.ShouldBeNull();
        stored.IsDeadLettered(3).ShouldBeTrue();
    }

    [Fact]
    public async Task SimulateOutboxDeadLetter_CustomArguments_AreApplied()
    {
        using var host = AspireTestApp.Create();
        var message = NewOutbox();
        await host.Outbox.AddAsync(message);

        host.App.SimulateOutboxDeadLetter(message.Id, maxRetries: 10, errorMessage: "gave up");

        var stored = host.Outbox.GetMessage(message.Id)!;
        stored.RetryCount.ShouldBe(11);
        stored.ErrorMessage.ShouldBe("gave up");
    }

    [Fact]
    public void SimulateOutboxDeadLetter_UnknownMessage_ThrowsInvalidOperationException()
    {
        using var host = AspireTestApp.Create();

        Should.Throw<InvalidOperationException>(() => host.App.SimulateOutboxDeadLetter(Guid.NewGuid()));
    }

    // ─── Inbox ───

    [Fact]
    public async Task SimulateInboxMessageFailure_DefaultRetry_IncrementsRetryAndSchedulesRetry()
    {
        using var host = AspireTestApp.Create();
        await host.Inbox.AddAsync(NewInbox("in-1"));
        var before = DateTime.UtcNow;

        host.App.SimulateInboxMessageFailure("in-1");

        var stored = host.Inbox.GetMessage("in-1")!;
        stored.ErrorMessage.ShouldBe("Simulated failure for testing");
        stored.RetryCount.ShouldBe(1);
        stored.NextRetryAtUtc!.Value.ShouldBeGreaterThan(before);
    }

    [Fact]
    public async Task SimulateInboxMessageFailure_ExplicitRetryTime_IsUsed()
    {
        using var host = AspireTestApp.Create();
        await host.Inbox.AddAsync(NewInbox("in-2"));
        var retryAt = new DateTime(2031, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        host.App.SimulateInboxMessageFailure("in-2", "custom", retryAt);

        var stored = host.Inbox.GetMessage("in-2")!;
        stored.ErrorMessage.ShouldBe("custom");
        stored.NextRetryAtUtc.ShouldBe(retryAt);
    }

    [Fact]
    public void SimulateInboxMessageFailure_UnknownMessage_ThrowsInvalidOperationException()
    {
        using var host = AspireTestApp.Create();

        Should.Throw<InvalidOperationException>(() => host.App.SimulateInboxMessageFailure("missing"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SimulateInboxMessageFailure_InvalidId_ThrowsArgumentException(string? messageId)
    {
        using var host = AspireTestApp.Create();

        Should.Throw<ArgumentException>(() => host.App.SimulateInboxMessageFailure(messageId!));
    }

    [Fact]
    public async Task SimulateInboxExpiration_MovesExpiryIntoThePast()
    {
        using var host = AspireTestApp.Create();
        await host.Inbox.AddAsync(NewInbox("in-3"));

        host.App.SimulateInboxExpiration("in-3");

        var stored = host.Inbox.GetMessage("in-3")!;
        stored.ExpiresAtUtc.ShouldBeLessThan(DateTime.UtcNow);
        stored.IsExpired().ShouldBeTrue();
    }

    [Fact]
    public void SimulateInboxExpiration_UnknownMessage_ThrowsInvalidOperationException()
    {
        using var host = AspireTestApp.Create();

        Should.Throw<InvalidOperationException>(() => host.App.SimulateInboxExpiration("missing"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SimulateInboxExpiration_InvalidId_ThrowsArgumentException(string? messageId)
    {
        using var host = AspireTestApp.Create();

        Should.Throw<ArgumentException>(() => host.App.SimulateInboxExpiration(messageId!));
    }

    // ─── Dead letter ───

    [Fact]
    public async Task AddToDeadLetterAsync_Defaults_StoresMessageWithDefaults()
    {
        using var host = AspireTestApp.Create();

        var id = await host.App.AddToDeadLetterAsync("Type.A", "{\"a\":1}");

        id.ShouldNotBe(Guid.Empty);
        var stored = host.DeadLetters.GetMessage(id)!;
        stored.RequestType.ShouldBe("Type.A");
        stored.RequestContent.ShouldBe("{\"a\":1}");
        stored.SourcePattern.ShouldBe("Simulated");
        stored.ErrorCode.ShouldBe("Simulated dead letter");
        stored.TotalRetryAttempts.ShouldBe(3);
        stored.SourceMessageId.ShouldBe(id.ToString("D"));
    }

    [Fact]
    public async Task AddToDeadLetterAsync_CustomArguments_AreStored()
    {
        using var host = AspireTestApp.Create();

        var id = await host.App.AddToDeadLetterAsync("Type.B", "{}", "Outbox", "failed hard", 7);

        var stored = host.DeadLetters.GetMessage(id)!;
        stored.SourcePattern.ShouldBe("Outbox");
        stored.ErrorCode.ShouldBe("failed hard");
        stored.TotalRetryAttempts.ShouldBe(7);
        host.DeadLetters.WasMessageDeadLettered("Outbox").ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, "{}")]
    [InlineData("", "{}")]
    [InlineData("Type.A", null)]
    [InlineData("Type.A", " ")]
    public async Task AddToDeadLetterAsync_InvalidArguments_ThrowsArgumentException(string? requestType, string? content)
    {
        using var host = AspireTestApp.Create();

        await Should.ThrowAsync<ArgumentException>(
            () => host.App.AddToDeadLetterAsync(requestType!, content!));
    }
}
