using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying DomainEvent and IntegrationEvent public API contracts.
/// </summary>
public sealed class DomainEventContracts
{
    private readonly Type _domainEventType = typeof(DomainEvent);
    private readonly Type _richDomainEventType = typeof(RichDomainEvent);
    private readonly Type _integrationEventType = typeof(IntegrationEvent);

    #region IDomainEvent Contracts

    [Fact]
    public void IDomainEvent_MustHaveEventIdProperty()
    {
        var property = typeof(IDomainEvent).GetProperty("EventId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Guid>();
    }

    [Fact]
    public void IDomainEvent_MustHaveOccurredAtUtcProperty()
    {
        var property = typeof(IDomainEvent).GetProperty("OccurredAtUtc");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DateTime>();
    }

    #endregion

    #region DomainEvent Contracts

    [Fact]
    public void DomainEvent_MustImplementIDomainEvent()
    {
        _domainEventType.GetInterfaces()
            .Should().Contain(typeof(IDomainEvent));
    }

    [Fact]
    public void DomainEvent_MustBeRecord()
    {
        _domainEventType.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public)
            .Should().NotBeNull();
    }

    [Fact]
    public void DomainEvent_MustBeAbstract()
    {
        _domainEventType.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void DomainEvent_MustHaveEventIdProperty()
    {
        var property = _domainEventType.GetProperty("EventId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Guid>();
    }

    [Fact]
    public void DomainEvent_MustHaveOccurredAtUtcProperty()
    {
        var property = _domainEventType.GetProperty("OccurredAtUtc");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DateTime>();
    }

    #endregion

    #region RichDomainEvent Contracts

    [Fact]
    public void RichDomainEvent_MustInheritFromDomainEvent()
    {
        _richDomainEventType.BaseType.Should().Be(_domainEventType);
    }

    [Fact]
    public void RichDomainEvent_MustHaveCorrelationIdProperty()
    {
        var property = _richDomainEventType.GetProperty("CorrelationId");
        property.Should().NotBeNull();
    }

    [Fact]
    public void RichDomainEvent_MustHaveCausationIdProperty()
    {
        var property = _richDomainEventType.GetProperty("CausationId");
        property.Should().NotBeNull();
    }

    [Fact]
    public void RichDomainEvent_MustHaveAggregateIdProperty()
    {
        var property = _richDomainEventType.GetProperty("AggregateId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Guid>();
    }

    [Fact]
    public void RichDomainEvent_MustHaveAggregateVersionProperty()
    {
        var property = _richDomainEventType.GetProperty("AggregateVersion");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<long>();
    }

    [Fact]
    public void RichDomainEvent_MustHaveEventVersionProperty()
    {
        var property = _richDomainEventType.GetProperty("EventVersion");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<int>();
    }

    #endregion

    #region IIntegrationEvent Contracts

    [Fact]
    public void IIntegrationEvent_MustHaveEventIdProperty()
    {
        var property = typeof(IIntegrationEvent).GetProperty("EventId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Guid>();
    }

    [Fact]
    public void IIntegrationEvent_MustHaveOccurredAtUtcProperty()
    {
        var property = typeof(IIntegrationEvent).GetProperty("OccurredAtUtc");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DateTime>();
    }

    [Fact]
    public void IIntegrationEvent_MustHaveCorrelationIdProperty()
    {
        var property = typeof(IIntegrationEvent).GetProperty("CorrelationId");
        property.Should().NotBeNull();
    }

    [Fact]
    public void IIntegrationEvent_MustHaveEventVersionProperty()
    {
        var property = typeof(IIntegrationEvent).GetProperty("EventVersion");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<int>();
    }

    #endregion

    #region IntegrationEvent Contracts

    [Fact]
    public void IntegrationEvent_MustImplementIIntegrationEvent()
    {
        _integrationEventType.GetInterfaces()
            .Should().Contain(typeof(IIntegrationEvent));
    }

    [Fact]
    public void IntegrationEvent_MustBeRecord()
    {
        _integrationEventType.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public)
            .Should().NotBeNull();
    }

    [Fact]
    public void IntegrationEvent_MustBeAbstract()
    {
        _integrationEventType.IsAbstract.Should().BeTrue();
    }

    #endregion

    #region Mapper Contracts

    [Fact]
    public void IDomainEventToIntegrationEventMapper_MustHaveMapMethod()
    {
        var mapperType = typeof(IDomainEventToIntegrationEventMapper<,>);
        var method = mapperType.GetMethod("Map");
        method.Should().NotBeNull();
    }

    #endregion
}
