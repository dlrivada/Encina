using System.Reflection;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests for integration event extension types.
/// </summary>
public class IntegrationEventExtensionsContracts
{
    private readonly Type _asyncMapperType = typeof(IAsyncDomainEventToIntegrationEventMapper<,>);
    private readonly Type _fallibleMapperType = typeof(IFallibleDomainEventToIntegrationEventMapper<,,>);
    private readonly Type _publisherType = typeof(IIntegrationEventPublisher);
    private readonly Type _falliblePublisherType = typeof(IFallibleIntegrationEventPublisher<>);
    private readonly Type _mappingErrorType = typeof(IntegrationEventMappingError);
    private readonly Type _publishErrorType = typeof(IntegrationEventPublishError);
    private readonly Type _extensionsType = typeof(IntegrationEventMappingExtensions);

    // === IAsyncDomainEventToIntegrationEventMapper<TDomainEvent, TIntegrationEvent> ===

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_MustBeInterface()
    {
        _asyncMapperType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_MustBeGenericWithTwoParameters()
    {
        _asyncMapperType.IsGenericTypeDefinition.Should().BeTrue();
        _asyncMapperType.GetGenericArguments().Should().HaveCount(2);
    }

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_TDomainEvent_MustHaveIDomainEventConstraint()
    {
        var typeParam = _asyncMapperType.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();
        constraints.Should().Contain(typeof(IDomainEvent));
    }

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_TIntegrationEvent_MustHaveIIntegrationEventConstraint()
    {
        var typeParam = _asyncMapperType.GetGenericArguments()[1];
        var constraints = typeParam.GetGenericParameterConstraints();
        constraints.Should().Contain(typeof(IIntegrationEvent));
    }

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_MustHaveMapAsyncMethod()
    {
        var method = _asyncMapperType.GetMethod("MapAsync");
        method.Should().NotBeNull();
    }

    // === IFallibleDomainEventToIntegrationEventMapper<TDomainEvent, TIntegrationEvent, TError> ===

    [Fact]
    public void IFallibleDomainEventToIntegrationEventMapper_MustBeInterface()
    {
        _fallibleMapperType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IFallibleDomainEventToIntegrationEventMapper_MustBeGenericWithThreeParameters()
    {
        _fallibleMapperType.IsGenericTypeDefinition.Should().BeTrue();
        _fallibleMapperType.GetGenericArguments().Should().HaveCount(3);
    }

    [Fact]
    public void IFallibleDomainEventToIntegrationEventMapper_MustHaveMapAsyncMethod()
    {
        var method = _fallibleMapperType.GetMethod("MapAsync");
        method.Should().NotBeNull();
    }

    // === IIntegrationEventPublisher ===

    [Fact]
    public void IIntegrationEventPublisher_MustBeInterface()
    {
        _publisherType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IIntegrationEventPublisher_MustHavePublishAsyncMethod()
    {
        var method = _publisherType.GetMethod("PublishAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IIntegrationEventPublisher_MustHavePublishManyAsyncMethod()
    {
        var method = _publisherType.GetMethod("PublishManyAsync");
        method.Should().NotBeNull();
    }

    // === IFallibleIntegrationEventPublisher<TError> ===

    [Fact]
    public void IFallibleIntegrationEventPublisher_MustBeInterface()
    {
        _falliblePublisherType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IFallibleIntegrationEventPublisher_MustBeGenericWithOneParameter()
    {
        _falliblePublisherType.IsGenericTypeDefinition.Should().BeTrue();
        _falliblePublisherType.GetGenericArguments().Should().HaveCount(1);
    }

    [Fact]
    public void IFallibleIntegrationEventPublisher_MustHavePublishAsyncMethod()
    {
        var method = _falliblePublisherType.GetMethod("PublishAsync");
        method.Should().NotBeNull();
    }

    // === IntegrationEventMappingError ===

    [Fact]
    public void IntegrationEventMappingError_MustBeRecord()
    {
        _mappingErrorType.GetMethod("<Clone>$").Should().NotBeNull();
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveMessageProperty()
    {
        var property = _mappingErrorType.GetProperty("Message");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveErrorCodeProperty()
    {
        var property = _mappingErrorType.GetProperty("ErrorCode");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveDomainEventTypeProperty()
    {
        var property = _mappingErrorType.GetProperty("DomainEventType");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Type>();
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveIntegrationEventTypeProperty()
    {
        var property = _mappingErrorType.GetProperty("IntegrationEventType");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Type>();
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveFactoryMethods()
    {
        var methods = _mappingErrorType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        methods.Should().Contain("MissingField");
        methods.Should().Contain("ValidationFailed");
        methods.Should().Contain("LookupFailed");
    }

    // === IntegrationEventPublishError ===

    [Fact]
    public void IntegrationEventPublishError_MustBeRecord()
    {
        _publishErrorType.GetMethod("<Clone>$").Should().NotBeNull();
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveMessageProperty()
    {
        var property = _publishErrorType.GetProperty("Message");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveErrorCodeProperty()
    {
        var property = _publishErrorType.GetProperty("ErrorCode");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveEventTypeProperty()
    {
        var property = _publishErrorType.GetProperty("EventType");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Type>();
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveEventIdProperty()
    {
        var property = _publishErrorType.GetProperty("EventId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Guid>();
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveFactoryMethods()
    {
        var methods = _publishErrorType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        methods.Should().Contain("SerializationFailed");
        methods.Should().Contain("OutboxStoreFailed");
        methods.Should().Contain("BrokerFailed");
    }

    // === IntegrationEventMappingExtensions ===

    [Fact]
    public void IntegrationEventMappingExtensions_MustBeStaticClass()
    {
        _extensionsType.IsAbstract.Should().BeTrue();
        _extensionsType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveMapToMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "MapTo");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveMapToAsyncMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "MapToAsync");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveMapAllMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "MapAll");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveMapAllAsyncMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "MapAllAsync");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveTryMapToMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "TryMapTo");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveComposeMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "Compose");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }
}
