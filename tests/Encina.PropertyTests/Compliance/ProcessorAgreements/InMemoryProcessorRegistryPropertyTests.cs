using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="InMemoryProcessorRegistry"/> verifying store
/// invariants using FsCheck random data generation.
/// </summary>
public class InMemoryProcessorRegistryPropertyTests
{
    private static InMemoryProcessorRegistry CreateRegistry() =>
        new(NullLogger<InMemoryProcessorRegistry>.Instance);

    #region Register-then-Get Invariants

    /// <summary>
    /// Invariant: A registered processor can always be retrieved by its Id.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_ThenGet_AlwaysReturnsRegisteredProcessor(NonEmptyString processorId)
    {
        var registry = CreateRegistry();
        var processor = CreateProcessor(id: processorId.Get);

        var registerResult = registry.RegisterProcessorAsync(processor).AsTask().Result;
        if (!registerResult.IsRight) return false;

        var getResult = registry.GetProcessorAsync(processorId.Get).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (Option<Processor>)getResult;
        return option.Match(
            Some: retrieved => retrieved.Id == processor.Id
                && retrieved.Name == processor.Name
                && retrieved.Country == processor.Country,
            None: () => false);
    }

    /// <summary>
    /// Invariant: Getting a non-existent processor always returns None.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Get_NonExistent_AlwaysReturnsNone(NonEmptyString processorId)
    {
        var registry = CreateRegistry();

        var result = registry.GetProcessorAsync(processorId.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var option = (Option<Processor>)result;
        return option.IsNone;
    }

    #endregion

    #region GetAll Invariants

    /// <summary>
    /// Invariant: Registering multiple processors, GetAll returns all of them.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool RegisterMultiple_GetAll_ReturnsAll(PositiveInt count)
    {
        var registry = CreateRegistry();
        var n = Math.Min(count.Get, 20); // Cap to avoid excessive test time

        for (var i = 0; i < n; i++)
        {
            var processor = CreateProcessor(id: $"processor-{i}");
            registry.RegisterProcessorAsync(processor).AsTask().Wait();
        }

        var result = registry.GetAllProcessorsAsync().AsTask().Result;
        if (!result.IsRight) return false;

        var all = result.Match(list => list, _ => (IReadOnlyList<Processor>)[]);
        return all.Count == n;
    }

    /// <summary>
    /// Invariant: GetAll on an empty registry returns an empty list.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetAll_EmptyRegistry_ReturnsEmptyList()
    {
        var registry = CreateRegistry();

        var result = registry.GetAllProcessorsAsync().AsTask().Result;
        if (!result.IsRight) return false;

        var all = result.Match(list => list, _ => (IReadOnlyList<Processor>)[]);
        return all.Count == 0;
    }

    #endregion

    #region Duplicate Registration Invariants

    /// <summary>
    /// Invariant: Registering a processor with an existing Id always returns an error.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_DuplicateId_AlwaysReturnsError(NonEmptyString processorId)
    {
        var registry = CreateRegistry();
        var processor = CreateProcessor(id: processorId.Get);

        registry.RegisterProcessorAsync(processor).AsTask().Wait();
        var result = registry.RegisterProcessorAsync(processor).AsTask().Result;

        return result.IsLeft;
    }

    #endregion

    #region Remove Invariants

    /// <summary>
    /// Invariant: After removing a processor, it is no longer retrievable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Remove_ThenGet_AlwaysReturnsNone(NonEmptyString processorId)
    {
        var registry = CreateRegistry();
        var processor = CreateProcessor(id: processorId.Get);

        registry.RegisterProcessorAsync(processor).AsTask().Wait();
        var removeResult = registry.RemoveProcessorAsync(processorId.Get).AsTask().Result;
        if (!removeResult.IsRight) return false;

        var getResult = registry.GetProcessorAsync(processorId.Get).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (Option<Processor>)getResult;
        return option.IsNone;
    }

    /// <summary>
    /// Invariant: Removing a non-existent processor always returns an error.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Remove_NonExistent_AlwaysReturnsError(NonEmptyString processorId)
    {
        var registry = CreateRegistry();
        var result = registry.RemoveProcessorAsync(processorId.Get).AsTask().Result;
        return result.IsLeft;
    }

    #endregion

    #region Update Invariants

    /// <summary>
    /// Invariant: Updating an existing processor preserves the new values.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Update_ExistingProcessor_PreservesNewValues(
        NonEmptyString processorId, NonEmptyString newName)
    {
        var registry = CreateRegistry();
        var original = CreateProcessor(id: processorId.Get, name: "OriginalName");

        registry.RegisterProcessorAsync(original).AsTask().Wait();

        var updated = original with { Name = newName.Get, LastUpdatedAtUtc = DateTimeOffset.UtcNow };
        var updateResult = registry.UpdateProcessorAsync(updated).AsTask().Result;
        if (!updateResult.IsRight) return false;

        var getResult = registry.GetProcessorAsync(processorId.Get).AsTask().Result;
        if (!getResult.IsRight) return false;

        var option = (Option<Processor>)getResult;
        return option.Match(
            Some: retrieved => retrieved.Name == newName.Get,
            None: () => false);
    }

    /// <summary>
    /// Invariant: Updating a non-existent processor always returns an error.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Update_NonExistent_AlwaysReturnsError(NonEmptyString processorId)
    {
        var registry = CreateRegistry();
        var processor = CreateProcessor(id: processorId.Get);
        var result = registry.UpdateProcessorAsync(processor).AsTask().Result;
        return result.IsLeft;
    }

    #endregion

    #region Helpers

    private static Processor CreateProcessor(
        string? id = null,
        string? name = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Processor
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name ?? "TestProcessor",
            Country = "DE",
            Depth = 0,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.Specific,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        };
    }

    #endregion
}
