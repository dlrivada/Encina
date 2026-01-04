using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Shouldly;
using System.Globalization;

namespace Encina.Testing.Examples.PropertyBased;

/// <summary>
/// Examples demonstrating FsCheck property-based testing with Encina.Testing.FsCheck.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 7.1
/// </summary>
public sealed class PropertyTestExamples : PropertyTestBase
{
    /// <summary>
    /// Pattern: Basic property test using EncinaProperty attribute.
    /// EncinaProperty runs 100 tests by default with Encina type arbitraries auto-discovered.
    /// </summary>
    [EncinaProperty]
    public Property EncinaProperty_BasicUsage(EncinaError error)
    {
        // Property tests return Property type, not bool
        // Use .ToProperty() to convert boolean assertions
        return (!string.IsNullOrEmpty(error.Message)).ToProperty();
    }

    /// <summary>
    /// Pattern: QuickProperty for fast feedback (20 tests).
    /// Use during development for quick iteration.
    /// </summary>
    [QuickProperty]
    public Property QuickProperty_ForFastFeedback(int value)
    {
        // Quick properties run fewer tests for faster feedback
        return (value == int.MinValue || Math.Abs(value) >= 0).ToProperty();
    }

    /// <summary>
    /// Pattern: ThoroughProperty for comprehensive testing (1000 tests).
    /// Use for critical invariants that need extensive coverage.
    /// </summary>
    [ThoroughProperty]
    public Property ThoroughProperty_ForCriticalInvariants(bool value)
    {
        // Double negation is always identity
        return (!!value == value).ToProperty();
    }

    /// <summary>
    /// Pattern: Custom test count via attribute parameter.
    /// </summary>
    [EncinaProperty(50)]
    public Property EncinaProperty_CustomTestCount(Either<EncinaError, int> either)
    {
        // Either must be Left XOR Right (exclusive)
        return (either.IsLeft || either.IsRight).ToProperty();
    }

    /// <summary>
    /// Pattern: Testing with multiple Encina types via auto-discovery.
    /// PropertyTestBase automatically provides arbitraries for all Encina types.
    /// </summary>
    [EncinaProperty]
    public Property AutoDiscovery_AllEncinaTypes(
        EncinaError error,
        IRequestContext context,
        IOutboxMessage outbox)
    {
        // All Encina types are automatically generated
        return (
            !string.IsNullOrEmpty(error.Message) &&
            !string.IsNullOrEmpty(context.CorrelationId) &&
            outbox.Id != Guid.Empty
        ).ToProperty();
    }

    /// <summary>
    /// Pattern: Using labels for better failure diagnostics.
    /// Labels help identify which property failed in complex tests.
    /// </summary>
    [EncinaProperty]
    public Property Labels_ForBetterDiagnostics(EncinaError error)
    {
        return (!string.IsNullOrEmpty(error.Message))
            .ToProperty()
            .Label("Error message should not be empty");
    }

    /// <summary>
    /// Pattern: Using pre-built properties from EncinaProperties.
    /// The library provides common invariant checks ready to use.
    /// </summary>
    [EncinaProperty]
    public Property PreBuiltProperties_EitherExclusive(Either<EncinaError, int> either)
    {
        // EncinaProperties provides pre-built invariants
        return EncinaProperties.EitherIsExclusive(either);
    }

    /// <summary>
    /// Pattern: Testing outbox message invariants.
    /// </summary>
    [EncinaProperty]
    public Property OutboxMessage_ProcessedStateConsistency(IOutboxMessage message)
    {
        // Use pre-built property for common invariant
        return EncinaProperties.OutboxProcessedStateIsConsistent(message);
    }

    /// <summary>
    /// Pattern: Testing inbox message invariants.
    /// </summary>
    [EncinaProperty]
    public Property InboxMessage_RequiredFields(IInboxMessage message)
    {
        return EncinaProperties.InboxHasRequiredFields(message);
    }

    /// <summary>
    /// Pattern: Testing saga state invariants.
    /// </summary>
    [EncinaProperty]
    public Property SagaState_ValidStatus(ISagaState state)
    {
        return EncinaProperties.SagaStatusIsValid(state);
    }

    /// <summary>
    /// Pattern: Testing scheduled message timing.
    /// </summary>
    [EncinaProperty]
    public Property ScheduledMessage_ValidTiming(IScheduledMessage message)
    {
        return EncinaProperties.ScheduledHasValidTiming(message);
    }

    /// <summary>
    /// Pattern: Combining multiple properties with And.
    /// </summary>
    [EncinaProperty]
    public Property CombinedProperties_MultipleInvariants(IOutboxMessage message)
    {
        var prop1 = EncinaProperties.OutboxHasRequiredFields(message);
        var prop2 = EncinaProperties.OutboxProcessedStateIsConsistent(message);
        return prop1.And(prop2);
    }

    /// <summary>
    /// Pattern: Using generators for custom test data.
    /// GenExtensions provides helpers for common types.
    /// </summary>
    [Property]
    public Property CustomGenerators_StringTypes()
    {
        // Use GenExtensions for custom generators
        var emailGen = GenExtensions.EmailAddress();
        var nonEmptyGen = GenExtensions.NonEmptyString();

        return Prop.ForAll(emailGen.ToArbitrary(), nonEmptyGen.ToArbitrary(),
            (email, text) =>
            {
                email.ShouldContain("@");
                text.ShouldNotBeNullOrEmpty();
                return true;
            });
    }

    /// <summary>
    /// Pattern: UTC DateTime generation.
    /// </summary>
    [Property]
    public Property UtcDateTimeGenerators()
    {
        var pastGen = GenExtensions.PastUtcDateTime(30);
        var futureGen = GenExtensions.FutureUtcDateTime(30);

        return Prop.ForAll(pastGen.ToArbitrary(), futureGen.ToArbitrary(),
            (past, future) =>
            {
                past.Kind.ShouldBe(DateTimeKind.Utc);
                future.Kind.ShouldBe(DateTimeKind.Utc);
                past.ShouldBeLessThan(DateTime.UtcNow);
                future.ShouldBeGreaterThan(DateTime.UtcNow);
                return true;
            });
    }

    /// <summary>
    /// Pattern: Either generators for success/failure scenarios.
    /// </summary>
    [EncinaProperty]
    public Property EitherGenerators_SuccessOnly()
    {
        // Generate only success (Right) values
        // SuccessEither<T>() returns Arbitrary<Either<EncinaError, T>>
        var successArb = EncinaArbitraries.SuccessEither<int>();

        return Prop.ForAll(successArb, either =>
        {
            either.IsRight.ShouldBeTrue();
            return true;
        });
    }

    /// <summary>
    /// Pattern: Testing map preserves state.
    /// </summary>
    [EncinaProperty]
    public Property MapProperty_PreservesRightState(Either<EncinaError, int> either)
    {
        Func<int, string> mapper = x => x.ToString(CultureInfo.InvariantCulture);
        return EncinaProperties.MapPreservesRightState(either, mapper);
    }
}
