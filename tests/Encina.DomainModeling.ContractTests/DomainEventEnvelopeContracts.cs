using System.Reflection;
using Shouldly;

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
        _metadataInterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveCorrelationIdProperty()
    {
        var property = _metadataInterfaceType.GetProperty("CorrelationId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveCausationIdProperty()
    {
        var property = _metadataInterfaceType.GetProperty("CausationId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveUserIdProperty()
    {
        var property = _metadataInterfaceType.GetProperty("UserId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveTenantIdProperty()
    {
        var property = _metadataInterfaceType.GetProperty("TenantId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void IDomainEventMetadata_MustHaveAdditionalMetadataProperty()
    {
        var property = _metadataInterfaceType.GetProperty("AdditionalMetadata");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    // === DomainEventMetadata Contract ===

    [Fact]
    public void DomainEventMetadata_MustImplementIDomainEventMetadata()
    {
        _metadataType.GetInterfaces().ShouldContain(typeof(IDomainEventMetadata));
    }

    [Fact]
    public void DomainEventMetadata_MustBeRecord()
    {
        // Records have a special <Clone>$ method
        _metadataType.GetMethod("<Clone>$").ShouldNotBeNull();
    }

    [Fact]
    public void DomainEventMetadata_MustHaveEmptyStaticProperty()
    {
        var property = _metadataType.GetProperty("Empty", BindingFlags.Static | BindingFlags.Public);
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(DomainEventMetadata));
    }

    [Fact]
    public void DomainEventMetadata_MustHaveWithCorrelationFactory()
    {
        var method = _metadataType.GetMethod("WithCorrelation", BindingFlags.Static | BindingFlags.Public);
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(DomainEventMetadata));
    }

    [Fact]
    public void DomainEventMetadata_MustHaveWithCausationFactory()
    {
        var method = _metadataType.GetMethod("WithCausation", BindingFlags.Static | BindingFlags.Public);
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(DomainEventMetadata));
    }

    // === DomainEventEnvelope<T> Contract ===

    [Fact]
    public void DomainEventEnvelope_MustBeGeneric()
    {
        _envelopeType.IsGenericTypeDefinition.ShouldBeTrue();
        _envelopeType.GetGenericArguments().Length.ShouldBe(1);
    }

    [Fact]
    public void DomainEventEnvelope_GenericParameter_MustHaveIDomainEventConstraint()
    {
        var typeParam = _envelopeType.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();
        constraints.ShouldContain(typeof(IDomainEvent));
    }

    [Fact]
    public void DomainEventEnvelope_MustHaveEventProperty()
    {
        var property = _envelopeType.GetProperty("Event");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void DomainEventEnvelope_MustHaveMetadataProperty()
    {
        var property = _envelopeType.GetProperty("Metadata");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void DomainEventEnvelope_MustHaveEnvelopeIdProperty()
    {
        var property = _envelopeType.GetProperty("EnvelopeId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Guid));
    }

    [Fact]
    public void DomainEventEnvelope_MustHaveEnvelopeCreatedAtUtcProperty()
    {
        var property = _envelopeType.GetProperty("EnvelopeCreatedAtUtc");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(DateTime));
    }

    // === Static DomainEventEnvelope Factory ===

    [Fact]
    public void DomainEventEnvelope_Static_MustHaveCreateMethodWithMetadata()
    {
        var methods = _staticEnvelopeType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == "Create")
            .ToList();

        methods.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void DomainEventEnvelope_Static_MustHaveWithCorrelationMethod()
    {
        var method = _staticEnvelopeType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "WithCorrelation");

        method.ShouldNotBeNull();
    }

    // === DomainEventExtensions Contract ===

    [Fact]
    public void DomainEventExtensions_MustBeStaticClass()
    {
        _extensionsType.IsAbstract.ShouldBeTrue();
        _extensionsType.IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void DomainEventExtensions_MustHaveToEnvelopeMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "ToEnvelope");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void DomainEventExtensions_MustHaveWithMetadataMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "WithMetadata");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void DomainEventExtensions_MustHaveWithCorrelationMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "WithCorrelation");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void DomainEventExtensions_MustHaveMapMethod()
    {
        var method = _extensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "Map");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }
}
