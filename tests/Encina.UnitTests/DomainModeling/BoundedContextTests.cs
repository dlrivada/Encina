using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Unit tests for bounded context registration and resolution.
/// </summary>
public class BoundedContextTests
{
    #region Test Contexts

    private sealed class TestOrdersContext : BoundedContextModule
    {
        public override string ContextName => "Orders";
        public override string? Description => "Order management";

        public override void Configure(IServiceCollection services)
        {
            // No-op for testing
        }
    }

    private sealed class TestShippingContext : BoundedContextModule
    {
        public override string ContextName => "Shipping";

        public override void Configure(IServiceCollection services)
        {
            // No-op for testing
        }
    }

    #endregion

    [Fact]
    public void AddBoundedContext_Parameterless_RegistersContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBoundedContext<TestOrdersContext>();

        // Assert
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<TestOrdersContext>();
        resolved.ShouldNotBeNull();
        resolved.ContextName.ShouldBe("Orders");
    }

    [Fact]
    public void AddBoundedContext_WithFactory_RegistersContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBoundedContext<TestOrdersContext>(_ => new TestOrdersContext());

        // Assert
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<TestOrdersContext>();
        resolved.ShouldNotBeNull();
        resolved.ContextName.ShouldBe("Orders");
    }

    [Fact]
    public void AddBoundedContext_WithFactory_RegistersAsBaseType()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBoundedContext<TestOrdersContext>(_ => new TestOrdersContext());

        // Assert
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<BoundedContextModule>();
        resolved.ShouldNotBeNull();
        resolved.ContextName.ShouldBe("Orders");
    }

    [Fact]
    public void AddBoundedContextModule_RegistersAsInterface()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBoundedContextModule<TestModuleContext>();

        // Assert
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IBoundedContextModule>();
        resolved.ShouldNotBeNull();
        resolved.ContextName.ShouldBe("TestModule");
    }

    [Fact]
    public void AddBoundedContext_WithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            BoundedContextExtensions.AddBoundedContext<TestOrdersContext>(null!, _ => new TestOrdersContext()));
    }

    [Fact]
    public void AddBoundedContext_WithFactory_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddBoundedContext<TestOrdersContext>((Func<IServiceProvider, TestOrdersContext>)null!));
    }

    #region Test IBoundedContextModule

    private sealed class TestModuleContext : IBoundedContextModule
    {
        public string ContextName => "TestModule";
        public string? Description => "Test module";
        public IEnumerable<Type> PublishedIntegrationEvents => [];
        public IEnumerable<Type> ConsumedIntegrationEvents => [];
        public IEnumerable<Type> ExposedPorts => [];

        public void Configure(IServiceCollection services)
        {
            // No-op for testing
        }
    }

    #endregion
}
