using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for ports and adapters patterns.
/// </summary>
public class PortsAndAdaptersProperties
{
    // Test port interface
    private interface ITestPort : IOutboundPort
    {
        string GetValue();
    }

    // === AdapterError Factory Methods ===

    [Property(MaxTest = 100)]
    public bool AdapterError_OperationFailed_HasCorrectCode(NonEmptyString operationName)
    {
        var exception = new InvalidOperationException("Test error");
        var error = AdapterError.OperationFailed<ITestPort>(operationName.Get, exception);

        return error.ErrorCode == "ADAPTER_OPERATION_FAILED"
            && error.PortType == typeof(ITestPort)
            && error.OperationName == operationName.Get
            && error.InnerException == exception
            && error.Message.Contains(operationName.Get);
    }

    [Property(MaxTest = 100)]
    public bool AdapterError_Cancelled_HasCorrectCode(NonEmptyString operationName)
    {
        var error = AdapterError.Cancelled<ITestPort>(operationName.Get);

        return error.ErrorCode == "ADAPTER_OPERATION_CANCELLED"
            && error.PortType == typeof(ITestPort)
            && error.OperationName == operationName.Get
            && error.InnerException is null;
    }

    [Property(MaxTest = 100)]
    public bool AdapterError_NotFound_HasCorrectCode(NonEmptyString resourceName)
    {
        var error = AdapterError.NotFound<ITestPort>(resourceName.Get);

        return error.ErrorCode == "ADAPTER_NOT_FOUND"
            && error.PortType == typeof(ITestPort)
            && error.Message.Contains(resourceName.Get);
    }

    [Property(MaxTest = 100)]
    public bool AdapterError_CommunicationFailed_HasCorrectCode(NonEmptyString systemName)
    {
        var exception = new InvalidOperationException("Connection failed");
        var error = AdapterError.CommunicationFailed<ITestPort>(systemName.Get, exception);

        return error.ErrorCode == "ADAPTER_COMMUNICATION_FAILED"
            && error.PortType == typeof(ITestPort)
            && error.Message.Contains(systemName.Get)
            && error.InnerException == exception;
    }

    [Property(MaxTest = 100)]
    public bool AdapterError_ExternalError_HasCorrectCode(
        NonEmptyString systemName,
        NonEmptyString errorMessage)
    {
        var error = AdapterError.ExternalError<ITestPort>(systemName.Get, errorMessage.Get);

        return error.ErrorCode == "ADAPTER_EXTERNAL_ERROR"
            && error.PortType == typeof(ITestPort)
            && error.Message.Contains(systemName.Get)
            && error.Message.Contains(errorMessage.Get);
    }

    // === AdapterBase Execute Methods ===

    [Property(MaxTest = 100)]
    public bool AdapterBase_Execute_ReturnsSuccessOnValidOperation(NonEmptyString value)
    {
        var logger = NullLogger.Instance;
        var adapter = new TestAdapter(logger, value.Get);

        var result = adapter.TestExecute("TestOperation", () => value.Get);

        return result.IsRight && result.Match(Left: _ => false, Right: v => v == value.Get);
    }

    [Property(MaxTest = 100)]
    public bool AdapterBase_Execute_ReturnsErrorOnException(NonEmptyString operationName)
    {
        var logger = NullLogger.Instance;
        var adapter = new TestAdapter(logger, "test");

        var result = adapter.TestExecute<string>(operationName.Get, () => throw new InvalidOperationException("Test error"));

        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public async Task AdapterBase_ExecuteAsync_ReturnsSuccessOnValidOperation(NonEmptyString value)
    {
        var logger = NullLogger.Instance;
        var adapter = new TestAdapter(logger, value.Get);

        var result = await adapter.TestExecuteAsync("TestOperation", () => Task.FromResult(value.Get));

        Assert.True(result.IsRight);
        result.IfRight(v => Assert.Equal(value.Get, v));
    }

    // === Port Registration ===

    [Property(MaxTest = 100)]
    public bool AddPort_RegistersAdapterCorrectly(NonEmptyString value)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(NullLogger.Instance);
        services.AddSingleton(value.Get);

        // Can't use AddPort with TestAdapter because it needs constructor params
        // Just test that the extension method works
        services.AddPort<ITestPort>(sp => new TestAdapter(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<string>()));

        var provider = services.BuildServiceProvider();
        var port = provider.GetService<ITestPort>();

        return port is not null && port.GetValue() == value.Get;
    }

    // Helper class to expose protected methods for testing
    private sealed class TestAdapter : AdapterBase<ITestPort>, ITestPort
    {
        private readonly string _value;

        public TestAdapter(ILogger logger, string value) : base(logger)
        {
            _value = value;
        }

        public string GetValue() => _value;

        public LanguageExt.Either<AdapterError, T> TestExecute<T>(string operationName, Func<T> operation)
            => Execute(operationName, operation);

        public Task<LanguageExt.Either<AdapterError, T>> TestExecuteAsync<T>(string operationName, Func<Task<T>> operation, CancellationToken ct = default)
            => ExecuteAsync(operationName, operation, ct);
    }
}
