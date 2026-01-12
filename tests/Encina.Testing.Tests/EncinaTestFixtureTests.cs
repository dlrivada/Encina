using Encina.Testing.Fakes;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Time;

namespace Encina.Testing.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaTestFixture"/>.
/// </summary>
public sealed class EncinaTestFixtureTests : IDisposable
{
    private EncinaTestFixture? _fixture;

    public void Dispose()
    {
        _fixture?.Dispose();
    }

    [Fact]
    public void WithMockedOutbox_ShouldMakeOutboxAccessible()
    {
        // Arrange & Act
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox();
        _fixture.Build();

        // Assert
        _fixture.Outbox.ShouldNotBeNull();
        _fixture.Outbox.ShouldBeOfType<FakeOutboxStore>();
    }

    [Fact]
    public void WithMockedInbox_ShouldMakeInboxAccessible()
    {
        // Arrange & Act
        _fixture = new EncinaTestFixture()
            .WithMockedInbox();
        _fixture.Build();

        // Assert
        _fixture.Inbox.ShouldNotBeNull();
        _fixture.Inbox.ShouldBeOfType<FakeInboxStore>();
    }

    [Fact]
    public void WithMockedSaga_ShouldMakeSagaStoreAccessible()
    {
        // Arrange & Act
        _fixture = new EncinaTestFixture()
            .WithMockedSaga();
        _fixture.Build();

        // Assert
        _fixture.SagaStore.ShouldNotBeNull();
        _fixture.SagaStore.ShouldBeOfType<FakeSagaStore>();
    }

    [Fact]
    public void WithMockedScheduling_ShouldMakeScheduledMessageStoreAccessible()
    {
        // Arrange & Act
        _fixture = new EncinaTestFixture()
            .WithMockedScheduling();
        _fixture.Build();

        // Assert
        _fixture.ScheduledMessageStore.ShouldNotBeNull();
        _fixture.ScheduledMessageStore.ShouldBeOfType<FakeScheduledMessageStore>();
    }

    [Fact]
    public void WithMockedDeadLetter_ShouldMakeDeadLetterStoreAccessible()
    {
        // Arrange & Act
        _fixture = new EncinaTestFixture()
            .WithMockedDeadLetter();
        _fixture.Build();

        // Assert
        _fixture.DeadLetterStore.ShouldNotBeNull();
        _fixture.DeadLetterStore.ShouldBeOfType<FakeDeadLetterStore>();
    }

    [Fact]
    public void WithAllMockedStores_ShouldMakeAllStoresAccessible()
    {
        // Arrange & Act
        _fixture = new EncinaTestFixture()
            .WithAllMockedStores();
        _fixture.Build();

        // Assert
        _fixture.Outbox.ShouldNotBeNull();
        _fixture.Inbox.ShouldNotBeNull();
        _fixture.SagaStore.ShouldNotBeNull();
        _fixture.ScheduledMessageStore.ShouldNotBeNull();
        _fixture.DeadLetterStore.ShouldNotBeNull();
    }

    [Fact]
    public void Outbox_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _fixture = new EncinaTestFixture();
        _fixture.Build();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = _fixture.Outbox)
            .Message.ShouldContain("WithMockedOutbox");
    }

    [Fact]
    public void Inbox_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _fixture = new EncinaTestFixture();
        _fixture.Build();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = _fixture.Inbox)
            .Message.ShouldContain("WithMockedInbox");
    }

    [Fact]
    public void SagaStore_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _fixture = new EncinaTestFixture();
        _fixture.Build();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = _fixture.SagaStore)
            .Message.ShouldContain("WithMockedSaga");
    }

    [Fact]
    public void WithService_ShouldRegisterService()
    {
        // Arrange
        var mockService = new MockService();
        _fixture = new EncinaTestFixture()
            .WithService<ITestService>(mockService);
        _fixture.Build();

        // Act
        var service = _fixture.GetService<ITestService>();

        // Assert
        service.ShouldBe(mockService);
    }

    [Fact]
    public void WithServiceGeneric_ShouldRegisterServiceWithImplementation()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithService<ITestService, MockService>();
        _fixture.Build();

        // Act
        var service = _fixture.GetService<ITestService>();

        // Assert
        service.ShouldNotBeNull();
        service.ShouldBeOfType<MockService>();
    }

    [Fact]
    public async Task ClearStores_ShouldResetAllStores()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithMockedInbox()
            .WithMockedSaga();
        _fixture.Build();

        // Simulate some store activity
        await _fixture.Outbox.AddAsync(new Fakes.Models.FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act
        _fixture.ClearStores();

        // Assert
        _fixture.Outbox.GetMessages().ShouldBeEmpty();
        _fixture.Outbox.GetAddedMessages().ShouldBeEmpty();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox();
        _fixture.Build();

        // Act & Assert
        Should.NotThrow(() => _fixture.Dispose());
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox();
        _fixture.Build();

        // Act & Assert
        Should.NotThrow(() =>
        {
            _fixture.Dispose();
            _fixture.Dispose();
        });
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox();
        _fixture.Build();

        // Act & Assert
        await Should.NotThrowAsync(async () => await _fixture.DisposeAsync());
    }

    [Fact]
    public void ServiceProvider_BeforeBuild_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _fixture = new EncinaTestFixture();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = _fixture.ServiceProvider)
            .Message.ShouldContain("SendAsync");
    }

    [Fact]
    public void GetRequiredService_BeforeBuild_ShouldAutoBuild()
    {
        // Arrange
        _fixture = new EncinaTestFixture();

        // Act - GetRequiredService should auto-build the fixture
        var encina = _fixture.GetRequiredService<IEncina>();

        // Assert
        encina.ShouldNotBeNull();
    }

    [Fact]
    public void FluentChaining_ShouldWorkCorrectly()
    {
        // Arrange & Act
        _fixture = new EncinaTestFixture()
            .WithMockedOutbox()
            .WithMockedInbox()
            .WithMockedSaga()
            .WithMockedScheduling()
            .WithMockedDeadLetter()
            .WithService<ITestService>(new MockService());

        // Assert - fluent chaining returns the same fixture
        _fixture.ShouldNotBeNull();
    }

    #region Time-Travel Testing

    [Fact]
    public void WithFakeTimeProvider_ShouldMakeTimeProviderAccessible()
    {
        // Arrange & Act
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider();
        _fixture.Build();

        // Assert
        _fixture.TimeProvider.ShouldNotBeNull();
        _fixture.TimeProvider.ShouldBeOfType<FakeTimeProvider>();
    }

    [Fact]
    public void WithFakeTimeProvider_WithStartTime_ShouldUseSpecifiedTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime);
        _fixture.Build();

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime);
    }

    [Fact]
    public void TimeProvider_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _fixture = new EncinaTestFixture();
        _fixture.Build();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = _fixture.TimeProvider)
            .Message.ShouldContain("WithFakeTimeProvider");
    }

    [Fact]
    public void AdvanceTimeBy_ShouldAdvanceTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime);
        _fixture.Build();

        // Act
        _fixture.AdvanceTimeBy(TimeSpan.FromHours(2));

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(2));
    }

    [Fact]
    public void AdvanceTimeByMinutes_ShouldAdvanceTimeByMinutes()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime);
        _fixture.Build();

        // Act
        _fixture.AdvanceTimeByMinutes(30);

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddMinutes(30));
    }

    [Fact]
    public void AdvanceTimeByHours_ShouldAdvanceTimeByHours()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime);
        _fixture.Build();

        // Act
        _fixture.AdvanceTimeByHours(5);

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(5));
    }

    [Fact]
    public void AdvanceTimeByDays_ShouldAdvanceTimeByDays()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime);
        _fixture.Build();

        // Act
        _fixture.AdvanceTimeByDays(7);

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddDays(7));
    }

    [Fact]
    public void SetTimeTo_ShouldSetTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var targetTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime);
        _fixture.Build();

        // Act
        _fixture.SetTimeTo(targetTime);

        // Assert
        _fixture.TimeProvider.GetUtcNow().ShouldBe(targetTime);
    }

    [Fact]
    public void GetCurrentTime_ShouldReturnCurrentFakeTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider(startTime);
        _fixture.Build();

        // Act
        var currentTime = _fixture.GetCurrentTime();

        // Assert
        currentTime.ShouldBe(startTime);
    }

    [Fact]
    public void AdvanceTimeBy_ShouldReturnFixtureForChaining()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider();
        _fixture.Build();

        // Act
        var result = _fixture.AdvanceTimeBy(TimeSpan.FromMinutes(10));

        // Assert
        result.ShouldBe(_fixture);
    }

    [Fact]
    public void TimeProvider_ShouldBeRegisteredInServiceProvider()
    {
        // Arrange
        _fixture = new EncinaTestFixture()
            .WithFakeTimeProvider();
        _fixture.Build();

        // Act
        var timeProvider = _fixture.GetService<TimeProvider>();

        // Assert
        timeProvider.ShouldNotBeNull();
        timeProvider.ShouldBe(_fixture.TimeProvider);
    }

    #endregion

    #region Test Helpers

    private interface ITestService
    {
        void DoSomething();
    }

    private sealed class MockService : ITestService
    {
        public void DoSomething() { }
    }

    #endregion
}
