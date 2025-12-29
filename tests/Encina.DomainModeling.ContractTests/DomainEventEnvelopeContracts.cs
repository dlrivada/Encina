using System.Reflection;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests for DomainEventEnvelope and related types.
/// </summary>
public class DomainEventEnvelopeContracts
{
    private readonly Type _envelopeType = typeof(DomainEventEnvelope<>);
    private readonly Type _metadataInterfaceType = typeof(IDomainEventMetadata);
    private readonly Type _metadataType = typeof(DomainEventMetadata);
    private readonly Type _staticEnvelopeType = typeof(DomainEventEnvelope);
    private readonly Type _extensionsType = typeof(DomainEventExtensions);

    // === IDomainEventMetadata Interface Contract ===

    [Fact]
    public void IDomainEventMetadata_MustBeInterface()
    {
        _metadataInterfaceType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveCorrelationIdProperty()
    {
        var property = _metadataInterfaceType.GetProperty("CorrelationId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveCausationIdProperty()
    {
        var property = _metadataInterfaceType.GetProperty("CausationId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveUserIdProperty()
    {
        var property = _metadataInterfaceType.GetProperty("UserId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveTenantIdProperty()
    {
        var property = _metadataInterfaceType.GetProperty("TenantId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveAdditionalMetadataProperty()
    {
        var property = _metadataInterfaceType.GetProperty("AdditionalMetadata");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
    }

    // === DomainEventMetadata Contract ===

    [Fact]
    public void DomainEventMetadata_MustImplementIDomainEventMetadata()
    {
        _metadataType.Should().Implement<IDomainEventMetadata>();
    }

    [Fact]
    public void DomainEventMetadata_MustBeRecord()
    {
        // Records have a special <Clone>$ method
        _metadataType.GetMethod("<Clone>$").Should().NotBeNull();
    }

    [Fact]
    public void DomainEventMetadata_MustHaveEmptyStaticProperty()
    {
        var property = _metadataType.GetProperty("Empty", BindingFlags.Static | BindingFlags.Public);
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DomainEventMetadata>();
    }

    [Fact]
    public void DomainEventMetadata_MustHaveWithCorrelationFactory()
    {
        var method = _metadataType.GetMethod("WithCorrelation", BindingFlags.Static | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<DomainEventMetadata>();
    }

    [Fact]
    public void DomainEventMetadata_MustHaveWithCausationFactory()
    {
        var method = _metadataType.GetMethod("WithCausation", BindingFlags.Static | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<DomainEventMetadata>();
    }

    // === DomainEventEnvelope<T> Contract ===

    [Fact]
    public void DomainEventEnvelope_MustBeGeneric()
    {
        _envelopeType.IsGenericTypeDefinition.Should().BeTrue();
        _envelopeType.GetGenericArguments().Should().HaveCount(1);
    }

    [Fact]
    public void DomainEventEnvelope_GenericParameter_MustHaveIDomainEventConstraint()
    {
        var typeParam = _envelopeType.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();
        constraints.Should().Contain(typeof(IDomainEvent));
    }

    [Fact]
    public void DomainEventEnvelope_MustHaveEventProperty()
    {
        var property = _envelopeType.GetProperty("Event");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void DomainEventEnvelope_MustHaveMetadataProperty()
    {
        var property = _envelopeType.GetProperty("Metadata");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void DomainEventEnvelope_MustHaveEnvelopeIdProperty()
    {
        var property = _envelopeType.GetProperty("EnvelopeId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Guid>();
    }

    [Fact]
    public void DomainEventEnvelope_MustHaveEnvelopeCreatedAtUtcProperty()
    {
        var property = _envelopeType.GetProperty("EnvelopeCreatedAtUtc");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DateTime>();
    }

    // === Static DomainEventEnvelope Factory ===

    [Fact]
    public void DomainEventEnvelope_Static_MustHaveCreateMethodWithMetadata()
    {
        var methods = _staticEnvelopeType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == "Create")
            .ToList();

        methods.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void DomainEventEnvelope_Static_MustHaveWithCorrelationMethod()
    {
        var method = _staticEnvelopeType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "WithCorrelation");

        method.Should().NotBeNull();
    }

    // === DomainEventExtensions Contract ===

    [Fact]
    public void DomainEventExtensions_MustBeStaticClass()
    {
        _extensionsType.IsAbstract.Should().BeTrue();
        _extensionsType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void DomainEventExtensions_MustHaveToEnvelopeMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "ToEnvelope");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void DomainEventExtensions_MustHaveWithMetadataMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "WithMetadata");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void DomainEventExtensions_MustHaveWithCorrelationMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "WithCorrelation");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void DomainEventExtensions_MustHaveMapMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "Map");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }
}
