using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for Ports and Adapters pattern classes: IPort, AdapterBase, AdapterError, PortRegistrationExtensions.
/// </summary>
public sealed class PortsAndAdaptersTests
{
    #region Test Types

    public interface ITestInboundPort : IInboundPort
    {
        string GetData();
    }

    public interface ITestOutboundPort : IOutboundPort
    {
        Task<string> FetchDataAsync(CancellationToken ct = default);
    }

    public sealed class TestInboundAdapter : ITestInboundPort
    {
        public string GetData() => "test-data";
    }

    public sealed class TestOutboundAdapter : ITestOutboundPort
    {
        public Task<string> FetchDataAsync(CancellationToken ct = default)
            => Task.FromResult("fetched-data");
    }

    public sealed class TestAdapterWithBase : AdapterBase<ITestOutboundPort>, ITestOutboundPort
    {
        public TestAdapterWithBase(ILogger<TestAdapterWithBase> logger) : base(logger) { }

        public async Task<string> FetchDataAsync(CancellationToken ct = default)
        {
            var result = await ExecuteAsync(
                "FetchData",
                () => Task.FromResult("base-adapter-data"),
                ct);
            return result.Match(
                Right: data => data,
                Left: _ => "error");
        }

        public LanguageExt.Either<AdapterError, string> TestExecute(string operationName, Func<string> operation)
            => Execute(operationName, operation);

        public Task<LanguageExt.Either<AdapterError, string>> TestExecuteAsync(
            string operationName,
            Func<Task<string>> operation,
            CancellationToken ct = default)
            => ExecuteAsync(operationName, operation, ct);
    }

    public sealed class FailingAdapter : AdapterBase<ITestOutboundPort>, ITestOutboundPort
    {
        public FailingAdapter(ILogger<FailingAdapter> logger) : base(logger) { }

        public Task<string> FetchDataAsync(CancellationToken ct = default)
            => throw new InvalidOperationException("Fetch failed");
    }

    #endregion

    #region AdapterBase Tests

    [Fact]
    public void AdapterBase_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestAdapterWithBase(null!));
    }

    [Fact]
    public void AdapterBase_Execute_Success_ReturnsRight()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);

        // Act
        var result = adapter.TestExecute("TestOp", () => "success");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: data => data.ShouldBe("success"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void AdapterBase_Execute_ThrowsException_ReturnsLeft()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);

        // Act
        var result = adapter.TestExecute("FailingOp", () => throw new InvalidOperationException("Test failure"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.ErrorCode.ShouldBe("ADAPTER_OPERATION_FAILED");
                error.OperationName.ShouldBe("FailingOp");
            });
    }

    [Fact]
    public void AdapterBase_Execute_NullOperationName_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            adapter.TestExecute(null!, () => "data"));
    }

    [Fact]
    public void AdapterBase_Execute_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            adapter.TestExecute("Op", null!));
    }

    [Fact]
    public async Task AdapterBase_ExecuteAsync_Success_ReturnsRight()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);

        // Act
        var result = await adapter.TestExecuteAsync("AsyncOp", () => Task.FromResult("async-success"));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: data => data.ShouldBe("async-success"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task AdapterBase_ExecuteAsync_ThrowsException_ReturnsLeft()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);

        // Act
        var result = await adapter.TestExecuteAsync("FailingAsyncOp",
            () => throw new InvalidOperationException("Async failure"));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.ErrorCode.ShouldBe("ADAPTER_OPERATION_FAILED"));
    }

    [Fact]
    public async Task AdapterBase_ExecuteAsync_Cancelled_ReturnsLeft()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await adapter.TestExecuteAsync(
            "CancelledOp",
            async () =>
            {
                // Use a very short delay that respects the cancellation token
                await Task.Delay(1, cts.Token);
                return "should not return";
            },
            cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.ErrorCode.ShouldBe("ADAPTER_OPERATION_CANCELLED"));
    }

    [Fact]
    public async Task AdapterBase_ExecuteAsync_NullOperationName_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await adapter.TestExecuteAsync(null!, () => Task.FromResult("data")));
    }

    [Fact]
    public async Task AdapterBase_ExecuteAsync_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<TestAdapterWithBase>.Instance;
        var adapter = new TestAdapterWithBase(logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await adapter.TestExecuteAsync("Op", null!));
    }

    #endregion

    #region AdapterError Tests

    [Fact]
    public void AdapterError_OperationFailed_CreatesCorrectError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test failure");

        // Act
        var error = AdapterError.OperationFailed<ITestOutboundPort>("FetchData", exception);

        // Assert
        error.ErrorCode.ShouldBe("ADAPTER_OPERATION_FAILED");
        error.PortType.ShouldBe(typeof(ITestOutboundPort));
        error.OperationName.ShouldBe("FetchData");
        error.InnerException.ShouldBe(exception);
        error.Message.ShouldContain("FetchData");
    }

    [Fact]
    public void AdapterError_Cancelled_CreatesCorrectError()
    {
        // Act
        var error = AdapterError.Cancelled<ITestOutboundPort>("FetchData");

        // Assert
        error.ErrorCode.ShouldBe("ADAPTER_OPERATION_CANCELLED");
        error.PortType.ShouldBe(typeof(ITestOutboundPort));
        error.OperationName.ShouldBe("FetchData");
        error.Message.ShouldContain("cancelled");
    }

    [Fact]
    public void AdapterError_NotFound_CreatesCorrectError()
    {
        // Act
        var error = AdapterError.NotFound<ITestOutboundPort>("Customer");

        // Assert
        error.ErrorCode.ShouldBe("ADAPTER_NOT_FOUND");
        error.PortType.ShouldBe(typeof(ITestOutboundPort));
        error.Message.ShouldContain("Customer");
    }

    [Fact]
    public void AdapterError_CommunicationFailed_CreatesCorrectError()
    {
        // Arrange
        var exception = new TimeoutException("Connection timeout");

        // Act
        var error = AdapterError.CommunicationFailed<ITestOutboundPort>("External API", exception);

        // Assert
        error.ErrorCode.ShouldBe("ADAPTER_COMMUNICATION_FAILED");
        error.Message.ShouldContain("External API");
        error.InnerException.ShouldBe(exception);
    }

    [Fact]
    public void AdapterError_CommunicationFailed_WithoutException_CreatesCorrectError()
    {
        // Act
        var error = AdapterError.CommunicationFailed<ITestOutboundPort>("External API");

        // Assert
        error.ErrorCode.ShouldBe("ADAPTER_COMMUNICATION_FAILED");
        error.InnerException.ShouldBeNull();
    }

    [Fact]
    public void AdapterError_ExternalError_CreatesCorrectError()
    {
        // Act
        var error = AdapterError.ExternalError<ITestOutboundPort>("Payment Gateway", "Invalid card");

        // Assert
        error.ErrorCode.ShouldBe("ADAPTER_EXTERNAL_ERROR");
        error.Message.ShouldContain("Payment Gateway");
        error.Message.ShouldContain("Invalid card");
    }

    #endregion

    #region PortRegistrationExtensions Tests

    [Fact]
    public void AddPort_RegistersAdapterForPort()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPort<ITestInboundPort, TestInboundAdapter>();
        var provider = services.BuildServiceProvider();

        // Assert
        var port = provider.GetService<ITestInboundPort>();
        port.ShouldNotBeNull();
        port.ShouldBeOfType<TestInboundAdapter>();
        port.GetData().ShouldBe("test-data");
    }

    [Fact]
    public void AddPort_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddPort<ITestInboundPort, TestInboundAdapter>());
    }

    [Fact]
    public void AddPort_WithLifetime_UsesSpecifiedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPort<ITestInboundPort, TestInboundAdapter>(ServiceLifetime.Singleton);

        // Assert
        var descriptor = services.First();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddPort_WithFactory_RegistersFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPort<ITestInboundPort>(_ => new TestInboundAdapter());
        var provider = services.BuildServiceProvider();

        // Assert
        var port = provider.GetService<ITestInboundPort>();
        port.ShouldNotBeNull();
    }

    [Fact]
    public void AddPort_WithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddPort<ITestInboundPort>(_ => new TestInboundAdapter()));
    }

    [Fact]
    public void AddPort_WithFactory_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddPort<ITestInboundPort>(null!));
    }

    [Fact]
    public void AddPortsFromAssembly_RegistersPortsFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        // Add logger dependencies for adapters that need them
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var assembly = typeof(PortsAndAdaptersTests).Assembly;

        // Act
        services.AddPortsFromAssembly(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        // Should find and register TestInboundAdapter and TestOutboundAdapter
        provider.GetService<ITestInboundPort>().ShouldNotBeNull();
        provider.GetService<ITestOutboundPort>().ShouldNotBeNull();
    }

    [Fact]
    public void AddPortsFromAssembly_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = typeof(PortsAndAdaptersTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddPortsFromAssembly(assembly));
    }

    [Fact]
    public void AddPortsFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddPortsFromAssembly(null!));
    }

    [Fact]
    public void AddPortsFromAssemblies_RegistersFromMultipleAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        // Add logger dependencies for adapters that need them
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var assemblies = new[] { typeof(PortsAndAdaptersTests).Assembly };

        // Act
        services.AddPortsFromAssemblies(assemblies);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ITestInboundPort>().ShouldNotBeNull();
    }

    [Fact]
    public void AddPortsFromAssemblies_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var assemblies = new[] { typeof(PortsAndAdaptersTests).Assembly };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddPortsFromAssemblies(assemblies));
    }

    [Fact]
    public void AddPortsFromAssemblies_NullAssemblies_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddPortsFromAssemblies(null!));
    }

    #endregion
}
