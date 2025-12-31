using Bogus;
using Shouldly;
using Xunit;

namespace Encina.Testing.Bogus.Tests;

/// <summary>
/// Unit tests for domain model extension methods in <see cref="EncinaFakerExtensions"/>.
/// </summary>
public sealed class EncinaFakerExtensionsTests
{
    private readonly Randomizer _randomizer = new(12345);
    private readonly Faker _faker = new() { Random = new Randomizer(12345) };

    #region Entity ID Tests

    [Fact]
    public void EntityId_Guid_ShouldGenerateValidGuid()
    {
        // Act
        var id = _randomizer.EntityId<Guid>();

        // Assert
        id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void EntityId_Int_ShouldGeneratePositiveInteger()
    {
        // Act
        var id = _randomizer.EntityId<int>();

        // Assert
        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void EntityId_Long_ShouldGeneratePositiveLong()
    {
        // Act
        var id = _randomizer.EntityId<long>();

        // Assert
        id.ShouldBeGreaterThan(0L);
    }

    [Fact]
    public void EntityId_String_ShouldGenerateNonEmptyString()
    {
        // Act
        var id = _randomizer.EntityId<string>();

        // Assert
        id.ShouldNotBeNullOrEmpty();
        id.Length.ShouldBe(12);
    }

    [Fact]
    public void EntityId_UnsupportedType_ShouldThrowNotSupportedException()
    {
        // Act & Assert
        Should.Throw<NotSupportedException>(() => _randomizer.EntityId<decimal>());
    }

    [Fact]
    public void EntityId_NullRandomizer_ShouldThrowArgumentNullException()
    {
        // Arrange
        Randomizer? nullRandomizer = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => nullRandomizer!.EntityId<Guid>());
    }

    [Fact]
    public void GuidEntityId_ShouldGenerateValidGuid()
    {
        // Act
        var id = _randomizer.GuidEntityId();

        // Assert
        id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void IntEntityId_ShouldGeneratePositiveInteger()
    {
        // Act
        var id = _randomizer.IntEntityId();

        // Assert
        id.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void IntEntityId_WithRange_ShouldRespectBounds()
    {
        // Act
        var id = _randomizer.IntEntityId(100, 200);

        // Assert
        id.ShouldBeInRange(100, 200);
    }

    [Fact]
    public void IntEntityId_MinLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _randomizer.IntEntityId(0, 100));
    }

    [Fact]
    public void LongEntityId_ShouldGeneratePositiveLong()
    {
        // Act
        var id = _randomizer.LongEntityId();

        // Assert
        id.ShouldBeGreaterThanOrEqualTo(1L);
    }

    [Fact]
    public void LongEntityId_WithRange_ShouldRespectBounds()
    {
        // Act
        var id = _randomizer.LongEntityId(1_000_000, 9_999_999);

        // Assert
        id.ShouldBeInRange(1_000_000, 9_999_999);
    }

    [Fact]
    public void LongEntityId_MinLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _randomizer.LongEntityId(0, 100));
    }

    [Fact]
    public void StringEntityId_ShouldGenerateNonEmptyString()
    {
        // Act
        var id = _randomizer.StringEntityId();

        // Assert
        id.ShouldNotBeNullOrEmpty();
        id.Length.ShouldBe(12);
    }

    [Fact]
    public void StringEntityId_WithCustomLength_ShouldRespectLength()
    {
        // Act
        var id = _randomizer.StringEntityId(length: 8);

        // Assert
        id.Length.ShouldBe(8);
    }

    [Fact]
    public void StringEntityId_WithPrefix_ShouldIncludePrefix()
    {
        // Act
        var id = _randomizer.StringEntityId(8, "ORD");

        // Assert
        id.ShouldStartWith("ORD_");
        id.Length.ShouldBe(12); // "ORD_" + 8 chars
    }

    [Fact]
    public void StringEntityId_LengthLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _randomizer.StringEntityId(0));
    }

    #endregion

    #region Strongly-Typed ID Tests

    [Fact]
    public void StronglyTypedIdValue_Guid_ShouldGenerateValidGuid()
    {
        // Act
        var value = _randomizer.StronglyTypedIdValue<Guid>();

        // Assert
        value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void StronglyTypedIdValue_Int_ShouldGeneratePositiveInteger()
    {
        // Act
        var value = _randomizer.StronglyTypedIdValue<int>();

        // Assert
        value.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void StronglyTypedIdValue_Long_ShouldGeneratePositiveLong()
    {
        // Act
        var value = _randomizer.StronglyTypedIdValue<long>();

        // Assert
        value.ShouldBeGreaterThan(0L);
    }

    [Fact]
    public void StronglyTypedIdValue_String_ShouldGenerateNonEmptyString()
    {
        // Act
        var value = _randomizer.StronglyTypedIdValue<string>();

        // Assert
        value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void StronglyTypedIdValue_UnsupportedType_ShouldThrowNotSupportedException()
    {
        // Act & Assert
        Should.Throw<NotSupportedException>(() => _randomizer.StronglyTypedIdValue<decimal>());
    }

    [Fact]
    public void GuidStronglyTypedIdValue_ShouldGenerateValidGuid()
    {
        // Act
        var value = _randomizer.GuidStronglyTypedIdValue();

        // Assert
        value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void IntStronglyTypedIdValue_ShouldGeneratePositiveInteger()
    {
        // Act
        var value = _randomizer.IntStronglyTypedIdValue();

        // Assert
        value.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void IntStronglyTypedIdValue_WithRange_ShouldRespectBounds()
    {
        // Act
        var value = _randomizer.IntStronglyTypedIdValue(500, 1000);

        // Assert
        value.ShouldBeInRange(500, 1000);
    }

    [Fact]
    public void LongStronglyTypedIdValue_ShouldGeneratePositiveLong()
    {
        // Act
        var value = _randomizer.LongStronglyTypedIdValue();

        // Assert
        value.ShouldBeGreaterThanOrEqualTo(1L);
    }

    [Fact]
    public void StringStronglyTypedIdValue_ShouldGenerateNonEmptyString()
    {
        // Act
        var value = _randomizer.StringStronglyTypedIdValue();

        // Assert
        value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void StringStronglyTypedIdValue_WithPrefix_ShouldIncludePrefix()
    {
        // Act
        var value = _randomizer.StringStronglyTypedIdValue(8, "SKU");

        // Assert
        value.ShouldStartWith("SKU_");
    }

    #endregion

    #region Value Object Tests

    [Fact]
    public void QuantityValue_ShouldGenerateNonNegativeInteger()
    {
        // Act
        var value = _randomizer.QuantityValue();

        // Assert
        value.ShouldBeGreaterThanOrEqualTo(0);
        value.ShouldBeLessThanOrEqualTo(1000);
    }

    [Fact]
    public void QuantityValue_WithRange_ShouldRespectBounds()
    {
        // Act
        var value = _randomizer.QuantityValue(10, 50);

        // Assert
        value.ShouldBeInRange(10, 50);
    }

    [Fact]
    public void QuantityValue_NegativeMin_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _randomizer.QuantityValue(-1, 100));
    }

    [Fact]
    public void PercentageValue_ShouldGenerateValueBetweenZeroAndHundred()
    {
        // Act
        var value = _randomizer.PercentageValue();

        // Assert
        value.ShouldBeGreaterThanOrEqualTo(0m);
        value.ShouldBeLessThanOrEqualTo(100m);
    }

    [Fact]
    public void PercentageValue_WithRange_ShouldRespectBounds()
    {
        // Act
        var value = _randomizer.PercentageValue(10, 50);

        // Assert
        value.ShouldBeGreaterThanOrEqualTo(10m);
        value.ShouldBeLessThanOrEqualTo(50m);
    }

    [Fact]
    public void PercentageValue_ShouldRespectDecimalPlaces()
    {
        // Act
        var value = _randomizer.PercentageValue(0, 100, decimals: 1);

        // Assert
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
        decimalPlaces.ShouldBeLessThanOrEqualTo((byte)1);
    }

    [Fact]
    public void PercentageValue_NegativeMin_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _randomizer.PercentageValue(-1, 100));
    }

    [Fact]
    public void PercentageValue_MaxExceedsHundred_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _randomizer.PercentageValue(0, 101));
    }

    [Fact]
    public void DateRangeValue_ShouldGenerateValidDateRange()
    {
        // Act
        var (start, end) = _faker.Date.DateRangeValue();

        // Assert
        start.ShouldBeLessThanOrEqualTo(end);
    }

    [Fact]
    public void DateRangeValue_StartShouldBeWithinPastDays()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var (start, _) = _faker.Date.DateRangeValue(daysInPast: 7);

        // Assert
        start.ShouldBeGreaterThanOrEqualTo(today.AddDays(-7));
        start.ShouldBeLessThanOrEqualTo(today);
    }

    [Fact]
    public void DateRangeValue_NegativeDaysInPast_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _faker.Date.DateRangeValue(daysInPast: -1));
    }

    [Fact]
    public void TimeRangeValue_ShouldGenerateValidTimeRange()
    {
        // Act
        var (start, end) = _faker.Date.TimeRangeValue();

        // Assert
        start.ShouldBeLessThan(end);
    }

    [Fact]
    public void TimeRangeValue_ShouldRespectMinHourSpan()
    {
        // Act
        var (start, end) = _faker.Date.TimeRangeValue(minHourSpan: 2, maxHourSpan: 4);

        // Assert
        var duration = end - start;
        duration.TotalHours.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void TimeRangeValue_MinHourSpanLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _faker.Date.TimeRangeValue(minHourSpan: 0));
    }

    [Fact]
    public void TimeRangeValue_MaxHourSpanExceedsTwentyThree_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _faker.Date.TimeRangeValue(maxHourSpan: 24));
    }

    [Fact]
    public void TimeRangeValue_MaxLessThanMin_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _faker.Date.TimeRangeValue(minHourSpan: 5, maxHourSpan: 3));
    }

    #endregion

    #region Seed Reproducibility Tests

    [Fact]
    public void EntityId_SameSeed_ShouldProduceSameResults()
    {
        // Arrange
        var randomizer1 = new Randomizer(42);
        var randomizer2 = new Randomizer(42);

        // Act
        var id1 = randomizer1.EntityId<Guid>();
        var id2 = randomizer2.EntityId<Guid>();

        // Assert
        id1.ShouldBe(id2);
    }

    [Fact]
    public void StronglyTypedIdValue_SameSeed_ShouldProduceSameResults()
    {
        // Arrange
        var randomizer1 = new Randomizer(42);
        var randomizer2 = new Randomizer(42);

        // Act
        var value1 = randomizer1.StronglyTypedIdValue<int>();
        var value2 = randomizer2.StronglyTypedIdValue<int>();

        // Assert
        value1.ShouldBe(value2);
    }

    [Fact]
    public void QuantityValue_SameSeed_ShouldProduceSameResults()
    {
        // Arrange
        var randomizer1 = new Randomizer(42);
        var randomizer2 = new Randomizer(42);

        // Act
        var value1 = randomizer1.QuantityValue();
        var value2 = randomizer2.QuantityValue();

        // Assert
        value1.ShouldBe(value2);
    }

    [Fact]
    public void PercentageValue_SameSeed_ShouldProduceSameResults()
    {
        // Arrange
        var randomizer1 = new Randomizer(42);
        var randomizer2 = new Randomizer(42);

        // Act
        var value1 = randomizer1.PercentageValue();
        var value2 = randomizer2.PercentageValue();

        // Assert
        value1.ShouldBe(value2);
    }

    [Fact]
    public void DateRangeValue_SameSeed_ShouldProduceSameResults()
    {
        // Arrange
        var faker1 = new Faker { Random = new Randomizer(42) };
        var faker2 = new Faker { Random = new Randomizer(42) };

        // Act
        var range1 = faker1.Date.DateRangeValue();
        var range2 = faker2.Date.DateRangeValue();

        // Assert
        range1.Start.ShouldBe(range2.Start);
        range1.End.ShouldBe(range2.End);
    }

    [Fact]
    public void TimeRangeValue_SameSeed_ShouldProduceSameResults()
    {
        // Arrange
        var faker1 = new Faker { Random = new Randomizer(42) };
        var faker2 = new Faker { Random = new Randomizer(42) };

        // Act
        var range1 = faker1.Date.TimeRangeValue();
        var range2 = faker2.Date.TimeRangeValue();

        // Assert
        range1.Start.ShouldBe(range2.Start);
        range1.End.ShouldBe(range2.End);
    }

    #endregion
}
