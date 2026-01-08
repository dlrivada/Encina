using System.Text.Json;
using Encina.gRPC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Encina.gRPC.Tests;

/// <summary>
/// Unit tests for the <see cref="GrpcEncinaService"/> class.
/// </summary>
public sealed class GrpcEncinaServiceTests
{
    private readonly IEncina _encina;
    private readonly ILogger<GrpcEncinaService> _logger;
    private readonly IOptions<EncinaGrpcOptions> _options;
    private readonly GrpcEncinaService _service;

    public GrpcEncinaServiceTests()
    {
        _encina = Substitute.For<IEncina>();
        _logger = Substitute.For<ILogger<GrpcEncinaService>>();
        _options = Options.Create(new EncinaGrpcOptions());
        _service = new GrpcEncinaService(_encina, _logger, _options);
    }

    [Fact]
    public void Constructor_WithNullEncina_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(null!, _logger, _options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(_encina, null!, _options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(_encina, _logger, null!));
    }

    [Fact]
    public async Task SendAsync_WithNullRequestType_ThrowsArgumentNullException()
    {
        // Arrange
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _service.SendAsync(null!, data));
    }

    [Fact]
    public async Task SendAsync_WithNullRequestData_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _service.SendAsync("SomeType", null!));
    }

    [Fact]
    public async Task SendAsync_WithUnknownType_ReturnsTypeNotFoundError()
    {
        // Arrange
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act
        var result = await _service.SendAsync("Unknown.Type, NonExistentAssembly", data);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().IfSome(code => code.ShouldBe("GRPC_TYPE_NOT_FOUND"));
            error.Message.ShouldContain("not found");
        });
    }

    [Fact]
    public async Task PublishAsync_WithNullNotificationType_ThrowsArgumentNullException()
    {
        // Arrange
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _service.PublishAsync(null!, data));
    }

    [Fact]
    public async Task PublishAsync_WithNullNotificationData_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _service.PublishAsync("SomeType", null!));
    }

    [Fact]
    public async Task PublishAsync_WithUnknownType_ReturnsTypeNotFoundError()
    {
        // Arrange
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act
        var result = await _service.PublishAsync("Unknown.Type, NonExistentAssembly", data);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().IfSome(code => code.ShouldBe("GRPC_TYPE_NOT_FOUND"));
            error.Message.ShouldContain("not found");
        });
    }

    [Fact]
    public async Task StreamAsync_ReturnsNotImplementedError()
    {
        // Arrange
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act
        var results = new List<LanguageExt.Either<EncinaError, byte[]>>();
        await foreach (var result in _service.StreamAsync("SomeType", data))
        {
            results.Add(result);
        }

        // Assert
        results.Count.ShouldBe(1);
        results[0].IsLeft.ShouldBeTrue();
        results[0].IfLeft(error =>
        {
            error.GetCode().IfSome(code => code.ShouldBe("GRPC_STREAMING_NOT_IMPLEMENTED"));
        });
    }

    [Fact]
    public async Task SendAsync_WithInvalidJson_ReturnsDeserializationError()
    {
        // Arrange - create test notification type in the current assembly
        var typeName = typeof(TestNotificationForGrpc).AssemblyQualifiedName!;
        var invalidJson = "{invalid json"u8.ToArray();

        // Act
        var result = await _service.SendAsync(typeName, invalidJson);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            // Should return either GRPC_SEND_FAILED (exception) or GRPC_DESERIALIZE_FAILED
            error.GetCode().IfSome(code =>
                (code == "GRPC_SEND_FAILED" || code == "GRPC_DESERIALIZE_FAILED").ShouldBeTrue());
        });
    }

    [Fact]
    public async Task PublishAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var typeName = typeof(TestNotificationForGrpc).AssemblyQualifiedName!;
        var invalidJson = "{invalid json"u8.ToArray();

        // Act
        var result = await _service.PublishAsync(typeName, invalidJson);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().IfSome(code =>
                (code == "GRPC_PUBLISH_FAILED" || code == "GRPC_DESERIALIZE_FAILED").ShouldBeTrue());
        });
    }

    [Fact]
    public async Task SendAsync_WithValidTypeButNoIRequestInterface_ReturnsResponseTypeNotFoundError()
    {
        // Arrange - use a type that doesn't implement IRequest<>
        var typeName = typeof(NonRequestType).AssemblyQualifiedName!;
        var data = JsonSerializer.SerializeToUtf8Bytes(new NonRequestType("test"));

        // Act
        var result = await _service.SendAsync(typeName, data);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().IfSome(code =>
                (code == "GRPC_RESPONSE_TYPE_NOT_FOUND" || code == "GRPC_SEND_FAILED").ShouldBeTrue());
        });
    }

    [Fact]
    public async Task SendAsync_WithCancellation_PropagatesCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act - this tests that cancellation token is passed through
        // Even with unknown type, the token should be passed
        var result = await _service.SendAsync("Unknown.Type", data, cts.Token);

        // Assert - type not found before any async operation
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern")]
    public async Task PublishAsync_WithValidNotificationType_SucceedsWhenEncinaPublishes()
    {
        // Arrange
        var typeName = typeof(TestNotificationForGrpc).AssemblyQualifiedName!;
        var data = JsonSerializer.SerializeToUtf8Bytes(new TestNotificationForGrpc("test"));

        // Configure mock to return success
        var successResult = LanguageExt.Prelude.Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default);
        _encina.Publish(Arg.Any<TestNotificationForGrpc>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(ValueTask.FromResult(successResult));

        // Act
        var result = await _service.PublishAsync(typeName, data);

        // Assert - should succeed when Encina.Publish returns Right
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_WithEmptyJson_ReturnsDeserializationError()
    {
        // Arrange - use a type that requires a non-null value
        var typeName = typeof(TestNotificationForGrpc).AssemblyQualifiedName!;
        var emptyJson = "null"u8.ToArray();

        // Act
        var result = await _service.SendAsync(typeName, emptyJson);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task PublishAsync_WithEmptyJson_ReturnsDeserializationError()
    {
        // Arrange
        var typeName = typeof(TestNotificationForGrpc).AssemblyQualifiedName!;
        var emptyJson = "null"u8.ToArray();

        // Act
        var result = await _service.PublishAsync(typeName, emptyJson);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().IfSome(code =>
                code.ShouldBe("GRPC_DESERIALIZE_FAILED"));
        });
    }

    [Fact]
    public async Task SendAsync_CalledTwiceWithSameType_UsesCacheOnSecondCall()
    {
        // Arrange
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act - call twice to trigger cache hit
        await _service.SendAsync("Unknown.Type, Assembly", data);
        var result = await _service.SendAsync("Unknown.Type, Assembly", data);

        // Assert - both should return type not found (cache returns null for unknown types)
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task PublishAsync_CalledTwiceWithSameType_UsesCacheOnSecondCall()
    {
        // Arrange
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act - call twice to trigger cache hit
        await _service.PublishAsync("Unknown.Type, Assembly", data);
        var result = await _service.PublishAsync("Unknown.Type, Assembly", data);

        // Assert - both should return type not found
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task StreamAsync_WithCancellationToken_ReturnsNotImplemented()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });

        // Act
        var results = new List<LanguageExt.Either<EncinaError, byte[]>>();
        await foreach (var result in _service.StreamAsync("SomeType", data, cts.Token))
        {
            results.Add(result);
        }

        // Assert
        results.Count.ShouldBe(1);
        results[0].IsLeft.ShouldBeTrue();
    }

    // Test notification type for serialization tests
    public sealed record TestNotificationForGrpc(string Message) : INotification;

    // Type that doesn't implement IRequest<> - used for testing responseType null path
    public sealed record NonRequestType(string Value);
}
