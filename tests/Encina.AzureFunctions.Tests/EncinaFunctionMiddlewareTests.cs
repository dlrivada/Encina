using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.AzureFunctions.Tests;

public class EncinaFunctionMiddlewareTests
{
    private readonly IOptions<EncinaAzureFunctionsOptions> _options;
    private readonly FakeLogger<EncinaFunctionMiddleware> _logger;
    private readonly EncinaFunctionMiddleware _middleware;
    private readonly FunctionContext _context;
    private readonly FunctionDefinition _functionDefinition;
    private readonly Dictionary<object, object> _items;

    public EncinaFunctionMiddlewareTests()
    {
        _options = Options.Create(new EncinaAzureFunctionsOptions());
        _logger = new FakeLogger<EncinaFunctionMiddleware>();
        _middleware = new EncinaFunctionMiddleware(_options, _logger);
        _context = Substitute.For<FunctionContext>();
        _functionDefinition = Substitute.For<FunctionDefinition>();
        _items = new Dictionary<object, object>();

        _context.Items.Returns(_items);
        _context.InvocationId.Returns("test-invocation-id");
        _functionDefinition.Name.Returns("TestFunction");
        _context.FunctionDefinition.Returns(_functionDefinition);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EncinaFunctionMiddleware(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EncinaFunctionMiddleware(_options, null!));
    }

    [Fact]
    public async Task Invoke_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        FunctionExecutionDelegate next = _ => Task.CompletedTask;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _middleware.Invoke(null!, next));
    }

    [Fact]
    public async Task Invoke_WithNullNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _middleware.Invoke(_context, null!));
    }

    [Fact]
    public async Task Invoke_CallsNextDelegate()
    {
        // Arrange
        var nextCalled = false;
        FunctionExecutionDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _middleware.Invoke(_context, next);

        // Assert
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_EnrichesContext_WhenEnabled()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions { EnableRequestContextEnrichment = true });
        var middleware = new EncinaFunctionMiddleware(options, _logger);
        FunctionExecutionDelegate next = _ => Task.CompletedTask;

        // Act
        await middleware.Invoke(_context, next);

        // Assert
        _items.ShouldContainKey(EncinaFunctionMiddleware.CorrelationIdKey);
        var correlationId = _items[EncinaFunctionMiddleware.CorrelationIdKey] as string;
        correlationId.ShouldNotBeNull();
        correlationId!.Length.ShouldBe(32); // GUID without dashes
    }

    [Fact]
    public async Task Invoke_DoesNotEnrichContext_WhenDisabled()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions { EnableRequestContextEnrichment = false });
        var middleware = new EncinaFunctionMiddleware(options, _logger);
        FunctionExecutionDelegate next = _ => Task.CompletedTask;

        // Act
        await middleware.Invoke(_context, next);

        // Assert
        _items.ShouldNotContainKey(EncinaFunctionMiddleware.CorrelationIdKey);
    }

    [Fact]
    public async Task Invoke_LogsFunctionExecutionStarting()
    {
        // Arrange
        FunctionExecutionDelegate next = _ => Task.CompletedTask;

        // Act
        await _middleware.Invoke(_context, next);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("starting"));
        logEntry.ShouldNotBeNull();
    }

    [Fact]
    public async Task Invoke_LogsFunctionExecutionCompleted()
    {
        // Arrange
        FunctionExecutionDelegate next = _ => Task.CompletedTask;

        // Act
        await _middleware.Invoke(_context, next);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("completed"));
        logEntry.ShouldNotBeNull();
    }

    [Fact]
    public async Task Invoke_LogsFunctionExecutionFailed_OnException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        FunctionExecutionDelegate next = _ => throw exception;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _middleware.Invoke(_context, next));

        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("failed"));
        logEntry.ShouldNotBeNull();
    }

    [Fact]
    public async Task Invoke_RethrowsException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        FunctionExecutionDelegate next = _ => throw exception;

        // Act & Assert
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _middleware.Invoke(_context, next));

        thrown.ShouldBe(exception);
    }
}
