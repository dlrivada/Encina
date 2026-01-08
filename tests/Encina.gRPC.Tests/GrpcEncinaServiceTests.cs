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
    private readonly ITypeResolver _typeResolver;
    private readonly IOptions<EncinaGrpcOptions> _options;
    private readonly GrpcEncinaService _service;

    public GrpcEncinaServiceTests()
    {
        _encina = Substitute.For<IEncina>();
        _logger = Substitute.For<ILogger<GrpcEncinaService>>();
        _typeResolver = Substitute.For<ITypeResolver>();
        _options = Options.Create(new EncinaGrpcOptions());
        _service = new GrpcEncinaService(_encina, _logger, _typeResolver, _options);
    }

    [Fact]
    public void Constructor_WithNullEncina_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(null!, _logger, _typeResolver, _options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(_encina, null!, _typeResolver, _options));
    }

    [Fact]
    public void Constructor_WithNullTypeResolver_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(_encina, _logger, null!, _options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(_encina, _logger, _typeResolver, null!));
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
        _typeResolver.ResolveRequestType(typeName).Returns(typeof(TestNotificationForGrpc));

        // Act
        var result = await _service.SendAsync(typeName, invalidJson);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            // Deserialization is performed before send attempt, so malformed JSON always returns GRPC_DESERIALIZE_FAILED
            error.GetCode().IfSome(code => code.ShouldBe("GRPC_DESERIALIZE_FAILED"));
        });
    }

    [Fact]
    public async Task PublishAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var typeName = typeof(TestNotificationForGrpc).AssemblyQualifiedName!;
        var invalidJson = "{invalid json"u8.ToArray();
        _typeResolver.ResolveNotificationType(typeName).Returns(typeof(TestNotificationForGrpc));

        // Act
        var result = await _service.PublishAsync(typeName, invalidJson);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            // Deserialization is performed before publish attempt, so malformed JSON always returns GRPC_DESERIALIZE_FAILED
            error.GetCode().IfSome(code => code.ShouldBe("GRPC_DESERIALIZE_FAILED"));
        });
    }

    [Fact]
    public async Task SendAsync_WithValidTypeButNoIRequestInterface_ReturnsResponseTypeNotFoundError()
    {
        // Arrange - use a type that doesn't implement IRequest<>
        var typeName = typeof(NonRequestType).AssemblyQualifiedName!;
        var data = JsonSerializer.SerializeToUtf8Bytes(new NonRequestType("test"));
        _typeResolver.ResolveRequestType(typeName).Returns(typeof(NonRequestType));

        // Act
        var result = await _service.SendAsync(typeName, data);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            // Type check for IRequest<> interface happens before any send attempt,
            // so NonRequestType (which doesn't implement IRequest<>) deterministically returns this error
            error.GetCode().IfSome(code => code.ShouldBe("GRPC_RESPONSE_TYPE_NOT_FOUND"));
        });
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern")]
    public async Task SendAsync_WithCancellation_PropagatesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var typeName = typeof(TestCommandForGrpc).AssemblyQualifiedName!;
        var data = JsonSerializer.SerializeToUtf8Bytes(new TestCommandForGrpc("test"));
        _typeResolver.ResolveRequestType(typeName).Returns(typeof(TestCommandForGrpc));

        _encina.Send(
                Arg.Any<TestCommandForGrpc>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                LanguageExt.Prelude.Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default)));

        // Act
        await _service.SendAsync(typeName, data, cts.Token);

        // Assert - verify the cancellation token was passed through to Encina
        await _encina.Received(1).Send(
            Arg.Any<TestCommandForGrpc>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern")]
    public async Task PublishAsync_WithValidNotificationType_SucceedsWhenEncinaPublishes()
    {
        // Arrange
        var typeName = typeof(TestNotificationForGrpc).AssemblyQualifiedName!;
        var data = JsonSerializer.SerializeToUtf8Bytes(new TestNotificationForGrpc("test"));
        _typeResolver.ResolveNotificationType(typeName).Returns(typeof(TestNotificationForGrpc));

        // Configure mock to return success only when deserialized notification has expected Message
        var successResult = LanguageExt.Prelude.Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default);
        _encina.Publish(
                Arg.Is<TestNotificationForGrpc>(n => n.Message == "test"),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(successResult));

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
        _typeResolver.ResolveRequestType(typeName).Returns(typeof(TestNotificationForGrpc));

        // Act
        var result = await _service.SendAsync(typeName, emptyJson);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().IfSome(code =>
                code.ShouldBe("GRPC_DESERIALIZE_FAILED"));
        });
    }

    [Fact]
    public async Task PublishAsync_WithEmptyJson_ReturnsDeserializationError()
    {
        // Arrange
        var typeName = typeof(TestNotificationForGrpc).AssemblyQualifiedName!;
        var emptyJson = "null"u8.ToArray();
        _typeResolver.ResolveNotificationType(typeName).Returns(typeof(TestNotificationForGrpc));

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
    public async Task SendAsync_CalledTwiceWithSameType_CallsTypeResolverEachTime()
    {
        // Arrange
        const string typeName = "Unknown.Type, Assembly";
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });
        _typeResolver.ResolveRequestType(typeName).Returns((Type?)null);

        // Act - call twice
        await _service.SendAsync(typeName, data);
        await _service.SendAsync(typeName, data);

        // Assert - type resolver is called for each SendAsync call
        // (caching is the responsibility of ITypeResolver implementation)
        _typeResolver.Received(2).ResolveRequestType(typeName);
    }

    [Fact]
    public async Task PublishAsync_CalledTwiceWithSameType_CallsTypeResolverEachTime()
    {
        // Arrange
        const string typeName = "Unknown.Type, Assembly";
        var data = JsonSerializer.SerializeToUtf8Bytes(new { });
        _typeResolver.ResolveNotificationType(typeName).Returns((Type?)null);

        // Act - call twice
        await _service.PublishAsync(typeName, data);
        await _service.PublishAsync(typeName, data);

        // Assert - type resolver is called for each PublishAsync call
        // (caching is the responsibility of ITypeResolver implementation)
        _typeResolver.Received(2).ResolveNotificationType(typeName);
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

    // Test command type for Send tests
    public sealed record TestCommandForGrpc(string Value) : ICommand<LanguageExt.Unit>;

    // Type that doesn't implement IRequest<> - used for testing responseType null path
    public sealed record NonRequestType(string Value);
}
