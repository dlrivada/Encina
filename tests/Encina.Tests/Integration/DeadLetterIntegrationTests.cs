using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.Messaging.Recoverability;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.Tests.Integration;

/// <summary>
/// Integration tests for Dead Letter Queue functionality.
/// Tests end-to-end scenarios with real DI container and messaging pipeline.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DeadLetterIntegrationTests : IAsyncLifetime
{
    private readonly List<IDeadLetterMessage> _deadLetteredMessages = [];
    private ServiceProvider? _serviceProvider;
    private InMemoryDeadLetterStore? _store;

    public Task InitializeAsync()
    {
        _store = new InMemoryDeadLetterStore();

        var services = new ServiceCollection();
        services.AddEncina();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // Register DLQ services manually for testing
        var options = new DeadLetterOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7),
            OnDeadLetter = (message, _) =>
            {
                _deadLetteredMessages.Add(message);
                return Task.CompletedTask;
            }
        };
        services.AddSingleton(options);
        services.AddSingleton<IDeadLetterStore>(_store);
        services.AddSingleton<IDeadLetterMessageFactory, InMemoryDeadLetterMessageFactory>();
        services.AddScoped<DeadLetterOrchestrator>();
        services.AddScoped<IDeadLetterManager, DeadLetterManager>();

        services.AddTransient<IRequestHandler<FailingRequest, Unit>, FailingRequestHandler>();
        services.AddTransient<IRequestHandler<SuccessfulRequest, string>, SuccessfulRequestHandler>();

        _serviceProvider = services.BuildServiceProvider();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    #region Orchestrator Integration Tests

    [Fact]
    public async Task Orchestrator_AddAsync_ShouldStoreMessageAndInvokeCallback()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var request = new FailingRequest { Value = 42 };
        var error = EncinaError.New("[test] Test error");

        // Act
        var message = await orchestrator.AddAsync(
            request,
            error,
            exception: null,
            sourcePattern: DeadLetterSourcePatterns.Recoverability,
            totalRetryAttempts: 3,
            firstFailedAtUtc: DateTime.UtcNow.AddMinutes(-5),
            correlationId: "test-correlation");

        // Assert
        message.ShouldNotBeNull();
        message.RequestType.ShouldContain(nameof(FailingRequest));
        message.ErrorMessage.ShouldBe("[test] Test error");
        message.SourcePattern.ShouldBe(DeadLetterSourcePatterns.Recoverability);
        message.TotalRetryAttempts.ShouldBe(3);
        message.CorrelationId.ShouldBe("test-correlation");

        // Verify callback was invoked
        _deadLetteredMessages.ShouldHaveSingleItem();
        _deadLetteredMessages[0].Id.ShouldBe(message.Id);
    }

    [Fact]
    public async Task Orchestrator_AddAsync_WithException_ShouldCaptureExceptionDetails()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var request = new FailingRequest { Value = 42 };
        var error = EncinaError.New("[test] Exception occurred");
        var exception = new InvalidOperationException("Test exception message");

        // Act
        var message = await orchestrator.AddAsync(
            request,
            error,
            exception,
            sourcePattern: DeadLetterSourcePatterns.Outbox,
            totalRetryAttempts: 1,
            firstFailedAtUtc: DateTime.UtcNow);

        // Assert
        message.ExceptionType.ShouldBe("System.InvalidOperationException");
        message.ExceptionMessage.ShouldBe("Test exception message");
        message.ExceptionStackTrace.ShouldBeNull(); // No stack trace when exception is created inline
    }

    [Fact]
    public async Task Orchestrator_GetPendingCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var error = EncinaError.New("[test] Error");

        // Add multiple messages
        await orchestrator.AddAsync(
            new FailingRequest { Value = 1 }, error, null,
            DeadLetterSourcePatterns.Recoverability, 1, DateTime.UtcNow);
        await orchestrator.AddAsync(
            new FailingRequest { Value = 2 }, error, null,
            DeadLetterSourcePatterns.Outbox, 1, DateTime.UtcNow);

        // Act
        var count = await orchestrator.GetPendingCountAsync();

        // Assert
        count.ShouldBe(2);
    }

    [Fact]
    public async Task Orchestrator_GetStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var error = EncinaError.New("[test] Error");

        await orchestrator.AddAsync(
            new FailingRequest { Value = 1 }, error, null,
            DeadLetterSourcePatterns.Recoverability, 1, DateTime.UtcNow);
        await orchestrator.AddAsync(
            new FailingRequest { Value = 2 }, error, null,
            DeadLetterSourcePatterns.Recoverability, 2, DateTime.UtcNow);
        await orchestrator.AddAsync(
            new FailingRequest { Value = 3 }, error, null,
            DeadLetterSourcePatterns.Outbox, 1, DateTime.UtcNow);

        // Act
        var stats = await orchestrator.GetStatisticsAsync();

        // Assert
        stats.TotalCount.ShouldBe(3);
        stats.PendingCount.ShouldBe(3);
        stats.CountBySource.ShouldContainKey(DeadLetterSourcePatterns.Recoverability);
        stats.CountBySource[DeadLetterSourcePatterns.Recoverability].ShouldBe(2);
        stats.CountBySource.ShouldContainKey(DeadLetterSourcePatterns.Outbox);
        stats.CountBySource[DeadLetterSourcePatterns.Outbox].ShouldBe(1);
    }

    #endregion

    #region Manager Integration Tests

    [Fact]
    public async Task Manager_GetMessagesAsync_ShouldReturnFilteredMessages()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var manager = _serviceProvider!.GetRequiredService<IDeadLetterManager>();
        var error = EncinaError.New("[test] Error");

        await orchestrator.AddAsync(
            new FailingRequest { Value = 1 }, error, null,
            DeadLetterSourcePatterns.Recoverability, 1, DateTime.UtcNow);
        await orchestrator.AddAsync(
            new FailingRequest { Value = 2 }, error, null,
            DeadLetterSourcePatterns.Outbox, 1, DateTime.UtcNow);

        // Act
        var allMessages = await manager.GetMessagesAsync();
        var recoverabilityMessages = await manager.GetMessagesAsync(
            DeadLetterFilter.FromSource(DeadLetterSourcePatterns.Recoverability));

        // Assert
        allMessages.Count().ShouldBe(2);
        recoverabilityMessages.Count().ShouldBe(1);
        recoverabilityMessages.First().SourcePattern.ShouldBe(DeadLetterSourcePatterns.Recoverability);
    }

    [Fact]
    public async Task Manager_DeleteAsync_ShouldRemoveMessage()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var manager = _serviceProvider!.GetRequiredService<IDeadLetterManager>();
        var error = EncinaError.New("[test] Error");

        var message = await orchestrator.AddAsync(
            new FailingRequest { Value = 1 }, error, null,
            DeadLetterSourcePatterns.Recoverability, 1, DateTime.UtcNow);

        // Act
        var deleted = await manager.DeleteAsync(message.Id);
        var retrieved = await manager.GetMessageAsync(message.Id);

        // Assert
        deleted.ShouldBeTrue();
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task Manager_DeleteAllAsync_ShouldDeleteMatchingMessages()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var manager = _serviceProvider!.GetRequiredService<IDeadLetterManager>();
        var error = EncinaError.New("[test] Error");

        await orchestrator.AddAsync(
            new FailingRequest { Value = 1 }, error, null,
            DeadLetterSourcePatterns.Recoverability, 1, DateTime.UtcNow);
        await orchestrator.AddAsync(
            new FailingRequest { Value = 2 }, error, null,
            DeadLetterSourcePatterns.Outbox, 1, DateTime.UtcNow);

        // Act
        var deletedCount = await manager.DeleteAllAsync(
            DeadLetterFilter.FromSource(DeadLetterSourcePatterns.Recoverability));
        var remainingCount = await manager.GetCountAsync();

        // Assert
        deletedCount.ShouldBe(1);
        remainingCount.ShouldBe(1);
    }

    #endregion

    #region Health Check Integration Tests

    [Fact]
    public async Task HealthCheck_WithNoPendingMessages_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck = new DeadLetterHealthCheck(_store!);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("empty");
    }

    [Fact]
    public async Task HealthCheck_WithMessagesBelowThreshold_ShouldReturnHealthy()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var error = EncinaError.New("[test] Error");

        // Add 5 messages (below default warning threshold of 10)
        for (int i = 0; i < 5; i++)
        {
            await orchestrator.AddAsync(
                new FailingRequest { Value = i }, error, null,
                DeadLetterSourcePatterns.Recoverability, 1, DateTime.UtcNow);
        }

        var healthCheck = new DeadLetterHealthCheck(_store!);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("5");
    }

    [Fact]
    public async Task HealthCheck_WithMessagesAboveWarningThreshold_ShouldReturnDegraded()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var error = EncinaError.New("[test] Error");

        // Add 15 messages (above default warning threshold of 10)
        for (int i = 0; i < 15; i++)
        {
            await orchestrator.AddAsync(
                new FailingRequest { Value = i }, error, null,
                DeadLetterSourcePatterns.Recoverability, 1, DateTime.UtcNow);
        }

        var healthCheck = new DeadLetterHealthCheck(_store!);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("15");
    }

    [Fact]
    public async Task HealthCheck_WithMessagesAboveCriticalThreshold_ShouldReturnUnhealthy()
    {
        // Arrange
        var orchestrator = _serviceProvider!.GetRequiredService<DeadLetterOrchestrator>();
        var error = EncinaError.New("[test] Error");

        // Add 150 messages (above default critical threshold of 100)
        for (int i = 0; i < 150; i++)
        {
            await orchestrator.AddAsync(
                new FailingRequest { Value = i }, error, null,
                DeadLetterSourcePatterns.Recoverability, 1, DateTime.UtcNow);
        }

        var healthCheck = new DeadLetterHealthCheck(_store!);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("150");
    }

    #endregion

    #region Test Helpers

    private sealed record FailingRequest : IRequest<Unit>
    {
        public int Value { get; init; }
    }

    private sealed record SuccessfulRequest : IRequest<string>
    {
        public string Input { get; init; } = "";
    }

    private sealed class FailingRequestHandler : IRequestHandler<FailingRequest, Unit>
    {
        public Task<Either<EncinaError, Unit>> Handle(FailingRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Left<EncinaError, Unit>(EncinaError.New($"[test] Failed for value {request.Value}")));
        }
    }

    private sealed class SuccessfulRequestHandler : IRequestHandler<SuccessfulRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(SuccessfulRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>($"Success: {request.Input}"));
        }
    }

    /// <summary>
    /// In-memory implementation of IDeadLetterStore for testing.
    /// </summary>
    private sealed class InMemoryDeadLetterStore : IDeadLetterStore
    {
        private readonly List<InMemoryDeadLetterMessage> _messages = [];

        public Task AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
        {
            if (message is InMemoryDeadLetterMessage inMemoryMessage)
            {
                _messages.Add(inMemoryMessage);
            }
            return Task.CompletedTask;
        }

        public Task<IDeadLetterMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var message = _messages.FirstOrDefault(m => m.Id == messageId);
            return Task.FromResult<IDeadLetterMessage?>(message);
        }

        public Task<IEnumerable<IDeadLetterMessage>> GetMessagesAsync(
            DeadLetterFilter? filter = null,
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<IDeadLetterMessage> query = _messages;

            if (filter != null)
            {
                if (filter.SourcePattern != null)
                    query = query.Where(m => m.SourcePattern == filter.SourcePattern);

                if (filter.RequestType != null)
                    query = query.Where(m => m.RequestType == filter.RequestType);

                if (filter.CorrelationId != null)
                    query = query.Where(m => m.CorrelationId == filter.CorrelationId);

                if (filter.DeadLetteredAfterUtc.HasValue)
                    query = query.Where(m => m.DeadLetteredAtUtc >= filter.DeadLetteredAfterUtc.Value);

                if (filter.DeadLetteredBeforeUtc.HasValue)
                    query = query.Where(m => m.DeadLetteredAtUtc <= filter.DeadLetteredBeforeUtc.Value);

                if (filter.ExcludeReplayed == true)
                    query = query.Where(m => !m.IsReplayed);
            }

            return Task.FromResult(query.OrderBy(m => m.DeadLetteredAtUtc).Skip(skip).Take(take));
        }

        public Task<int> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<IDeadLetterMessage> query = _messages;

            if (filter != null)
            {
                if (filter.SourcePattern != null)
                    query = query.Where(m => m.SourcePattern == filter.SourcePattern);

                if (filter.ExcludeReplayed == true)
                    query = query.Where(m => !m.IsReplayed);
            }

            return Task.FromResult(query.Count());
        }

        public Task MarkAsReplayedAsync(Guid messageId, string result, CancellationToken cancellationToken = default)
        {
            var message = _messages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                message.ReplayedAtUtc = DateTime.UtcNow;
                message.ReplayResult = result;
            }
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var message = _messages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                _messages.Remove(message);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expired = _messages.Where(m => m.ExpiresAtUtc.HasValue && m.ExpiresAtUtc <= now).ToList();
            foreach (var message in expired)
            {
                _messages.Remove(message);
            }
            return Task.FromResult(expired.Count);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// In-memory implementation of IDeadLetterMessage for testing.
    /// </summary>
    private sealed class InMemoryDeadLetterMessage : IDeadLetterMessage
    {
        public Guid Id { get; set; }
        public string RequestType { get; set; } = "";
        public string RequestContent { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionStackTrace { get; set; }
        public string? CorrelationId { get; set; }
        public string SourcePattern { get; set; } = "";
        public int TotalRetryAttempts { get; set; }
        public DateTime FirstFailedAtUtc { get; set; }
        public DateTime DeadLetteredAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public DateTime? ReplayedAtUtc { get; set; }
        public string? ReplayResult { get; set; }

        public bool IsReplayed => ReplayedAtUtc.HasValue;
        public bool IsExpired => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// In-memory implementation of IDeadLetterMessageFactory for testing.
    /// </summary>
    private sealed class InMemoryDeadLetterMessageFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(
            Guid id,
            string requestType,
            string requestContent,
            string errorMessage,
            string sourcePattern,
            int totalRetryAttempts,
            DateTime firstFailedAtUtc,
            DateTime deadLetteredAtUtc,
            DateTime? expiresAtUtc,
            string? correlationId,
            string? exceptionType,
            string? exceptionMessage,
            string? exceptionStackTrace)
        {
            return new InMemoryDeadLetterMessage
            {
                Id = id,
                RequestType = requestType,
                RequestContent = requestContent,
                ErrorMessage = errorMessage,
                SourcePattern = sourcePattern,
                TotalRetryAttempts = totalRetryAttempts,
                FirstFailedAtUtc = firstFailedAtUtc,
                DeadLetteredAtUtc = deadLetteredAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                CorrelationId = correlationId,
                ExceptionType = exceptionType,
                ExceptionMessage = exceptionMessage,
                ExceptionStackTrace = exceptionStackTrace
            };
        }

        public IDeadLetterMessage CreateFromFailedMessage(
            FailedMessage failedMessage,
            string sourcePattern,
            DateTime? expiresAtUtc)
        {
            return new InMemoryDeadLetterMessage
            {
                Id = Guid.NewGuid(),
                RequestType = failedMessage.RequestType,
                RequestContent = System.Text.Json.JsonSerializer.Serialize(failedMessage.Request),
                ErrorMessage = failedMessage.Error.Message,
                SourcePattern = sourcePattern,
                TotalRetryAttempts = failedMessage.TotalAttempts,
                FirstFailedAtUtc = failedMessage.FirstAttemptAtUtc,
                DeadLetteredAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expiresAtUtc,
                CorrelationId = failedMessage.CorrelationId,
                ExceptionType = failedMessage.Exception?.GetType().FullName,
                ExceptionMessage = failedMessage.Exception?.Message,
                ExceptionStackTrace = failedMessage.Exception?.StackTrace
            };
        }
    }

    #endregion
}
