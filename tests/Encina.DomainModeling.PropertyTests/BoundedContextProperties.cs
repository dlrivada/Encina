using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for bounded context patterns.
/// </summary>
public class BoundedContextProperties
{
    // Helper to check if string is valid (non-whitespace)
    private static bool IsValid(NonEmptyString s) => !string.IsNullOrWhiteSpace(s.Get);
    private static bool AllValid(params NonEmptyString[] strings) => strings.All(IsValid);

    // === ContextMap Properties ===

    [Property(MaxTest = 100)]
    public bool ContextMap_AddRelation_IncreasesRelationCount(
        NonEmptyString upstream,
        NonEmptyString downstream)
    {
        if (!AllValid(upstream, downstream)) return true;

        var map = new ContextMap();
        var initialCount = map.Relations.Count;

        map.AddRelation(upstream.Get, downstream.Get, ContextRelationship.CustomerSupplier);

        return map.Relations.Count == initialCount + 1;
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_AddRelation_StoresCorrectValues(
        NonEmptyString upstream,
        NonEmptyString downstream,
        NonEmptyString description)
    {
        if (!AllValid(upstream, downstream, description)) return true;

        var map = new ContextMap();
        map.AddRelation(upstream.Get, downstream.Get, ContextRelationship.AntiCorruptionLayer, description.Get);

        var relation = map.Relations[0];
        return relation.UpstreamContext == upstream.Get
            && relation.DownstreamContext == downstream.Get
            && relation.Relationship == ContextRelationship.AntiCorruptionLayer
            && relation.Description == description.Get;
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_AddSharedKernel_StoresCorrectRelationship(
        NonEmptyString context1,
        NonEmptyString context2,
        NonEmptyString kernelName)
    {
        if (!AllValid(context1, context2, kernelName)) return true;

        var map = new ContextMap();
        map.AddSharedKernel(context1.Get, context2.Get, kernelName.Get);

        var relation = map.Relations[0];
        return relation.Relationship == ContextRelationship.SharedKernel
            && relation.Description == kernelName.Get;
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_AddCustomerSupplier_StoresCorrectRelationship(
        NonEmptyString supplier,
        NonEmptyString customer)
    {
        if (!AllValid(supplier, customer)) return true;

        var map = new ContextMap();
        map.AddCustomerSupplier(supplier.Get, customer.Get);

        var relation = map.Relations[0];
        return relation.UpstreamContext == supplier.Get
            && relation.DownstreamContext == customer.Get
            && relation.Relationship == ContextRelationship.CustomerSupplier;
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_AddPublishedLanguage_StoresCorrectRelationship(
        NonEmptyString publisher,
        NonEmptyString subscriber)
    {
        if (!AllValid(publisher, subscriber)) return true;

        var map = new ContextMap();
        map.AddPublishedLanguage(publisher.Get, subscriber.Get);

        var relation = map.Relations[0];
        return relation.UpstreamContext == publisher.Get
            && relation.DownstreamContext == subscriber.Get
            && relation.Relationship == ContextRelationship.PublishedLanguage;
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_GetContextNames_ContainsBothContexts(
        NonEmptyString upstream,
        NonEmptyString downstream)
    {
        if (!AllValid(upstream, downstream)) return true;

        var map = new ContextMap();
        map.AddRelation(upstream.Get, downstream.Get, ContextRelationship.Conformist);

        var names = map.GetContextNames();
        return names.Contains(upstream.Get) && names.Contains(downstream.Get);
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_GetRelationsFor_ReturnsMatchingRelations(
        NonEmptyString contextA,
        NonEmptyString contextB,
        NonEmptyString contextC)
    {
        if (!AllValid(contextA, contextB, contextC)) return true;
        if (contextA.Get == contextB.Get || contextB.Get == contextC.Get || contextA.Get == contextC.Get)
            return true; // Skip duplicate names

        var map = new ContextMap();
        map.AddRelation(contextA.Get, contextB.Get, ContextRelationship.CustomerSupplier);
        map.AddRelation(contextB.Get, contextC.Get, ContextRelationship.PublishedLanguage);

        var relationsForB = map.GetRelationsFor(contextB.Get).ToList();
        return relationsForB.Count == 2;
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_GetUpstreamDependencies_ReturnsCorrectRelations(
        NonEmptyString upstream,
        NonEmptyString downstream)
    {
        if (!AllValid(upstream, downstream)) return true;

        var map = new ContextMap();
        map.AddRelation(upstream.Get, downstream.Get, ContextRelationship.CustomerSupplier);

        var upstreamDeps = map.GetUpstreamDependencies(downstream.Get).ToList();
        return upstreamDeps.Count == 1
            && upstreamDeps[0].UpstreamContext == upstream.Get;
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_GetDownstreamConsumers_ReturnsCorrectRelations(
        NonEmptyString upstream,
        NonEmptyString downstream)
    {
        if (!AllValid(upstream, downstream)) return true;

        var map = new ContextMap();
        map.AddRelation(upstream.Get, downstream.Get, ContextRelationship.CustomerSupplier);

        var downstreamConsumers = map.GetDownstreamConsumers(upstream.Get).ToList();
        return downstreamConsumers.Count == 1
            && downstreamConsumers[0].DownstreamContext == downstream.Get;
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_ToMermaidDiagram_ContainsContextNames(
        NonEmptyString upstream,
        NonEmptyString downstream)
    {
        if (!AllValid(upstream, downstream)) return true;

        var map = new ContextMap();
        map.AddRelation(upstream.Get, downstream.Get, ContextRelationship.CustomerSupplier);

        var diagram = map.ToMermaidDiagram();
        return diagram.Contains(upstream.Get, StringComparison.Ordinal)
            && diagram.Contains(downstream.Get, StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_ToMermaidDiagram_StartsWithFlowchartLR(
        NonEmptyString upstream,
        NonEmptyString downstream)
    {
        if (!AllValid(upstream, downstream)) return true;

        var map = new ContextMap();
        map.AddRelation(upstream.Get, downstream.Get, ContextRelationship.CustomerSupplier);

        var diagram = map.ToMermaidDiagram();
        return diagram.StartsWith("flowchart LR", StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool ContextMap_FluentApi_ReturnsSameInstance(
        NonEmptyString context1,
        NonEmptyString context2)
    {
        if (!AllValid(context1, context2)) return true;

        var map = new ContextMap();
        var result = map.AddRelation(context1.Get, context2.Get, ContextRelationship.Conformist);

        return ReferenceEquals(map, result);
    }

    // === BoundedContextAttribute Properties ===

    [Property(MaxTest = 100)]
    public bool BoundedContextAttribute_StoresContextName(NonEmptyString contextName)
    {
        if (!IsValid(contextName)) return true;

        var attr = new BoundedContextAttribute(contextName.Get);
        return attr.ContextName == contextName.Get;
    }

    [Property(MaxTest = 100)]
    public bool BoundedContextAttribute_StoresDescription(
        NonEmptyString contextName,
        NonEmptyString description)
    {
        if (!AllValid(contextName, description)) return true;

        var attr = new BoundedContextAttribute(contextName.Get) { Description = description.Get };
        return attr.Description == description.Get;
    }

    // === BoundedContextError Properties ===

    [Property(MaxTest = 100)]
    public bool BoundedContextError_OrphanedConsumer_HasCorrectCode(NonEmptyString contextName)
    {
        if (!IsValid(contextName)) return true;

        var error = BoundedContextError.OrphanedConsumer(contextName.Get, typeof(string));
        return error.ErrorCode == "CONTEXT_ORPHANED_CONSUMER"
            && error.ContextName == contextName.Get
            && error.Message.Contains(contextName.Get, StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool BoundedContextError_CircularDependency_HasCorrectCode()
    {
        var cycle = new List<string> { "A", "B", "C", "A" };
        var error = BoundedContextError.CircularDependency(cycle);
        return error.ErrorCode == "CONTEXT_CIRCULAR_DEPENDENCY"
            && error.Details is not null
            && error.Details.Count == 4;
    }

    [Property(MaxTest = 100)]
    public bool BoundedContextError_ValidationFailed_HasCorrectCode(NonEmptyString message)
    {
        if (!IsValid(message)) return true;

        var error = BoundedContextError.ValidationFailed(message.Get);
        return error.ErrorCode == "CONTEXT_VALIDATION_FAILED"
            && error.Message == message.Get;
    }

    // === BoundedContextExtensions Properties ===

    [Property(MaxTest = 100)]
    public bool GetBoundedContextName_ReturnsNullForUnattributedType()
    {
        return typeof(string).GetBoundedContextName() is null;
    }
}

// Test bounded context for property tests
[BoundedContext("TestContext")]
internal sealed class TestBoundedContext { }
