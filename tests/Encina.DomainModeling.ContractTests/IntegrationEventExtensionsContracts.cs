using System.Reflection;
using Shouldly;

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
        _asyncMapperType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_MustBeGenericWithTwoParameters()
    {
        _asyncMapperType.IsGenericTypeDefinition.ShouldBeTrue();
        _asyncMapperType.GetGenericArguments().Length.ShouldBe(2);
    }

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_TDomainEvent_MustHaveIDomainEventConstraint()
    {
        var typeParam = _asyncMapperType.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();
        constraints.ShouldContain(typeof(IDomainEvent));
    }

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_TIntegrationEvent_MustHaveIIntegrationEventConstraint()
    {
        var typeParam = _asyncMapperType.GetGenericArguments()[1];
        var constraints = typeParam.GetGenericParameterConstraints();
        constraints.ShouldContain(typeof(IIntegrationEvent));
    }

    [Fact]
    public void IAsyncDomainEventToIntegrationEventMapper_MustHaveMapAsyncMethod()
    {
        var method = _asyncMapperType.GetMethod("MapAsync");
        method.ShouldNotBeNull();
    }

    // === IFallibleDomainEventToIntegrationEventMapper<TDomainEvent, TIntegrationEvent, TError> ===

    [Fact]
    public void IFallibleDomainEventToIntegrationEventMapper_MustBeInterface()
    {
        _fallibleMapperType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IFallibleDomainEventToIntegrationEventMapper_MustBeGenericWithThreeParameters()
    {
        _fallibleMapperType.IsGenericTypeDefinition.ShouldBeTrue();
        _fallibleMapperType.GetGenericArguments().Length.ShouldBe(3);
    }

    [Fact]
    public void IFallibleDomainEventToIntegrationEventMapper_MustHaveMapAsyncMethod()
    {
        var method = _fallibleMapperType.GetMethod("MapAsync");
        method.ShouldNotBeNull();
    }

    // === IIntegrationEventPublisher ===

    [Fact]
    public void IIntegrationEventPublisher_MustBeInterface()
    {
        _publisherType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IIntegrationEventPublisher_MustHavePublishAsyncMethod()
    {
        var method = _publisherType.GetMethod("PublishAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IIntegrationEventPublisher_MustHavePublishManyAsyncMethod()
    {
        var method = _publisherType.GetMethod("PublishManyAsync");
        method.ShouldNotBeNull();
    }

    // === IFallibleIntegrationEventPublisher<TError> ===

    [Fact]
    public void IFallibleIntegrationEventPublisher_MustBeInterface()
    {
        _falliblePublisherType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IFallibleIntegrationEventPublisher_MustBeGenericWithOneParameter()
    {
        _falliblePublisherType.IsGenericTypeDefinition.ShouldBeTrue();
        _falliblePublisherType.GetGenericArguments().Length.ShouldBe(1);
    }

    [Fact]
    public void IFallibleIntegrationEventPublisher_MustHavePublishAsyncMethod()
    {
        var method = _falliblePublisherType.GetMethod("PublishAsync");
        method.ShouldNotBeNull();
    }

    // === IntegrationEventMappingError ===

    [Fact]
    public void IntegrationEventMappingError_MustBeRecord()
    {
        _mappingErrorType.GetMethod("<Clone>$").ShouldNotBeNull();
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveMessageProperty()
    {
        var property = _mappingErrorType.GetProperty("Message");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveErrorCodeProperty()
    {
        var property = _mappingErrorType.GetProperty("ErrorCode");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveDomainEventTypeProperty()
    {
        var property = _mappingErrorType.GetProperty("DomainEventType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Type));
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveIntegrationEventTypeProperty()
    {
        var property = _mappingErrorType.GetProperty("IntegrationEventType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Type));
    }

    [Fact]
    public void IntegrationEventMappingError_MustHaveFactoryMethods()
    {
        var methods = _mappingErrorType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        methods.ShouldContain("MissingField");
        methods.ShouldContain("ValidationFailed");
        methods.ShouldContain("LookupFailed");
    }

    // === IntegrationEventPublishError ===

    [Fact]
    public void IntegrationEventPublishError_MustBeRecord()
    {
        _publishErrorType.GetMethod("<Clone>$").ShouldNotBeNull();
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveMessageProperty()
    {
        var property = _publishErrorType.GetProperty("Message");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveErrorCodeProperty()
    {
        var property = _publishErrorType.GetProperty("ErrorCode");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveEventTypeProperty()
    {
        var property = _publishErrorType.GetProperty("EventType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Type));
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveEventIdProperty()
    {
        var property = _publishErrorType.GetProperty("EventId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Guid));
    }

    [Fact]
    public void IntegrationEventPublishError_MustHaveFactoryMethods()
    {
        var methods = _publishErrorType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        methods.ShouldContain("SerializationFailed");
        methods.ShouldContain("OutboxStoreFailed");
        methods.ShouldContain("BrokerFailed");
    }

    // === IntegrationEventMappingExtensions ===

    [Fact]
    public void IntegrationEventMappingExtensions_MustBeStaticClass()
    {
        _extensionsType.IsAbstract.ShouldBeTrue();
        _extensionsType.IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveMapToMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "MapTo");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveMapToAsyncMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "MapToAsync");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveMapAllMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "MapAll");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveMapAllAsyncMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "MapAllAsync");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveTryMapToMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "TryMapTo");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void IntegrationEventMappingExtensions_MustHaveComposeMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "Compose");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }
}
