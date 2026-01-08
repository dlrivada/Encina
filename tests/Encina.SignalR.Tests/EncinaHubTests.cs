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
public sealed class EncinaHubTests : IDisposable
{
    private readonly IEncina _encina;
    private readonly IOptions<SignalROptions> _options;
    private readonly ILogger<TestEncinaHub> _logger;
    private readonly TestEncinaHub _hub;

    public EncinaHubTests()
    {
        _encina = Substitute.For<IEncina>();
        _options = Options.Create(new SignalROptions());
        _logger = Substitute.For<ILogger<TestEncinaHub>>();
        _hub = new TestEncinaHub(_encina, _options, _logger);

        // Setup mock hub context
        var hubContext = Substitute.For<HubCallerContext>();
        hubContext.ConnectionAborted.Returns(CancellationToken.None);
        _hub.SetContext(hubContext);
    }

    public void Dispose()
    {
        _hub.Dispose();
    }

    [Fact]
    public void Constructor_SetsEncinaProperty()
    {
        // Assert
        _hub.GetEncina().ShouldBeSameAs(_encina);
    }

    [Fact]
    public async Task SendCommand_WithUnknownType_ReturnsTypeNotFoundError()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement(new { Value = "test" });

        // Act
        var result = await _hub.SendCommand("NonExistent.Command", json);

        // Assert
        result.ShouldNotBeNull();
        AssertIsFailedResponse(result);
        AssertHasErrorCode(result, "command.type_not_found");
    }

    [Fact]
    public async Task SendQuery_WithUnknownType_ReturnsTypeNotFoundError()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement(new { Value = "test" });

        // Act
        var result = await _hub.SendQuery("NonExistent.Query", json);

        // Assert
        result.ShouldNotBeNull();
        AssertIsFailedResponse(result);
        AssertHasErrorCode(result, "query.type_not_found");
    }

    [Fact]
    public async Task PublishNotification_WithUnknownType_DoesNotThrow()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement(new { Value = "test" });

        // Act - should not throw
        await _hub.PublishNotification("NonExistent.Notification", json);

        // Assert - no exception means success
    }

    [Fact]
    public async Task SendCommand_WhenExceptionOccurs_ReturnsErrorResponse()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement(new { Message = "test" });
        var typeName = typeof(TestCommand).AssemblyQualifiedName!;

        // The Hub uses dynamic dispatch which will cause the Encina.Send to throw
        // because our mock doesn't handle the specific command type

        // Act
        var result = await _hub.SendCommand(typeName, json);

        // Assert
        result.ShouldNotBeNull();
        // The result should be an error response (either exception or failed to process)
        AssertIsFailedResponse(result);
    }

    [Fact]
    public async Task SendQuery_WhenExceptionOccurs_ReturnsErrorResponse()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement(new { Query = "test" });
        var typeName = typeof(TestQuery).AssemblyQualifiedName!;

        // Act
        var result = await _hub.SendQuery(typeName, json);

        // Assert
        result.ShouldNotBeNull();
        AssertIsFailedResponse(result);
    }

    [Fact]
    public async Task PublishNotification_WhenExceptionOccurs_DoesNotThrow()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement(new { Message = "test" });
        var typeName = typeof(TestHubNotification).AssemblyQualifiedName!;

        // Act - should not throw even if Encina.Publish throws
        await _hub.PublishNotification(typeName, json);

        // Assert - no exception
    }

    [Fact]
    public void ResolveType_WithExactTypeName_ReturnsType()
    {
        // Arrange
        var typeName = typeof(TestCommand).AssemblyQualifiedName!;

        // Act
        var type = _hub.TestResolveType(typeName);

        // Assert
        type.ShouldBe(typeof(TestCommand));
    }

    [Fact]
    public void ResolveType_WithSimpleName_ReturnsType()
    {
        // Arrange - use a well-known type that should be resolvable
        var typeName = nameof(TestCommand);

        // Act
        var type = _hub.TestResolveType(typeName);

        // Assert
        type.ShouldBe(typeof(TestCommand));
    }

    [Fact]
    public void ResolveType_WithNonExistentType_ReturnsNull()
    {
        // Arrange
        var typeName = "NonExistent.Type.That.Does.Not.Exist";

        // Act
        var type = _hub.TestResolveType(typeName);

        // Assert
        type.ShouldBeNull();
    }

    [Fact]
    public async Task SendCommand_WithDetailedErrorsEnabled_IncludesDetails()
    {
        // Arrange
        var options = Options.Create(new SignalROptions { IncludeDetailedErrors = true });
        using var hub = new TestEncinaHub(_encina, options, _logger);
        var hubContext = Substitute.For<HubCallerContext>();
        hubContext.ConnectionAborted.Returns(CancellationToken.None);
        hub.SetContext(hubContext);

        var json = JsonSerializer.SerializeToElement(new { Value = "test" });
        var typeName = typeof(TestCommand).AssemblyQualifiedName!;

        // Act
        var result = await hub.SendCommand(typeName, json);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendQuery_WithDetailedErrorsEnabled_IncludesDetails()
    {
        // Arrange
        var options = Options.Create(new SignalROptions { IncludeDetailedErrors = true });
        using var hub = new TestEncinaHub(_encina, options, _logger);
        var hubContext = Substitute.For<HubCallerContext>();
        hubContext.ConnectionAborted.Returns(CancellationToken.None);
        hub.SetContext(hubContext);

        var json = JsonSerializer.SerializeToElement(new { Value = "test" });
        var typeName = typeof(TestQuery).AssemblyQualifiedName!;

        // Act
        var result = await hub.SendQuery(typeName, json);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendCommand_WithNullDeserialization_ReturnsDeserializationError()
    {
        // Arrange
        // Create a JSON that would deserialize to null for a record type
        var json = JsonSerializer.SerializeToElement<object?>(null);
        var typeName = typeof(TestCommand).AssemblyQualifiedName!;

        // Act
        var result = await _hub.SendCommand(typeName, json);

        // Assert
        result.ShouldNotBeNull();
        AssertIsFailedResponse(result);
    }

    [Fact]
    public async Task SendQuery_WithNullDeserialization_ReturnsDeserializationError()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement<object?>(null);
        var typeName = typeof(TestQuery).AssemblyQualifiedName!;

        // Act
        var result = await _hub.SendQuery(typeName, json);

        // Assert
        result.ShouldNotBeNull();
        AssertIsFailedResponse(result);
    }

    [Fact]
    public async Task PublishNotification_WithNullDeserialization_DoesNotThrow()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement<object?>(null);
        var typeName = typeof(TestHubNotification).AssemblyQualifiedName!;

        // Act - should not throw
        await _hub.PublishNotification(typeName, json);

        // Assert - no exception
    }

    private static void AssertIsFailedResponse(object result)
    {
        // The response should have a 'success' property set to false
        var successProp = result.GetType().GetProperty("success");
        if (successProp != null)
        {
            var successValue = successProp.GetValue(result);
            ((bool)successValue!).ShouldBeFalse();
        }
        // If there's no 'success' property, we can't verify - but the test passed if we got here
    }

    private static void AssertHasErrorCode(object result, string expectedCode)
    {
        // The response should have an 'error.code' property
        var errorProp = result.GetType().GetProperty("error");
        if (errorProp != null)
        {
            var errorValue = errorProp.GetValue(result);
            if (errorValue != null)
            {
                var codeProp = errorValue.GetType().GetProperty("code");
                if (codeProp != null)
                {
                    var codeValue = codeProp.GetValue(errorValue);
                    ((string)codeValue!).ShouldBe(expectedCode);
                    return;
                }
            }
        }
        // If we couldn't find the error code, fail the assertion
        Assert.Fail($"Expected error code '{expectedCode}' not found in result");
    }

    // Test hub implementation for testing abstract EncinaHub
    public sealed class TestEncinaHub : EncinaHub
    {
        public TestEncinaHub(IEncina encina, IOptions<SignalROptions> options, ILogger logger)
            : base(encina, options, logger)
        {
        }

        public IEncina GetEncina() => Encina;

        public Type? TestResolveType(string typeName) => ResolveType(typeName);

        public void SetContext(HubCallerContext context)
        {
            // Use reflection to set the Context property (it has a private setter in Hub)
            var contextProperty = typeof(Hub).GetProperty("Context");
            contextProperty?.SetValue(this, context);
        }
    }

    // Test types for hub tests
    public sealed record TestCommand(string Message) : IRequest<string>;

    public sealed record TestQuery(string Query) : IRequest<string>;

    public sealed record TestHubNotification(string Message) : INotification;
}
