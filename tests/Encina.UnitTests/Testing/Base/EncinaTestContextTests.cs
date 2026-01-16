using Encina.Testing;
using Encina.Messaging.Outbox;
using LanguageExt;

namespace Encina.UnitTests.Testing.Base;

/// <summary>
/// Unit tests for <see cref="EncinaTestContext{TResponse}"/>.
/// </summary>
public sealed class EncinaTestContextTests : IDisposable
{
    private EncinaTestFixture? _fixture;

    public void Dispose()
    {
        _fixture?.Dispose();
    }

    [Fact]
    public async Task ShouldSucceed_WithSuccessResult_ShouldNotThrow()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        Should.NotThrow(() => context.ShouldSucceed());
    }

    [Fact]
    public async Task ShouldSucceed_WithErrorResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new FailingRequest("test"));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(() => context.ShouldSucceed());
        ex.Message.ShouldContain("Expected success");
    }

    [Fact]
    public async Task ShouldSucceedWith_WithSuccessResult_ShouldInvokeVerification()
    {
        // Arrange
        _fixture = CreateFixture();
        var wasVerified = false;

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test-value"));

        context.ShouldSucceedWith(value =>
        {
            value.ShouldBe("test-value");
            wasVerified = true;
        });

        // Assert
        wasVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldSucceedWith_WithErrorResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new FailingRequest("test"));

        // Assert
        Should.Throw<InvalidOperationException>(() =>
            context.ShouldSucceedWith(_ => { }));
    }

    [Fact]
    public async Task ShouldFail_WithErrorResult_ShouldNotThrow()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new FailingRequest("test"));

        // Assert
        Should.NotThrow(() => context.ShouldFail());
    }

    [Fact]
    public async Task ShouldFail_WithSuccessResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(() => context.ShouldFail());
        ex.Message.ShouldContain("Expected error");
    }

    [Fact]
    public async Task ShouldFailWith_WithErrorResult_ShouldInvokeVerification()
    {
        // Arrange
        _fixture = CreateFixture();
        var wasVerified = false;

        // Act
        var context = await _fixture.SendAsync(new FailingRequest("test"));

        context.ShouldFailWith(err =>
        {
            err.Message.ShouldContain("Test error");
            wasVerified = true;
        });

        // Assert
        wasVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task GetSuccessValue_WithSuccessResult_ShouldReturnValue()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test-value"));
        var value = context.GetSuccessValue();

        // Assert
        value.ShouldBe("test-value");
    }

    [Fact]
    public async Task GetSuccessValue_WithErrorResult_ShouldThrow()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new FailingRequest("test"));

        // Assert
        Should.Throw<InvalidOperationException>(() => context.GetSuccessValue());
    }

    [Fact]
    public async Task GetErrorValue_WithErrorResult_ShouldReturnError()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new FailingRequest("test"));
        var error = context.GetErrorValue();

        // Assert
        error.Message.ShouldContain("Test error");
    }

    [Fact]
    public async Task GetErrorValue_WithSuccessResult_ShouldThrow()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        Should.Throw<InvalidOperationException>(() => context.GetErrorValue());
    }

    [Fact]
    public async Task And_ShouldReturnSameContext()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));
        var andContext = context.And;

        // Assert
        andContext.ShouldBe(context);
    }

    [Fact]
    public async Task IsSuccess_WithSuccessResult_ShouldReturnTrue()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        context.IsSuccess.ShouldBeTrue();
        context.IsError.ShouldBeFalse();
    }

    [Fact]
    public async Task IsError_WithErrorResult_ShouldReturnTrue()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new FailingRequest("test"));

        // Assert
        context.IsError.ShouldBeTrue();
        context.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ImplicitConversionToEither_ShouldReturnResult()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));
        Either<EncinaError, string> converted = context;

        // Assert
        converted.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task OutboxShouldBeEmpty_WhenOutboxEmpty_ShouldNotThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        Should.NotThrow(() => context.OutboxShouldBeEmpty());
    }

    [Fact]
    public async Task OutboxShouldContainExactly_WithCorrectCount_ShouldNotThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        Should.NotThrow(() => context.OutboxShouldContainExactly(0));
    }

    [Fact]
    public async Task OutboxShouldContainExactly_WithIncorrectCount_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        Should.Throw<InvalidOperationException>(() =>
            context.OutboxShouldContainExactly(1))
            .Message.ShouldContain("exactly 1");
    }

    [Fact]
    public async Task OutboxShouldContain_WhenNotificationExists_ShouldNotThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithHandler<NotificationPublishingHandler>();

        // Act
        var context = await _fixture.SendAsync(new NotificationRequest("test"));

        // Assert
        Should.NotThrow(() => context.OutboxShouldContain<TestNotification>());
    }

    [Fact]
    public async Task OutboxShouldContain_WhenNotificationDoesNotExist_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        Should.Throw<InvalidOperationException>(() =>
            context.OutboxShouldContain<TestNotification>())
            .Message.ShouldContain("TestNotification");
    }

    [Fact]
    public async Task OutboxShouldContain_WithPredicate_WhenMatchingNotificationExists_ShouldNotThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithHandler<NotificationPublishingHandler>();

        // Act
        var context = await _fixture.SendAsync(new NotificationRequest("expected-value"));

        // Assert
        Should.NotThrow(() => context.OutboxShouldContain<TestNotification>(n => n.Message == "expected-value"));
    }

    [Fact]
    public async Task OutboxShouldContain_WithPredicate_WhenPredicateDoesNotMatch_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithHandler<NotificationPublishingHandler>();

        // Act
        var context = await _fixture.SendAsync(new NotificationRequest("actual-value"));

        // Assert
        Should.Throw<InvalidOperationException>(() =>
            context.OutboxShouldContain<TestNotification>(n => n.Message == "expected-value"))
            .Message.ShouldContain("TestNotification");
    }

    [Fact]
    public async Task FluentChaining_ShouldWorkCorrectly()
    {
        // Arrange
        _fixture = CreateFixture();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test-value"));

        // Assert - fluent chaining
        context
            .ShouldSucceed()
            .And.ShouldSucceedWith(v => v.ShouldBe("test-value"))
            .And.ShouldSatisfy(v => v.Length.ShouldBeGreaterThan(0));
    }

    private static EncinaTestFixture CreateFixture()
    {
        var fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithMockedSaga()
            .WithHandler<SuccessHandler>()
            .WithHandler<FailingHandler>();
        return fixture;
    }

    #region Time-Travel Testing

    [Fact]
    public async Task ThenAdvanceTimeBy_ShouldAdvanceTimeAndReturnContext()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime)
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));
        var result = context.ThenAdvanceTimeBy(TimeSpan.FromHours(2));

        // Assert
        result.ShouldBe(context);
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(2));
    }

    [Fact]
    public async Task ThenAdvanceTimeByMinutes_ShouldAdvanceTimeByMinutes()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime)
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));
        context.ThenAdvanceTimeByMinutes(45);

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddMinutes(45));
    }

    [Fact]
    public async Task ThenAdvanceTimeByHours_ShouldAdvanceTimeByHours()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime)
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));
        context.ThenAdvanceTimeByHours(3);

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(3));
    }

    [Fact]
    public async Task ThenAdvanceTimeByDays_ShouldAdvanceTimeByDays()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime)
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));
        context.ThenAdvanceTimeByDays(5);

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddDays(5));
    }

    [Fact]
    public async Task ThenSetTimeTo_ShouldSetTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var targetTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime)
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));
        context.ThenSetTimeTo(targetTime);

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(targetTime);
    }

    [Fact]
    public async Task ThenAdvanceTimeBy_WithoutTimeProvider_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(
            () => context.ThenAdvanceTimeBy(TimeSpan.FromHours(1)));
        ex.Message.ShouldContain("WithFakeTimeProvider");
    }

    [Fact]
    public async Task TimeTravelChaining_ShouldWorkWithOtherAssertions()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime)
            .WithMockedOutbox()
            .WithHandler<SuccessHandler>();

        // Act & Assert - fluent chaining with time travel
        var context = await _fixture.SendAsync(new SuccessRequest("test"));
        context
            .ShouldSucceed()
            .ThenAdvanceTimeByHours(2)
            .And.OutboxShouldBeEmpty();

        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(2));
    }

    #endregion

    #region Saga State Assertions

    [Fact]
    public async Task SagaShouldHaveTimedOut_WhenSagaNotTimedOut_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedSaga()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(
            () => context.SagaShouldHaveTimedOut<TestSaga>());
        ex.Message.ShouldContain("TestSaga");
        ex.Message.ShouldContain("timed out");
    }

    [Fact]
    public async Task SagaShouldHaveCompleted_WhenSagaNotCompleted_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedSaga()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(
            () => context.SagaShouldHaveCompleted<TestSaga>());
        ex.Message.ShouldContain("TestSaga");
        ex.Message.ShouldContain("completed");
    }

    [Fact]
    public async Task SagaShouldBeCompensating_WhenSagaNotCompensating_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedSaga()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(
            () => context.SagaShouldBeCompensating<TestSaga>());
        ex.Message.ShouldContain("TestSaga");
        ex.Message.ShouldContain("compensating");
    }

    [Fact]
    public async Task SagaShouldHaveFailed_WhenSagaNotFailed_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedSaga()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(
            () => context.SagaShouldHaveFailed<TestSaga>());
        ex.Message.ShouldContain("TestSaga");
        ex.Message.ShouldContain("failed");
    }

    [Fact]
    public async Task SagaStateAssertion_WithoutMockedSaga_ShouldThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithHandler<SuccessHandler>();

        // Act
        var context = await _fixture.SendAsync(new SuccessRequest("test"));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(
            () => context.SagaShouldBeStarted<TestSaga>());
        ex.Message.ShouldContain("WithMockedSaga");
    }

    #endregion

    #region Test Helpers

    private sealed class TestSaga { }

    private sealed record TestNotification(string Message) : INotification;

    private sealed record SuccessRequest(string Value) : IRequest<string>;

    private sealed record FailingRequest(string Value) : IRequest<string>;

    /// <summary>
    /// Request that triggers notification publishing.
    /// </summary>
    private sealed record NotificationRequest(string Value) : IRequest<string>;

    private sealed class SuccessHandler : IRequestHandler<SuccessRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(SuccessRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Either<EncinaError, string>.Right(request.Value));
        }
    }

    /// <summary>
    /// Handler that publishes a notification to the outbox.
    /// </summary>
    private sealed class NotificationPublishingHandler : IRequestHandler<NotificationRequest, string>
    {
        private readonly IOutboxStore _outboxStore;
        private readonly IOutboxMessageFactory _messageFactory;

        public NotificationPublishingHandler(IOutboxStore outboxStore, IOutboxMessageFactory messageFactory)
        {
            _outboxStore = outboxStore;
            _messageFactory = messageFactory;
        }

        public async Task<Either<EncinaError, string>> Handle(NotificationRequest request, CancellationToken cancellationToken)
        {
            var notification = new TestNotification(request.Value);
            var notificationType = typeof(TestNotification).FullName ?? typeof(TestNotification).Name;
            var content = System.Text.Json.JsonSerializer.Serialize(notification);

            var message = _messageFactory.Create(
                Guid.NewGuid(),
                notificationType,
                content,
                DateTime.UtcNow);

            await _outboxStore.AddAsync(message, cancellationToken);
            return Either<EncinaError, string>.Right(request.Value);
        }
    }

    private sealed class FailingHandler : IRequestHandler<FailingRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(FailingRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Either<EncinaError, string>.Left(EncinaError.New("Test error message")));
        }
    }

    #endregion
}
