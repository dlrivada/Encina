using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for BoundedContextAttribute, ContextMap, BoundedContextModule,
/// BoundedContextValidator, BoundedContextError, and BoundedContextExtensions.
/// </summary>
public sealed class BoundedContextTests
{
    #region Test Types

    [BoundedContext("Orders", Description = "Order management")]
    private sealed class TestOrder { }

    [BoundedContext("Inventory")]
    private sealed class TestInventoryItem { }

    private sealed class UnattributedClass { }

    private sealed class TestOrdersContext : BoundedContextModule
    {
        public override string ContextName => "Orders";
        public override string? Description => "Order management context";

        public override void Configure(IServiceCollection services)
        {
            services.AddSingleton<ITestOrderService, TestOrderService>();
        }
    }

    private sealed class TestInventoryContext : BoundedContextModule
    {
        public override string ContextName => "Inventory";

        public override void Configure(IServiceCollection services)
        {
            services.AddSingleton<ITestInventoryService, TestInventoryService>();
        }
    }

    private interface ITestOrderService { }
    private sealed class TestOrderService : ITestOrderService { }
    private interface ITestInventoryService { }
    private sealed class TestInventoryService : ITestInventoryService { }

    // Integration event types for testing
    private sealed record OrderPlaced;
    private sealed record OrderShipped;
    private sealed record InventoryReserved;

    private sealed class OrdersModuleWithContracts : IBoundedContextModule
    {
        public string ContextName => "Orders";
        public string? Description => "Orders context";

        public IEnumerable<Type> PublishedIntegrationEvents =>
        [
            typeof(OrderPlaced),
            typeof(OrderShipped)
        ];

        public IEnumerable<Type> ConsumedIntegrationEvents =>
        [
            typeof(InventoryReserved)
        ];

        public IEnumerable<Type> ExposedPorts => [];

        public void Configure(IServiceCollection services) { }
    }

    private sealed class InventoryModuleWithContracts : IBoundedContextModule
    {
        public string ContextName => "Inventory";
        public string? Description => null;

        public IEnumerable<Type> PublishedIntegrationEvents =>
        [
            typeof(InventoryReserved)
        ];

        public IEnumerable<Type> ConsumedIntegrationEvents =>
        [
            typeof(OrderPlaced)
        ];

        public IEnumerable<Type> ExposedPorts => [];

        public void Configure(IServiceCollection services) { }
    }

    private sealed class OrphanConsumerModule : IBoundedContextModule
    {
        public string ContextName => "Orphan";
        public string? Description => null;

        public IEnumerable<Type> PublishedIntegrationEvents => [];

        public IEnumerable<Type> ConsumedIntegrationEvents =>
        [
            typeof(NonExistentEvent)
        ];

        public IEnumerable<Type> ExposedPorts => [];

        public void Configure(IServiceCollection services) { }
    }

    private sealed record NonExistentEvent;

    #endregion

    #region BoundedContextAttribute Tests

    [Fact]
    public void BoundedContextAttribute_SetsContextName()
    {
        // Arrange
        var attribute = new BoundedContextAttribute("TestContext");

        // Act

        // Assert
        attribute.ContextName.ShouldBe("TestContext");
        attribute.Description.ShouldBeNull();
    }

    [Fact]
    public void BoundedContextAttribute_SetsDescription()
    {
        // Arrange
        var attribute = new BoundedContextAttribute("TestContext")
        {
            Description = "Test description"
        };

        // Assert
        attribute.Description.ShouldBe("Test description");
    }

    [Fact]
    public void BoundedContextAttribute_NullContextName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new BoundedContextAttribute(null!));
    }

    [Fact]
    public void BoundedContextAttribute_EmptyContextName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new BoundedContextAttribute(""));
    }

    [Fact]
    public void BoundedContextAttribute_CanBeRetrievedFromType()
    {
        // Act
        var attribute = typeof(TestOrder)
            .GetCustomAttributes(typeof(BoundedContextAttribute), true)
            .FirstOrDefault() as BoundedContextAttribute;

        // Assert
        attribute.ShouldNotBeNull();
        attribute.ContextName.ShouldBe("Orders");
        attribute.Description.ShouldBe("Order management");
    }

    #endregion

    #region ContextRelation Tests

    [Fact]
    public void ContextRelation_CreatesCorrectRecord()
    {
        // Act
        var relation = new ContextRelation(
            "Orders",
            "Shipping",
            ContextRelationship.PublishedLanguage,
            "OrderPlaced event");

        // Assert
        relation.UpstreamContext.ShouldBe("Orders");
        relation.DownstreamContext.ShouldBe("Shipping");
        relation.Relationship.ShouldBe(ContextRelationship.PublishedLanguage);
        relation.Description.ShouldBe("OrderPlaced event");
    }

    #endregion

    #region ContextMap Tests

    [Fact]
    public void ContextMap_AddRelation_AddsRelationship()
    {
        // Arrange
        var map = new ContextMap();

        // Act
        map.AddRelation("Orders", "Shipping", ContextRelationship.PublishedLanguage, "Events");

        // Assert
        map.Relations.Count.ShouldBe(1);
        map.Relations[0].UpstreamContext.ShouldBe("Orders");
    }

    [Fact]
    public void ContextMap_AddRelation_NullUpstream_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            map.AddRelation(null!, "Downstream", ContextRelationship.Conformist));
    }

    [Fact]
    public void ContextMap_AddRelation_NullDownstream_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            map.AddRelation("Upstream", null!, ContextRelationship.Conformist));
    }

    [Fact]
    public void ContextMap_AddSharedKernel_AddsSharedKernelRelation()
    {
        // Arrange
        var map = new ContextMap();

        // Act
        map.AddSharedKernel("Orders", "Billing", "Money");

        // Assert
        map.Relations.Count.ShouldBe(1);
        map.Relations[0].Relationship.ShouldBe(ContextRelationship.SharedKernel);
        map.Relations[0].Description.ShouldBe("Money");
    }

    [Fact]
    public void ContextMap_AddSharedKernel_NullContext1_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            map.AddSharedKernel(null!, "Context2", "Kernel"));
    }

    [Fact]
    public void ContextMap_AddSharedKernel_NullContext2_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            map.AddSharedKernel("Context1", null!, "Kernel"));
    }

    [Fact]
    public void ContextMap_AddSharedKernel_NullKernelName_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            map.AddSharedKernel("Context1", "Context2", null!));
    }

    [Fact]
    public void ContextMap_AddCustomerSupplier_AddsRelation()
    {
        // Arrange
        var map = new ContextMap();

        // Act
        map.AddCustomerSupplier("Inventory", "Orders", "Stock availability");

        // Assert
        map.Relations.Count.ShouldBe(1);
        map.Relations[0].Relationship.ShouldBe(ContextRelationship.CustomerSupplier);
    }

    [Fact]
    public void ContextMap_AddCustomerSupplier_NullSupplier_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            map.AddCustomerSupplier(null!, "Customer", "Description"));
    }

    [Fact]
    public void ContextMap_AddPublishedLanguage_AddsRelation()
    {
        // Arrange
        var map = new ContextMap();

        // Act
        map.AddPublishedLanguage("Orders", "Shipping", "OrderPlaced event");

        // Assert
        map.Relations.Count.ShouldBe(1);
        map.Relations[0].Relationship.ShouldBe(ContextRelationship.PublishedLanguage);
    }

    [Fact]
    public void ContextMap_AddPublishedLanguage_NullPublisher_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            map.AddPublishedLanguage(null!, "Subscriber", "Description"));
    }

    [Fact]
    public void ContextMap_GetContextNames_ReturnsUniqueNames()
    {
        // Arrange
        var map = new ContextMap()
            .AddRelation("Orders", "Shipping", ContextRelationship.PublishedLanguage)
            .AddRelation("Orders", "Billing", ContextRelationship.PublishedLanguage)
            .AddRelation("Inventory", "Orders", ContextRelationship.CustomerSupplier);

        // Act
        var names = map.GetContextNames();

        // Assert
        names.Count.ShouldBe(4);
        names.ShouldContain("Orders");
        names.ShouldContain("Shipping");
        names.ShouldContain("Billing");
        names.ShouldContain("Inventory");
    }

    [Fact]
    public void ContextMap_GetRelationsFor_ReturnsRelatedRelations()
    {
        // Arrange
        var map = new ContextMap()
            .AddRelation("Orders", "Shipping", ContextRelationship.PublishedLanguage)
            .AddRelation("Orders", "Billing", ContextRelationship.PublishedLanguage)
            .AddRelation("Inventory", "Orders", ContextRelationship.CustomerSupplier);

        // Act
        var relations = map.GetRelationsFor("Orders").ToList();

        // Assert
        relations.Count.ShouldBe(3);
    }

    [Fact]
    public void ContextMap_GetRelationsFor_EmptyContextName_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() => map.GetRelationsFor("").ToList());
    }

    [Fact]
    public void ContextMap_GetUpstreamDependencies_ReturnsCorrectRelations()
    {
        // Arrange
        var map = new ContextMap()
            .AddRelation("Inventory", "Orders", ContextRelationship.CustomerSupplier)
            .AddRelation("Pricing", "Orders", ContextRelationship.Conformist);

        // Act
        var upstream = map.GetUpstreamDependencies("Orders").ToList();

        // Assert
        upstream.Count.ShouldBe(2);
        upstream.All(r => r.DownstreamContext == "Orders").ShouldBeTrue();
    }

    [Fact]
    public void ContextMap_GetUpstreamDependencies_EmptyContextName_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() => map.GetUpstreamDependencies("").ToList());
    }

    [Fact]
    public void ContextMap_GetDownstreamConsumers_ReturnsCorrectRelations()
    {
        // Arrange
        var map = new ContextMap()
            .AddRelation("Orders", "Shipping", ContextRelationship.PublishedLanguage)
            .AddRelation("Orders", "Billing", ContextRelationship.PublishedLanguage);

        // Act
        var downstream = map.GetDownstreamConsumers("Orders").ToList();

        // Assert
        downstream.Count.ShouldBe(2);
        downstream.All(r => r.UpstreamContext == "Orders").ShouldBeTrue();
    }

    [Fact]
    public void ContextMap_GetDownstreamConsumers_EmptyContextName_ThrowsArgumentException()
    {
        // Arrange
        var map = new ContextMap();

        // Act & Assert
        Should.Throw<ArgumentException>(() => map.GetDownstreamConsumers("").ToList());
    }

    [Fact]
    public void ContextMap_ToMermaidDiagram_GeneratesValidDiagram()
    {
        // Arrange
        var map = new ContextMap()
            .AddRelation("Orders", "Shipping", ContextRelationship.PublishedLanguage, "Events")
            .AddSharedKernel("Orders", "Billing", "Money");

        // Act
        var diagram = map.ToMermaidDiagram();

        // Assert
        diagram.ShouldContain("flowchart LR");
        diagram.ShouldContain("Orders");
        diagram.ShouldContain("Shipping");
        diagram.ShouldContain("Events");
        diagram.ShouldContain("<-->"); // Shared kernel uses bidirectional arrow
    }

    [Fact]
    public void ContextMap_FluentChaining_WorksCorrectly()
    {
        // Act
        var map = new ContextMap()
            .AddRelation("A", "B", ContextRelationship.Conformist)
            .AddSharedKernel("B", "C", "SharedLib")
            .AddCustomerSupplier("D", "E")
            .AddPublishedLanguage("E", "F");

        // Assert
        map.Relations.Count.ShouldBe(4);
    }

    #endregion

    #region BoundedContextModule Tests

    [Fact]
    public void BoundedContextModule_Properties_ReturnCorrectValues()
    {
        // Arrange/Act
        var context = new TestOrdersContext();

        // Assert
        context.ContextName.ShouldBe("Orders");
        context.Description.ShouldBe("Order management context");
    }

    [Fact]
    public void BoundedContextModule_DefaultDescription_ReturnsNull()
    {
        // Arrange
        var context = new TestInventoryContext();

        // Assert
        context.Description.ShouldBeNull();
    }

    [Fact]
    public void BoundedContextModule_Configure_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var context = new TestOrdersContext();

        // Act
        context.Configure(services);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ITestOrderService>().ShouldNotBeNull();
    }

    #endregion

    #region BoundedContextValidator Tests

    [Fact]
    public void BoundedContextValidator_AddContext_AddsContext()
    {
        // Arrange
        var validator = new BoundedContextValidator();
        var context = new OrdersModuleWithContracts();
        // Act
        var ex = Record.Exception(() => validator.AddContext(context));

        // Assert
        ex.ShouldBeNull();
    }

    [Fact]
    public void BoundedContextValidator_AddContext_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = new BoundedContextValidator();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => validator.AddContext(null!));
    }

    [Fact]
    public void BoundedContextValidator_ValidateEventContracts_AllValid_ReturnsRight()
    {
        // Arrange
        var validator = new BoundedContextValidator()
            .AddContext(new OrdersModuleWithContracts())
            .AddContext(new InventoryModuleWithContracts());

        // Act
        var result = validator.ValidateEventContracts();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void BoundedContextValidator_ValidateEventContracts_OrphanConsumer_ReturnsLeft()
    {
        // Arrange
        var validator = new BoundedContextValidator()
            .AddContext(new OrphanConsumerModule());

        // Act
        var result = validator.ValidateEventContracts();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.ErrorCode.ShouldBe("CONTEXT_VALIDATION_FAILED");
                error.Details.ShouldNotBeNull();
            });
    }

    [Fact]
    public void BoundedContextValidator_GenerateContextMap_CreatesMapFromEvents()
    {
        // Arrange
        var validator = new BoundedContextValidator()
            .AddContext(new OrdersModuleWithContracts())
            .AddContext(new InventoryModuleWithContracts());

        // Act
        var map = validator.GenerateContextMap();

        // Assert
        map.Relations.Count.ShouldBeGreaterThan(0);
        // Orders publishes OrderPlaced, Inventory consumes it
        var ordersToInventory = map.Relations
            .FirstOrDefault(r => r.UpstreamContext == "Orders" && r.DownstreamContext == "Inventory");
        ordersToInventory.ShouldNotBeNull();
    }

    #endregion

    #region BoundedContextError Tests

    [Fact]
    public void BoundedContextError_OrphanedConsumer_CreatesCorrectError()
    {
        // Act
        var error = BoundedContextError.OrphanedConsumer("Orders", typeof(OrderPlaced));

        // Assert
        error.ErrorCode.ShouldBe("CONTEXT_ORPHANED_CONSUMER");
        error.ContextName.ShouldBe("Orders");
        error.Message.ShouldContain("OrderPlaced");
    }

    [Fact]
    public void BoundedContextError_CircularDependency_CreatesCorrectError()
    {
        // Arrange
        var cycle = new List<string> { "A", "B", "C", "A" };

        // Act
        var error = BoundedContextError.CircularDependency(cycle);

        // Assert
        error.ErrorCode.ShouldBe("CONTEXT_CIRCULAR_DEPENDENCY");
        error.Details.ShouldBe(cycle);
    }

    [Fact]
    public void BoundedContextError_ValidationFailed_CreatesCorrectError()
    {
        // Arrange
        var details = new List<string> { "Error 1", "Error 2" };

        // Act
        var error = BoundedContextError.ValidationFailed("Validation failed", details);

        // Assert
        error.ErrorCode.ShouldBe("CONTEXT_VALIDATION_FAILED");
        error.Details.ShouldBe(details);
    }

    #endregion

    #region BoundedContextExtensions Tests

    [Fact]
    public void AddBoundedContext_RegistersContextModule()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBoundedContext<TestOrdersContext>();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<TestOrdersContext>().ShouldNotBeNull();
        provider.GetService<ITestOrderService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddBoundedContext_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddBoundedContext<TestOrdersContext>());
    }

    [Fact]
    public void AddBoundedContextModule_RegistersModuleWithContracts()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBoundedContextModule<OrdersModuleWithContractsNewable>();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IBoundedContextModule>().ShouldNotBeNull();
    }

    [Fact]
    public void AddBoundedContextModule_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddBoundedContextModule<OrdersModuleWithContractsNewable>());
    }

    [Fact]
    public void AddBoundedContext_WithFactory_RegistersContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBoundedContext<TestOrdersContext>(_ => new TestOrdersContext());

        // Assert - factory registration resolved from provider
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<TestOrdersContext>();
        resolved.ShouldNotBeNull();
    }

    [Fact]
    public void AddBoundedContext_WithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddBoundedContext<TestOrdersContext>(_ => new TestOrdersContext()));
    }

    [Fact]
    public void AddBoundedContext_WithFactory_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddBoundedContext<TestOrdersContext>(null!));
    }

    [Fact]
    public void AddBoundedContextsFromAssembly_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = typeof(BoundedContextTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddBoundedContextsFromAssembly(assembly));
    }

    [Fact]
    public void AddBoundedContextsFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddBoundedContextsFromAssembly(null!));
    }

    [Fact]
    public void GetBoundedContextName_ReturnsContextName_WhenAttributed()
    {
        // Act
        var name = typeof(TestOrder).GetBoundedContextName();

        // Assert
        name.ShouldBe("Orders");
    }

    [Fact]
    public void GetBoundedContextName_ReturnsNull_WhenNotAttributed()
    {
        // Act
        var name = typeof(UnattributedClass).GetBoundedContextName();

        // Assert
        name.ShouldBeNull();
    }

    [Fact]
    public void GetBoundedContextName_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? type = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => type!.GetBoundedContextName());
    }

    [Fact]
    public void GetTypesInBoundedContext_ReturnsTypesInContext()
    {
        // Arrange
        var assembly = typeof(BoundedContextTests).Assembly;

        // Act
        var types = assembly.GetTypesInBoundedContext("Orders").ToList();

        // Assert
        types.ShouldContain(typeof(TestOrder));
    }

    [Fact]
    public void GetTypesInBoundedContext_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        System.Reflection.Assembly? assembly = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            assembly!.GetTypesInBoundedContext("Orders").ToList());
    }

    [Fact]
    public void GetTypesInBoundedContext_EmptyContextName_ThrowsArgumentException()
    {
        // Arrange
        var assembly = typeof(BoundedContextTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            assembly.GetTypesInBoundedContext("").ToList());
    }

    // Helper class that can be instantiated (has parameterless constructor)
    private sealed class OrdersModuleWithContractsNewable : IBoundedContextModule
    {
        public string ContextName => "Orders";
        public string? Description => null;
        public IEnumerable<Type> PublishedIntegrationEvents => [];
        public IEnumerable<Type> ConsumedIntegrationEvents => [];
        public IEnumerable<Type> ExposedPorts => [];
        public void Configure(IServiceCollection services) { }
    }

    #endregion
}
