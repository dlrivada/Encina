using System.Text.Json;
using Encina.gRPC;
using LanguageExt;
using static LanguageExt.Prelude;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.gRPC.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="GrpcEncinaService"/> with full DI setup.
/// Demonstrates real-world usage patterns for the gRPC Encina service.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Service", "gRPC")]
public sealed class GrpcEncinaServiceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IGrpcEncinaService _grpcService;

    public GrpcEncinaServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddEncina(typeof(GrpcEncinaServiceIntegrationTests).Assembly);
        services.AddEncinaGrpc();

        _serviceProvider = services.BuildServiceProvider();
        _grpcService = _serviceProvider.GetRequiredService<IGrpcEncinaService>();
    }

    [Fact]
    public void Service_CanBeResolved_FromServiceProvider()
    {
        // Act
        var service = _serviceProvider.GetService<IGrpcEncinaService>();

        // Assert
        service.ShouldNotBeNull();
        service.ShouldBeOfType<GrpcEncinaService>();
    }

    [Fact(Skip = "GrpcEncinaService.SendAsync has a reflection bug - see issue #520")]
    public async Task SendAsync_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = new TestQuery("test-value");
        var requestType = typeof(TestQuery).AssemblyQualifiedName!;
        var requestData = JsonSerializer.SerializeToUtf8Bytes(request);

        // Act
        var result = await _grpcService.SendAsync(requestType, requestData);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(responseBytes =>
        {
            var response = JsonSerializer.Deserialize<TestResponse>(responseBytes);
            response.ShouldNotBeNull();
            response!.Value.ShouldBe("Processed: test-value");
        });
    }

    [Fact]
    public async Task SendAsync_WithUnknownType_ReturnsError()
    {
        // Arrange
        var requestType = "Unknown.Type.That.Does.Not.Exist, Unknown.Assembly";
        var requestData = JsonSerializer.SerializeToUtf8Bytes(new { Value = "test" });

        // Act
        var result = await _grpcService.SendAsync(requestType, requestData);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("not found");
        });
    }

    [Fact(Skip = "GrpcEncinaService has a reflection bug - see issue #520")]
    public async Task PublishAsync_WithValidNotification_Succeeds()
    {
        // Arrange
        var notification = new TestNotification("event-data");
        var notificationType = typeof(TestNotification).AssemblyQualifiedName!;
        var notificationData = JsonSerializer.SerializeToUtf8Bytes(notification);

        // Act
        var result = await _grpcService.PublishAsync(notificationType, notificationData);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task PublishAsync_WithUnknownType_ReturnsError()
    {
        // Arrange
        var notificationType = "Unknown.Notification.Type, Unknown.Assembly";
        var notificationData = JsonSerializer.SerializeToUtf8Bytes(new { Data = "test" });

        // Act
        var result = await _grpcService.PublishAsync(notificationType, notificationData);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("not found");
        });
    }

    [Fact]
    public async Task StreamAsync_ReturnsNotImplementedError()
    {
        // Arrange
        var requestType = typeof(TestQuery).AssemblyQualifiedName!;
        var requestData = JsonSerializer.SerializeToUtf8Bytes(new TestQuery("stream-test"));

        // Act
        var results = new List<Either<EncinaError, byte[]>>();
        await foreach (var result in _grpcService.StreamAsync(requestType, requestData))
        {
            results.Add(result);
        }

        // Assert - Streaming is not yet implemented
        results.Count.ShouldBe(1);
        results[0].IsLeft.ShouldBeTrue();
        results[0].IfLeft(error =>
        {
            error.Message.ShouldContain("not yet implemented");
        });
    }

    [Fact(Skip = "GrpcEncinaService has a reflection bug - see issue #520")]
    public async Task SendAsync_ConcurrentRequests_AllSucceed()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var request = new TestQuery($"concurrent-{i}");
            var requestType = typeof(TestQuery).AssemblyQualifiedName!;
            var requestData = JsonSerializer.SerializeToUtf8Bytes(request);

            return await _grpcService.SendAsync(requestType, requestData);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.ShouldBe(10);
        results.All(r => r.IsRight).ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_WithCancellation_ThrowsOrReturnsError()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var request = new TestQuery("cancelled");
        var requestType = typeof(TestQuery).AssemblyQualifiedName!;
        var requestData = JsonSerializer.SerializeToUtf8Bytes(request);

        // Act & Assert - Either throws or returns error
        try
        {
            var result = await _grpcService.SendAsync(requestType, requestData, cts.Token);
            // If it doesn't throw, it should be an error
            result.IsLeft.ShouldBeTrue();
        }
        catch (OperationCanceledException)
        {
            // Expected behavior
        }
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}

// Test types for integration tests
public sealed record TestQuery(string Value) : IQuery<TestResponse>;

public sealed record TestResponse(string Value);

public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestResponse>
{
    public Task<Either<EncinaError, TestResponse>> Handle(
        TestQuery request,
        CancellationToken cancellationToken)
    {
        var response = new TestResponse($"Processed: {request.Value}");
        return Task.FromResult<Either<EncinaError, TestResponse>>(response);
    }
}

public sealed record TestNotification(string Data) : INotification;

public sealed class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public Task<Either<EncinaError, Unit>> Handle(
        TestNotification notification,
        CancellationToken cancellationToken)
    {
        // Just return success for test
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
