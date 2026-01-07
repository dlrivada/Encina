using Encina.Testing;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.NATS.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="NATSMessagePublisher"/> using Testcontainers.
/// </summary>
[Collection(NatsCollection.Name)]
[Trait("Category", "Integration")]
public sealed class NATSMessagePublisherIntegrationTests : IAsyncLifetime
{
    private readonly NatsFixture _fixture;
    private readonly ILogger<NATSMessagePublisher> _logger;

    public NATSMessagePublisherIntegrationTests(NatsFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<NATSMessagePublisher>>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    #region PublishAsync Integration Tests

    [SkippableFact]
    public async Task PublishAsync_WithRealNATS_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "NATS is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaNATSOptions
        {
            SubjectPrefix = "test"
        });
        var publisher = new NATSMessagePublisher(_fixture.Connection!, null, _logger, options);
        var message = new TestMessage { Id = 1, Content = "Integration Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishAsync_WithCustomSubject_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "NATS is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaNATSOptions
        {
            SubjectPrefix = "test"
        });
        var publisher = new NATSMessagePublisher(_fixture.Connection!, null, _logger, options);
        var message = new TestMessage { Id = 2, Content = "Custom Subject Test" };
        var customSubject = "custom.subject.test";

        // Act
        var result = await publisher.PublishAsync(message, customSubject);

        // Assert
        result.ShouldBeSuccess();
    }

    [SkippableFact]
    public async Task PublishAsync_MultipleMessages_ShouldAllSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "NATS is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaNATSOptions
        {
            SubjectPrefix = "test"
        });
        var publisher = new NATSMessagePublisher(_fixture.Connection!, null, _logger, options);

        // Act & Assert
        for (var i = 0; i < 10; i++)
        {
            var message = new TestMessage { Id = i, Content = $"Message {i}" };
            var result = await publisher.PublishAsync(message);
            result.ShouldBeSuccess();
        }
    }

    #endregion

    #region RequestAsync Integration Tests

    [SkippableFact]
    public async Task RequestAsync_WhenNoResponder_ShouldReturnError()
    {
        Skip.IfNot(_fixture.IsAvailable, "NATS is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaNATSOptions
        {
            SubjectPrefix = "test"
        });
        var publisher = new NATSMessagePublisher(_fixture.Connection!, null, _logger, options);
        var request = new TestMessage { Id = 1, Content = "Request without responder" };

        // Act - Use a very short timeout since there's no responder
        var result = await publisher.RequestAsync<TestMessage, TestMessage>(
            request,
            subject: "test.no-responder",
            timeout: TimeSpan.FromMilliseconds(100));

        // Assert - Should return an error (either timeout or request failed)
        result.ShouldBeError();
        // The NATS client may throw a generic exception instead of OperationCanceledException
        // so we accept either error code
        result.IfLeft(error =>
        {
            var errorCode = error.GetCode().IfNone(string.Empty);
            var isValidErrorCode = errorCode == "NATS_REQUEST_TIMEOUT" || errorCode == "NATS_REQUEST_FAILED";
            isValidErrorCode.ShouldBeTrue($"Expected NATS_REQUEST_TIMEOUT or NATS_REQUEST_FAILED but got {errorCode}");
        });
    }

    #endregion

    #region JetStreamPublishAsync Integration Tests

    [SkippableFact]
    public async Task JetStreamPublishAsync_WithoutJetStream_ShouldReturnError()
    {
        Skip.IfNot(_fixture.IsAvailable, "NATS is not available (Docker may not be running)");

        // Arrange
        var options = Options.Create(new EncinaNATSOptions
        {
            SubjectPrefix = "test"
        });
        // No JetStream context provided
        var publisher = new NATSMessagePublisher(_fixture.Connection!, null, _logger, options);
        var message = new TestMessage { Id = 100, Content = "JetStream Test" };

        // Act
        var result = await publisher.JetStreamPublishAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("NATS_JETSTREAM_NOT_ENABLED");
    }

    #endregion

    #region Test Types

    private sealed record TestMessage
    {
        public int Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }

    #endregion
}
