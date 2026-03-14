using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Property-based tests for <see cref="SCCAgreementAggregate"/> verifying lifecycle
/// invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class SCCAgreementAggregatePropertyTests
{
    #region Factory Invariants

    /// <summary>
    /// Invariant: A newly registered SCC agreement is always in Active state
    /// (not revoked, not expired) regardless of the module or version used.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Register_AlwaysActive()
    {
        var moduleGen = Arb.From(Gen.Elements(
            SCCModule.ControllerToController,
            SCCModule.ControllerToProcessor,
            SCCModule.ProcessorToProcessor,
            SCCModule.ProcessorToController));

        return Prop.ForAll(moduleGen, module =>
        {
            var aggregate = SCCAgreementAggregate.Register(
                Guid.NewGuid(),
                "processor-1",
                module,
                "2021/914",
                DateTimeOffset.UtcNow);

            return !aggregate.IsRevoked &&
                   !aggregate.IsExpired &&
                   aggregate.SupplementaryMeasures.Count == 0;
        });
    }

    #endregion

    #region Revocation Invariants

    /// <summary>
    /// Invariant: After revocation, IsValid always returns false regardless of
    /// the current time or expiration settings.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Revoke_AlwaysSetsRevoked()
    {
        var moduleGen = Arb.From(Gen.Elements(
            SCCModule.ControllerToController,
            SCCModule.ControllerToProcessor,
            SCCModule.ProcessorToProcessor,
            SCCModule.ProcessorToController));

        return Prop.ForAll(moduleGen, module =>
        {
            var now = DateTimeOffset.UtcNow;
            var aggregate = SCCAgreementAggregate.Register(
                Guid.NewGuid(),
                "processor-1",
                module,
                "2021/914",
                now,
                expiresAtUtc: now.AddYears(5));

            aggregate.Revoke("Test revocation", "admin");

            return aggregate.IsRevoked &&
                   !aggregate.IsValid(now) &&
                   !aggregate.IsValid(now.AddYears(-10)) &&
                   !aggregate.IsValid(now.AddYears(10));
        });
    }

    #endregion

    #region Expiration Invariants

    /// <summary>
    /// Invariant: If expiresAtUtc is in the past relative to now, IsValid returns false.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property IsValid_AfterExpiration_ReturnsFalse()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10000)),
            minutesPast =>
            {
                var executedAt = DateTimeOffset.UtcNow.AddDays(-365);
                var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-minutesPast);
                var now = DateTimeOffset.UtcNow;

                var aggregate = SCCAgreementAggregate.Register(
                    Guid.NewGuid(),
                    "processor-1",
                    SCCModule.ControllerToProcessor,
                    "2021/914",
                    executedAt,
                    expiresAtUtc: expiresAt);

                return !aggregate.IsValid(now);
            });
    }

    /// <summary>
    /// Invariant: If expiresAtUtc is in the future and not revoked, IsValid returns true.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property IsValid_BeforeExpiration_ReturnsTrue()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10000)),
            minutesFuture =>
            {
                var executedAt = DateTimeOffset.UtcNow.AddDays(-30);
                var now = DateTimeOffset.UtcNow;
                var expiresAt = now.AddMinutes(minutesFuture);

                var aggregate = SCCAgreementAggregate.Register(
                    Guid.NewGuid(),
                    "processor-1",
                    SCCModule.ControllerToProcessor,
                    "2021/914",
                    executedAt,
                    expiresAtUtc: expiresAt);

                return aggregate.IsValid(now);
            });
    }

    #endregion

    #region Supplementary Measure Invariants

    /// <summary>
    /// Invariant: Adding N supplementary measures results in exactly N measures.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property AddSupplementaryMeasure_IncreasesCount()
    {
        var measureTypeGen = Arb.From(Gen.Elements(
            SupplementaryMeasureType.Technical,
            SupplementaryMeasureType.Organizational,
            SupplementaryMeasureType.Contractual));

        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 20)),
            measureTypeGen,
            (count, measureType) =>
            {
                var aggregate = SCCAgreementAggregate.Register(
                    Guid.NewGuid(),
                    "processor-1",
                    SCCModule.ControllerToProcessor,
                    "2021/914",
                    DateTimeOffset.UtcNow,
                    expiresAtUtc: DateTimeOffset.UtcNow.AddYears(5));

                for (var i = 0; i < count; i++)
                {
                    aggregate.AddSupplementaryMeasure(
                        Guid.NewGuid(),
                        measureType,
                        $"Measure {i + 1}: End-to-end encryption for data at rest");
                }

                return aggregate.SupplementaryMeasures.Count == count;
            });
    }

    #endregion
}
