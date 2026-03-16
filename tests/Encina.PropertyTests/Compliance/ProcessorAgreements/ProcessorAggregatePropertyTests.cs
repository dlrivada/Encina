using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="ProcessorAggregate"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class ProcessorAggregatePropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Invariant: Register always sets Name to the provided value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_Always_SetsNameCorrectly(NonEmptyString name, NonEmptyString country)
    {
        if (string.IsNullOrWhiteSpace(name.Get) || string.IsNullOrWhiteSpace(country.Get))
            return true;

        var aggregate = ProcessorAggregate.Register(
            Guid.NewGuid(), name.Get, country.Get, null, null, 0,
            SubProcessorAuthorizationType.Specific, Now);

        return aggregate.Name == name.Get;
    }

    /// <summary>
    /// Invariant: Register always sets Country to the provided value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_Always_SetsCountryCorrectly(NonEmptyString name, NonEmptyString country)
    {
        if (string.IsNullOrWhiteSpace(name.Get) || string.IsNullOrWhiteSpace(country.Get))
            return true;

        var aggregate = ProcessorAggregate.Register(
            Guid.NewGuid(), name.Get, country.Get, null, null, 0,
            SubProcessorAuthorizationType.Specific, Now);

        return aggregate.Country == country.Get;
    }

    /// <summary>
    /// Invariant: Register with a non-negative depth always preserves the depth value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_Depth_MatchesProvided(NonNegativeInt depth)
    {
        var aggregate = ProcessorAggregate.Register(
            Guid.NewGuid(), "TestProcessor", "DE", null, null, depth.Get,
            SubProcessorAuthorizationType.Specific, Now);

        return aggregate.Depth == depth.Get;
    }

    /// <summary>
    /// Invariant: CreatedAtUtc always equals the occurredAtUtc provided at registration.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_CreatedAtUtc_EqualsOccurredAtUtc(NonEmptyString name)
    {
        if (string.IsNullOrWhiteSpace(name.Get)) return true;

        var occurredAt = Now.AddDays(1);
        var aggregate = ProcessorAggregate.Register(
            Guid.NewGuid(), name.Get, "DE", null, null, 0,
            SubProcessorAuthorizationType.Specific, occurredAt);

        return aggregate.CreatedAtUtc == occurredAt;
    }

    /// <summary>
    /// Invariant: After Remove, IsRemoved is always true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Remove_Always_SetsIsRemovedTrue(NonEmptyString reason)
    {
        if (string.IsNullOrWhiteSpace(reason.Get)) return true;

        var aggregate = ProcessorAggregate.Register(
            Guid.NewGuid(), "TestProcessor", "DE", null, null, 0,
            SubProcessorAuthorizationType.Specific, Now);

        aggregate.Remove(reason.Get, Now.AddHours(1));

        return aggregate.IsRemoved;
    }

    /// <summary>
    /// Invariant: Update after Register preserves the aggregate Id.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Update_AfterRegister_PreservesId(NonEmptyString newName, NonEmptyString newCountry)
    {
        if (string.IsNullOrWhiteSpace(newName.Get) || string.IsNullOrWhiteSpace(newCountry.Get))
            return true;

        var id = Guid.NewGuid();
        var aggregate = ProcessorAggregate.Register(
            id, "OriginalName", "DE", null, null, 0,
            SubProcessorAuthorizationType.Specific, Now);

        aggregate.Update(newName.Get, newCountry.Get, null, SubProcessorAuthorizationType.General, Now.AddHours(1));

        return aggregate.Id == id;
    }

    /// <summary>
    /// Invariant: Register always sets IsRemoved to false.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Register_Always_SetsIsRemovedFalse(NonEmptyString name, NonEmptyString country)
    {
        if (string.IsNullOrWhiteSpace(name.Get) || string.IsNullOrWhiteSpace(country.Get))
            return true;

        var aggregate = ProcessorAggregate.Register(
            Guid.NewGuid(), name.Get, country.Get, null, null, 0,
            SubProcessorAuthorizationType.Specific, Now);

        return !aggregate.IsRemoved;
    }
}
