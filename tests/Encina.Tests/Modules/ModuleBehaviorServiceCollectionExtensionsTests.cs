using Encina.Modules;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Modules;

public sealed class ModuleBehaviorServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaModuleBehavior_RegistersBehaviorDescriptor()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new OrderModule();

        services.AddEncinaModules(config => config.AddModule(module));

        // Act
        services.AddEncinaModuleBehavior<OrderModule, CreateOrderRequest, string, OrderAuditBehavior>();

        // Assert - Verify the service descriptors are registered
        var behaviorDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<CreateOrderRequest, string>));

        behaviorDescriptor.ShouldNotBeNull();
        behaviorDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaModuleBehavior_RegistersConcreteType()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new OrderModule();

        services.AddEncinaModules(config => config.AddModule(module));

        // Act
        services.AddEncinaModuleBehavior<OrderModule, CreateOrderRequest, string, OrderAuditBehavior>();

        // Assert - Verify the concrete behavior type is registered
        var concreteDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(OrderAuditBehavior));

        concreteDescriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaModuleBehavior_WithLifetime_RegistersWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new OrderModule();

        services.AddEncinaModules(config => config.AddModule(module));

        // Act
        services.AddEncinaModuleBehavior<OrderModule, CreateOrderRequest, string, OrderAuditBehavior>(
            ServiceLifetime.Singleton);

        // Assert
        var behaviorDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(OrderAuditBehavior));

        behaviorDescriptor.ShouldNotBeNull();
        behaviorDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaModuleBehavior_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaModuleBehavior<OrderModule, CreateOrderRequest, string, OrderAuditBehavior>());
    }

    [Fact]
    public async Task ModuleBehavior_ExecutesForModuleHandlers()
    {
        // Arrange
        var auditLog = new TestAuditLog();
        var module = new OrderModule();
        var registry = CreateRegistryReturningTrue();

        var behavior = new OrderAuditBehavior(auditLog);
        var adapter = new ModuleBehaviorAdapter<OrderModule, CreateOrderRequest, string>(
            behavior, module, registry);

        var request = new CreateOrderRequest();
        var context = RequestContext.Create();
        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("order-created"));

        // Act
        var result = await adapter.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        auditLog.Entries.ShouldContain("Order operation started");
        auditLog.Entries.ShouldContain("Order operation completed");
    }

    [Fact]
    public async Task ModuleBehavior_DoesNotExecuteForNonModuleHandlers()
    {
        // Arrange
        var auditLog = new TestAuditLog();
        var module = new OrderModule();
        var registry = CreateRegistryReturningFalse();

        var behavior = new OtherAuditBehavior(auditLog);
        var adapter = new ModuleBehaviorAdapter<OrderModule, OtherRequest, string>(
            behavior, module, registry);

        var request = new OtherRequest();
        var context = RequestContext.Create();
        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("other-handled"));

        // Act
        var result = await adapter.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.ShouldBe("other-handled"));
        // The behavior should NOT have executed because handler is not in the module
        auditLog.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void AddEncinaModuleBehavior_CanRegisterMultipleBehaviorsForSameModule()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new OrderModule();

        services.AddEncinaModules(config => config.AddModule(module));

        // Act
        services.AddEncinaModuleBehavior<OrderModule, CreateOrderRequest, string, OrderAuditBehavior>();
        services.AddEncinaModuleBehavior<OrderModule, CreateOrderRequest, string, OrderLoggingBehavior>();

        // Assert - Both behaviors should be registered
        var behaviorDescriptors = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<CreateOrderRequest, string>))
            .ToList();

        behaviorDescriptors.Count.ShouldBe(2);
    }

    #region Helpers

    private static IModuleHandlerRegistry CreateRegistryReturningTrue()
    {
        var registry = Substitute.For<IModuleHandlerRegistry>();
        registry.BelongsToModule(Arg.Any<Type>(), Arg.Any<string>())
            .Returns(true);
        return registry;
    }

    private static IModuleHandlerRegistry CreateRegistryReturningFalse()
    {
        var registry = Substitute.For<IModuleHandlerRegistry>();
        registry.BelongsToModule(Arg.Any<Type>(), Arg.Any<string>())
            .Returns(false);
        return registry;
    }

    #endregion

    #region Test Fixtures

    public sealed class OrderModule : IModule
    {
        public string Name => "Orders";
        public void ConfigureServices(IServiceCollection services) { }
    }

    public sealed record CreateOrderRequest : IRequest<string>;

    public sealed record OtherRequest : IRequest<string>;

    public sealed class CreateOrderHandler : IRequestHandler<CreateOrderRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(
            CreateOrderRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("order-created"));
        }
    }

    public sealed class OtherHandler : IRequestHandler<OtherRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(
            OtherRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("other-handled"));
        }
    }

    public sealed class TestAuditLog
    {
        public List<string> Entries { get; } = [];

        public void Log(string entry) => Entries.Add(entry);
    }

    public sealed class OrderAuditBehavior : IModulePipelineBehavior<OrderModule, CreateOrderRequest, string>
    {
        private readonly TestAuditLog _auditLog;

        public OrderAuditBehavior(TestAuditLog auditLog)
        {
            _auditLog = auditLog;
        }

        public async ValueTask<Either<EncinaError, string>> Handle(
            CreateOrderRequest request,
            IRequestContext context,
            RequestHandlerCallback<string> nextStep,
            CancellationToken cancellationToken)
        {
            _auditLog.Log("Order operation started");
            var result = await nextStep();
            _auditLog.Log("Order operation completed");
            return result;
        }
    }

    public sealed class OtherAuditBehavior : IModulePipelineBehavior<OrderModule, OtherRequest, string>
    {
        private readonly TestAuditLog _auditLog;

        public OtherAuditBehavior(TestAuditLog auditLog)
        {
            _auditLog = auditLog;
        }

        public async ValueTask<Either<EncinaError, string>> Handle(
            OtherRequest request,
            IRequestContext context,
            RequestHandlerCallback<string> nextStep,
            CancellationToken cancellationToken)
        {
            _auditLog.Log("Other operation started");
            var result = await nextStep();
            _auditLog.Log("Other operation completed");
            return result;
        }
    }

    public sealed class OrderLoggingBehavior : IModulePipelineBehavior<OrderModule, CreateOrderRequest, string>
    {
        public async ValueTask<Either<EncinaError, string>> Handle(
            CreateOrderRequest request,
            IRequestContext context,
            RequestHandlerCallback<string> nextStep,
            CancellationToken cancellationToken)
        {
            return await nextStep();
        }
    }

    #endregion
}
