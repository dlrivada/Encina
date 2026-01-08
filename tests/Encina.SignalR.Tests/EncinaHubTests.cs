using System.Text.Json;
using Encina.SignalR;
using LanguageExt;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.SignalR.Tests;

/// <summary>
/// Tests for the <see cref="EncinaHub"/> class.
/// </summary>
public sealed class EncinaHubTests
{
    /// <summary>
    /// Creates a new test hub with fresh mocks for each test.
    /// Each call returns an isolated hub with its own IEncina, IOptions, and ILogger instances.
    /// </summary>
    private static TestEncinaHub CreateHub(SignalROptions? options = null)
    {
        var encina = Substitute.For<IEncina>();
        var opts = Microsoft.Extensions.Options.Options.Create(options ?? new SignalROptions());
        var logger = Substitute.For<ILogger<TestEncinaHub>>();
        var hub = new TestEncinaHub(encina, opts, logger);
        hub.Context = CreateMockHubContext();
        return hub;
    }

    [Fact]
    public void Constructor_SetsEncinaProperty()
    {
        // Act
        using var hub = CreateHub();

        // Assert
        Assert.NotNull(hub.GetEncina());
    }

    [Fact]
    public async Task SendCommand_WithUnknownType_ReturnsTypeNotFoundError()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement(new { Value = "test" });

        // Act
        var result = await hub.SendCommand("NonExistent.Command", json);

        // Assert
        Assert.NotNull(result);
        var response = ParseHubResponse(result);
        AssertIsFailedResponse(response);
        AssertHasErrorCode(response, "command.type_not_found");
    }

    [Fact]
    public async Task SendQuery_WithUnknownType_ReturnsTypeNotFoundError()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement(new { Value = "test" });

        // Act
        var result = await hub.SendQuery("NonExistent.Query", json);

        // Assert
        Assert.NotNull(result);
        var response = ParseHubResponse(result);
        AssertIsFailedResponse(response);
        AssertHasErrorCode(response, "query.type_not_found");
    }

    [Fact]
    public async Task PublishNotification_WithUnknownType_DoesNotThrow()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement(new { Value = "test" });

        // Act
        var exception = await Record.ExceptionAsync(() =>
            hub.PublishNotification("NonExistent.Notification", json));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendCommand_WhenExceptionOccurs_ReturnsErrorResponse()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement(new { Message = "test" });
        var typeName = typeof(TestCommand).AssemblyQualifiedName!;

        // The Hub uses dynamic dispatch which will cause the Encina.Send to throw
        // because our mock doesn't handle the specific command type

        // Act
        var result = await hub.SendCommand(typeName, json);

        // Assert
        Assert.NotNull(result);
        // The result should be an error response (either exception or failed to process)
        var response = ParseHubResponse(result);
        AssertIsFailedResponse(response);
    }

    [Fact]
    public async Task SendQuery_WhenExceptionOccurs_ReturnsErrorResponse()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement(new { Query = "test" });
        var typeName = typeof(TestQuery).AssemblyQualifiedName!;

        // Act
        var result = await hub.SendQuery(typeName, json);

        // Assert
        Assert.NotNull(result);
        var response = ParseHubResponse(result);
        AssertIsFailedResponse(response);
    }

    [Fact]
    public async Task PublishNotification_WhenExceptionOccurs_DoesNotThrow()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement(new { Message = "test" });
        var typeName = typeof(TestHubNotification).AssemblyQualifiedName!;

        // Act
        var exception = await Record.ExceptionAsync(() =>
            hub.PublishNotification(typeName, json));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ResolveType_WithExactTypeName_ReturnsType()
    {
        // Arrange
        using var hub = CreateHub();
        var typeName = typeof(TestCommand).AssemblyQualifiedName!;

        // Act
        var type = hub.TestResolveType(typeName);

        // Assert
        Assert.Equal(typeof(TestCommand), type);
    }

    [Fact]
    public void ResolveType_WithSimpleName_ReturnsType()
    {
        // Arrange
        using var hub = CreateHub();
        var typeName = nameof(TestCommand);

        // Act
        var type = hub.TestResolveType(typeName);

        // Assert
        Assert.Equal(typeof(TestCommand), type);
    }

    [Fact]
    public void ResolveType_WithNonExistentType_ReturnsNull()
    {
        // Arrange
        using var hub = CreateHub();
        var typeName = "NonExistent.Type.That.Does.Not.Exist";

        // Act
        var type = hub.TestResolveType(typeName);

        // Assert
        Assert.Null(type);
    }

    [Fact]
    public async Task SendCommand_WithDetailedErrorsEnabled_IncludesExceptionDetails()
    {
        // Arrange
        using var hub = CreateHub(new SignalROptions { IncludeDetailedErrors = true });
        var json = JsonSerializer.SerializeToElement(new { Value = "test" });
        var typeName = "NonExistent.CommandType"; // This will trigger type_not_found error

        // Act
        var result = await hub.SendCommand(typeName, json);

        // Assert - Verify detailed error message is included
        Assert.NotNull(result);
        var response = ParseHubResponse(result);
        AssertIsFailedResponse(response);
        AssertHasErrorCode(response, "command.type_not_found");
        AssertErrorMessageContains(response, typeName);
    }

    [Fact]
    public async Task SendQuery_WithDetailedErrorsEnabled_IncludesExceptionDetails()
    {
        // Arrange
        using var hub = CreateHub(new SignalROptions { IncludeDetailedErrors = true });
        var json = JsonSerializer.SerializeToElement(new { Value = "test" });
        var typeName = "NonExistent.QueryType"; // This will trigger type_not_found error

        // Act
        var result = await hub.SendQuery(typeName, json);

        // Assert - Verify detailed error message is included
        Assert.NotNull(result);
        var response = ParseHubResponse(result);
        AssertIsFailedResponse(response);
        AssertHasErrorCode(response, "query.type_not_found");
        AssertErrorMessageContains(response, typeName);
    }

    [Fact]
    public async Task SendCommand_WithNullDeserialization_ReturnsDeserializationError()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement<object?>(null);
        var typeName = typeof(TestCommand).AssemblyQualifiedName!;

        // Act
        var result = await hub.SendCommand(typeName, json);

        // Assert
        Assert.NotNull(result);
        var response = ParseHubResponse(result);
        AssertIsFailedResponse(response);
    }

    [Fact]
    public async Task SendQuery_WithNullDeserialization_ReturnsDeserializationError()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement<object?>(null);
        var typeName = typeof(TestQuery).AssemblyQualifiedName!;

        // Act
        var result = await hub.SendQuery(typeName, json);

        // Assert
        Assert.NotNull(result);
        var response = ParseHubResponse(result);
        AssertIsFailedResponse(response);
    }

    [Fact]
    public async Task PublishNotification_WithNullDeserialization_DoesNotThrow()
    {
        // Arrange
        using var hub = CreateHub();
        var json = JsonSerializer.SerializeToElement<object?>(null);
        var typeName = typeof(TestHubNotification).AssemblyQualifiedName!;

        // Act
        var exception = await Record.ExceptionAsync(() =>
            hub.PublishNotification(typeName, json));

        // Assert
        Assert.Null(exception);
    }

    private sealed record HubResponse(bool success, ErrorInfo? error);

    private sealed record ErrorInfo(string code, string? message);

    private static HubResponse ParseHubResponse(object result)
    {
        var json = JsonSerializer.Serialize(result);
        return JsonSerializer.Deserialize<HubResponse>(json)
            ?? throw new InvalidOperationException("Failed to parse hub response");
    }

    private static void AssertIsFailedResponse(HubResponse response)
    {
        Assert.False(response.success, "Expected 'success' property to be false for a failed response");
    }

    private static void AssertHasErrorCode(HubResponse response, string expectedCode)
    {
        Assert.NotNull(response.error);
        Assert.Equal(expectedCode, response.error.code);
    }

    private static void AssertErrorMessageContains(HubResponse response, string expectedContent)
    {
        Assert.NotNull(response.error);
        Assert.NotNull(response.error.message);
        Assert.Contains(expectedContent, response.error.message);
    }

    private static HubCallerContext CreateMockHubContext()
    {
        var hubContext = Substitute.For<HubCallerContext>();
        hubContext.ConnectionId.Returns("test-connection-id");
        hubContext.ConnectionAborted.Returns(CancellationToken.None);
        return hubContext;
    }

    // Test hub implementation for testing abstract EncinaHub
    private sealed class TestEncinaHub : EncinaHub
    {
        public TestEncinaHub(IEncina encina, IOptions<SignalROptions> options, ILogger logger)
            : base(encina, options, logger)
        {
        }

        public IEncina GetEncina() => Encina;

        public Type? TestResolveType(string typeName) => ResolveType(typeName);
    }

    // Test types for hub tests
    private sealed record TestCommand(string Message) : IRequest<string>;

    private sealed record TestQuery(string Query) : IRequest<string>;

    private sealed record TestHubNotification(string Message) : INotification;
}
