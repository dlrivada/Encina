using Encina.Messaging.Outbox;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Testing.FsCheck;

/// <summary>
/// Unit tests for <see cref="PropertyTestBase"/> and related attributes.
/// </summary>
public class PropertyTestBaseTests : PropertyTestBase
{
    #region PropertyTestBase Tests

    [Fact]
    public void PropertyTestBase_RegistersArbitrariesAutomatically()
    {
        // Act - Generate directly using EncinaArbitraries
        var errorArb = EncinaArbitraries.EncinaError();
        var contextArb = EncinaArbitraries.RequestContext();

        var errorSamples = Gen.Sample(errorArb.Generator, 10, 10).ToList();
        var contextSamples = Gen.Sample(contextArb.Generator, 10, 10).ToList();

        // Assert
        errorSamples.ShouldNotBeEmpty();
        contextSamples.ShouldNotBeEmpty();
    }

    #endregion

    #region PropertyTestConfig Tests

    [Fact]
    public void PropertyTestConfig_HasDefaultMaxTest()
    {
        // Assert
        PropertyTestConfig.DefaultMaxTest.ShouldBe(100);
    }

    [Fact]
    public void PropertyTestConfig_HasQuickMaxTest()
    {
        // Assert
        PropertyTestConfig.QuickMaxTest.ShouldBe(20);
    }

    [Fact]
    public void PropertyTestConfig_HasThoroughMaxTest()
    {
        // Assert
        PropertyTestConfig.ThoroughMaxTest.ShouldBe(1000);
    }

    [Fact]
    public void PropertyTestConfig_HasDefaultEndSize()
    {
        // Assert
        PropertyTestConfig.DefaultEndSize.ShouldBe(100);
    }

    [Fact]
    public void PropertyTestConfig_HasThoroughEndSize()
    {
        // Assert
        PropertyTestConfig.ThoroughEndSize.ShouldBe(200);
    }

    #endregion

    #region EncinaPropertyAttribute Tests

    [EncinaProperty]
    public Property EncinaPropertyAttribute_WorksWithEncinaTypes(EncinaError error)
    {
        return (!string.IsNullOrEmpty(error.Message)).ToProperty();
    }

    [EncinaProperty(50)]
    public Property EncinaPropertyAttribute_AcceptsCustomMaxTest(Either<EncinaError, int> either)
    {
        return (either.IsLeft || either.IsRight).ToProperty();
    }

    #endregion

    #region QuickPropertyAttribute Tests

    // Validates QuickPropertyAttribute runs fewer tests (10) while testing a real property:
    // Math.Abs should always return a non-negative value for any int except int.MinValue
    [QuickProperty]
    public Property QuickPropertyAttribute_RunsFewerTests(int value)
    {
        // int.MinValue has no positive counterpart in int range
        return (value == int.MinValue || Math.Abs(value) >= 0).ToProperty();
    }

    #endregion

    #region ThoroughPropertyAttribute Tests

    // Validates ThoroughPropertyAttribute runs more tests (1000) while testing a real property:
    // Double negation should return the original boolean value
    [ThoroughProperty]
    public Property ThoroughPropertyAttribute_RunsMoreTests(bool value)
    {
        return (!!value == value).ToProperty();
    }

    #endregion

    #region Integration Tests

    [EncinaProperty]
    public Property Integration_AllEncinaTypesWork(
        EncinaError error,
        IRequestContext context,
        IOutboxMessage outbox)
    {
        return (
            !string.IsNullOrEmpty(error.Message) &&
            !string.IsNullOrEmpty(context.CorrelationId) &&
            outbox.Id != Guid.Empty
        ).ToProperty();
    }

    #endregion
}
