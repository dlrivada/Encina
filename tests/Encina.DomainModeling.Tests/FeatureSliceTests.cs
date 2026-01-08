using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for FeatureSlice, FeatureSliceConfiguration, FeatureSliceExtensions,
/// SliceDependency, FeatureSliceError, and UseCase handlers.
/// </summary>
public sealed class FeatureSliceTests
{
    #region Test Feature Slices

    private sealed class OrdersSlice : FeatureSlice
    {
        public override string FeatureName => "Orders";
        public override string? Description => "Order management feature";
        public override string? RoutePrefix => "/api/orders";

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IOrderService, OrderService>();
        }
    }

    private sealed class InventorySlice : FeatureSlice
    {
        public override string FeatureName => "Inventory";

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IInventoryService, InventoryService>();
        }
    }

    private sealed class ShippingSlice : FeatureSlice, IFeatureSliceWithDependencies
    {
        public override string FeatureName => "Shipping";

        public IEnumerable<SliceDependency> Dependencies =>
        [
            new SliceDependency("Orders"),
            new SliceDependency("Tracking", IsOptional: true)
        ];

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IShippingService, ShippingService>();
        }
    }

    private sealed class InvalidSlice : FeatureSlice, IFeatureSliceWithDependencies
    {
        public override string FeatureName => "Invalid";

        public IEnumerable<SliceDependency> Dependencies =>
        [
            new SliceDependency("NonExistent") // Required but not registered
        ];

        public override void ConfigureServices(IServiceCollection services) { }
    }

    // Test service interfaces and implementations
    private interface IOrderService { }
    private sealed class OrderService : IOrderService { }
    private interface IInventoryService { }
    private sealed class InventoryService : IInventoryService { }
    private interface IShippingService { }
    private sealed class ShippingService : IShippingService { }

    #endregion

    #region FeatureSlice Basic Tests

    [Fact]
    public void FeatureSlice_Properties_ReturnCorrectValues()
    {
        // Arrange
        var slice = new OrdersSlice();
        // Act
        var featureName = slice.FeatureName;
        var description = slice.Description;
        var routePrefix = slice.RoutePrefix;

        // Assert
        featureName.ShouldBe("Orders");
        description.ShouldBe("Order management feature");
        routePrefix.ShouldBe("/api/orders");
    }

    [Fact]
    public void FeatureSlice_DefaultProperties_ReturnNull()
    {
        // Arrange
        var slice = new InventorySlice();

        // Act

        // Assert
        slice.Description.ShouldBeNull();
        slice.RoutePrefix.ShouldBeNull();
    }

    [Fact]
    public void FeatureSlice_ConfigureServices_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var slice = new OrdersSlice();

        // Act
        slice.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOrderService>().ShouldNotBeNull();
    }

    #endregion

    #region FeatureSliceConfiguration Tests

    [Fact]
    public void FeatureSliceConfiguration_AddSlice_Generic_AddsSlice()
    {
        // Arrange
        var config = new FeatureSliceConfiguration();

        // Act
        config.AddSlice<OrdersSlice>();

        // Assert
        config.Slices.Count.ShouldBe(1);
        config.Slices[0].FeatureName.ShouldBe("Orders");
    }

    [Fact]
    public void FeatureSliceConfiguration_AddSlice_Instance_AddsSlice()
    {
        // Arrange
        var config = new FeatureSliceConfiguration();
        var slice = new InventorySlice();

        // Act
        config.AddSlice(slice);

        // Assert
        config.Slices.Count.ShouldBe(1);
        config.Slices[0].ShouldBe(slice);
    }

    [Fact]
    public void FeatureSliceConfiguration_AddSlice_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new FeatureSliceConfiguration();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => config.AddSlice(null!));
    }

    [Fact]
    public void FeatureSliceConfiguration_FluentChaining_WorksCorrectly()
    {
        // Arrange & Act
        var config = new FeatureSliceConfiguration()
            .AddSlice<OrdersSlice>()
            .AddSlice<InventorySlice>();

        // Assert
        config.Slices.Count.ShouldBe(2);
    }

    [Fact]
    public void FeatureSliceConfiguration_ValidateDependencies_DefaultTrue()
    {
        // Arrange
        var config = new FeatureSliceConfiguration();

        // Act
        var result = config.ValidateDependencies;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void FeatureSliceConfiguration_AddSlicesFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new FeatureSliceConfiguration();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => config.AddSlicesFromAssembly(null!));
    }

    #endregion

    #region SliceDependency Tests

    [Fact]
    public void SliceDependency_RequiredDependency_IsNotOptional()
    {
        // Arrange
        // Act
        var dep = new SliceDependency("Orders");

        // Assert
        dep.SliceName.ShouldBe("Orders");
        dep.IsOptional.ShouldBeFalse();
    }

    [Fact]
    public void SliceDependency_OptionalDependency_IsOptional()
    {
        // Arrange
        // Act
        var dep = new SliceDependency("Tracking", IsOptional: true);

        // Assert
        dep.SliceName.ShouldBe("Tracking");
        dep.IsOptional.ShouldBeTrue();
    }

    #endregion

    #region FeatureSliceError Tests

    [Fact]
    public void FeatureSliceError_MissingDependency_CreatesCorrectError()
    {
        // Act
        var error = FeatureSliceError.MissingDependency("Shipping", "Orders");

        // Assert
        error.ErrorCode.ShouldBe("SLICE_MISSING_DEPENDENCY");
        error.SliceName.ShouldBe("Shipping");
        error.Message.ShouldContain("Shipping");
        error.Message.ShouldContain("Orders");
    }

    [Fact]
    public void FeatureSliceError_CircularDependency_CreatesCorrectError()
    {
        // Arrange
        var cycle = new List<string> { "A", "B", "C", "A" };

        // Act
        var error = FeatureSliceError.CircularDependency(cycle);

        // Assert
        error.ErrorCode.ShouldBe("SLICE_CIRCULAR_DEPENDENCY");
        error.Message.ShouldContain("A -> B -> C -> A");
    }

    [Fact]
    public void FeatureSliceError_RegistrationFailed_CreatesCorrectError()
    {
        // Arrange
        var exception = new InvalidOperationException("Registration failed");

        // Act
        var error = FeatureSliceError.RegistrationFailed("Orders", exception);

        // Assert
        error.ErrorCode.ShouldBe("SLICE_REGISTRATION_FAILED");
        error.SliceName.ShouldBe("Orders");
        error.Message.ShouldContain("Registration failed");
    }

    #endregion

    #region FeatureSliceExtensions Tests

    [Fact]
    public void AddFeatureSlices_RegistersSlicesAndConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFeatureSlices(config =>
        {
            config.AddSlice<OrdersSlice>();
            config.AddSlice<InventorySlice>();
        });
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<FeatureSliceConfiguration>().ShouldNotBeNull();
        // Slices are registered as FeatureSlice (ensure specific slices were registered)
        provider.GetServices<FeatureSlice>().Any(s => s.FeatureName == "Orders").ShouldBeTrue();
        provider.GetServices<FeatureSlice>().Any(s => s.FeatureName == "Inventory").ShouldBeTrue();
        // Services configured by slices should be available
        provider.GetService<IOrderService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddFeatureSlices_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddFeatureSlices(_ => { }));
    }

    [Fact]
    public void AddFeatureSlices_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddFeatureSlices(null!));
    }

    [Fact]
    public void AddFeatureSlices_ValidatesDependencies_ThrowsOnMissingRequired()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            services.AddFeatureSlices(config =>
            {
                config.AddSlice<InvalidSlice>();
            }));
    }

    [Fact]
    public void AddFeatureSlices_OptionalDependencyMissing_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFeatureSlices(config =>
        {
            config.AddSlice<OrdersSlice>();
            config.AddSlice<ShippingSlice>(); // Depends on Orders (present) and Tracking (optional, missing)
        });

        // Assert - should not throw
        var provider = services.BuildServiceProvider();
        provider.GetServices<FeatureSlice>().Any(s => s.FeatureName == "Orders").ShouldBeTrue();
        provider.GetServices<FeatureSlice>().Any(s => s.FeatureName == "Shipping").ShouldBeTrue();
    }

    [Fact]
    public void AddFeatureSlices_SkipValidation_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFeatureSlices(config =>
        {
            config.ValidateDependencies = false;
            config.AddSlice<InvalidSlice>(); // Missing required dependency
        });

        // Assert - should not throw because validation is disabled
        var provider = services.BuildServiceProvider();
        provider.GetServices<FeatureSlice>().Any(s => s.FeatureName == "Invalid").ShouldBeTrue();
    }

    [Fact]
    public void AddFeatureSlice_RegistersSingleSlice()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFeatureSlice<OrdersSlice>();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<OrdersSlice>().ShouldNotBeNull();
        provider.GetService<FeatureSlice>().ShouldNotBeNull();
        provider.GetService<IOrderService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddFeatureSlice_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddFeatureSlice<OrdersSlice>());
    }

    [Fact]
    public void AddFeatureSlicesFromAssembly_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = typeof(FeatureSliceTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddFeatureSlicesFromAssembly(assembly));
    }

    [Fact]
    public void AddFeatureSlicesFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddFeatureSlicesFromAssembly(null!));
    }

    [Fact]
    public void GetFeatureSlices_ReturnsAllRegisteredSlices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFeatureSlices(config =>
        {
            config.AddSlice<OrdersSlice>();
            config.AddSlice<InventorySlice>();
        });
        var provider = services.BuildServiceProvider();

        // Act
        var slices = provider.GetFeatureSlices();

        // Assert - ensure the requested feature slices were returned
        slices.Any(s => s.FeatureName == "Orders").ShouldBeTrue();
        slices.Any(s => s.FeatureName == "Inventory").ShouldBeTrue();
    }

    [Fact]
    public void GetFeatureSlices_NullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider? provider = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => provider!.GetFeatureSlices());
    }

    [Fact]
    public void GetFeatureSlice_ByName_ReturnsCorrectSlice()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFeatureSlices(config =>
        {
            config.AddSlice<OrdersSlice>();
            config.AddSlice<InventorySlice>();
        });
        var provider = services.BuildServiceProvider();

        // Act
        var slice = provider.GetFeatureSlice("Orders");

        // Assert
        slice.ShouldNotBeNull();
        slice.FeatureName.ShouldBe("Orders");
    }

    [Fact]
    public void GetFeatureSlice_ByName_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFeatureSlices(config =>
        {
            config.AddSlice<OrdersSlice>();
        });
        var provider = services.BuildServiceProvider();

        // Act
        var slice = provider.GetFeatureSlice("NonExistent");

        // Assert
        slice.ShouldBeNull();
    }

    [Fact]
    public void GetFeatureSlice_NullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider? provider = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => provider!.GetFeatureSlice("Orders"));
    }

    [Fact]
    public void GetFeatureSlice_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        Should.Throw<ArgumentException>(() => provider.GetFeatureSlice(""));
    }

    #endregion

    #region UseCase Handler Tests

    private interface ITestInput { }
    private sealed record TestInput(string Value) : ITestInput;
    private sealed record TestOutput(string Result);

    private sealed class TestUseCaseHandler : IUseCaseHandler<TestInput, TestOutput>
    {
        public Task<TestOutput> HandleAsync(TestInput input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TestOutput($"Processed: {input.Value}"));
        }
    }

    private sealed class TestCommandHandler : IUseCaseHandler<TestInput>
    {
        public bool Handled { get; private set; }

        public Task HandleAsync(TestInput input, CancellationToken cancellationToken = default)
        {
            Handled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void AddUseCaseHandler_RegistersHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUseCaseHandler<TestUseCaseHandler>();
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IUseCaseHandler<TestInput, TestOutput>>();
        handler.ShouldNotBeNull();
    }

    [Fact]
    public void AddUseCaseHandler_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddUseCaseHandler<TestUseCaseHandler>());
    }

    [Fact]
    public void AddUseCaseHandlersFromAssembly_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = typeof(FeatureSliceTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddUseCaseHandlersFromAssembly(assembly));
    }

    [Fact]
    public void AddUseCaseHandlersFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddUseCaseHandlersFromAssembly(null!));
    }

    [Fact]
    public void AddUseCaseHandler_WithDifferentLifetime_UsesSpecifiedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUseCaseHandler<TestUseCaseHandler>(ServiceLifetime.Singleton);

        // Assert
        var descriptor = services.Single(d => d.ServiceType == typeof(IUseCaseHandler<TestInput, TestOutput>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public async Task UseCaseHandler_WithInputOutput_HandlesCorrectly()
    {
        // Arrange
        var handler = new TestUseCaseHandler();
        var input = new TestInput("test");

        // Act
        var output = await handler.HandleAsync(input);

        // Assert
        output.Result.ShouldBe("Processed: test");
    }

    [Fact]
    public async Task UseCaseHandler_WithInputOnly_HandlesCorrectly()
    {
        // Arrange
        var handler = new TestCommandHandler();
        var input = new TestInput("test");

        // Act
        await handler.HandleAsync(input);

        // Assert
        handler.Handled.ShouldBeTrue();
    }

    #endregion
}
