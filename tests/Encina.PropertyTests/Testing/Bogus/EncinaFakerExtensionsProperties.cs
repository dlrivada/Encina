using Bogus;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Testing.Bogus;

/// <summary>
/// Property-based tests for domain model extension methods in <see cref="EncinaFakerExtensions"/>.
/// </summary>
public sealed class EncinaFakerExtensionsProperties
{
    #region Entity ID Properties

    [Property(MaxTest = 100)]
    public bool IntEntityId_AlwaysPositive(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var id = randomizer.IntEntityId();
        return id >= 1;
    }

    [Property(MaxTest = 100)]
    public bool IntEntityId_WithinRange_RespectsBounds(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var id = randomizer.IntEntityId(100, 200);
        return id >= 100 && id <= 200;
    }

    [Property(MaxTest = 100)]
    public bool LongEntityId_AlwaysPositive(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var id = randomizer.LongEntityId();
        return id >= 1L;
    }

    [Property(MaxTest = 100)]
    public bool StringEntityId_NeverEmpty(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var id = randomizer.StringEntityId();
        return !string.IsNullOrEmpty(id) && id.Length == 12;
    }

    [Property(MaxTest = 100)]
    public bool StringEntityId_WithPrefix_AlwaysStartsWithPrefix(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var id = randomizer.StringEntityId(8, "TEST");
        return id.StartsWith("TEST_", StringComparison.Ordinal) && id.Length == 13;
    }

    [Property(MaxTest = 100)]
    public bool GuidEntityId_NeverEmpty(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var id = randomizer.GuidEntityId();
        return id != Guid.Empty;
    }

    #endregion

    #region Strongly-Typed ID Properties

    [Property(MaxTest = 100)]
    public bool StronglyTypedIdValue_Int_AlwaysPositive(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var value = randomizer.StronglyTypedIdValue<int>();
        return value >= 1;
    }

    [Property(MaxTest = 100)]
    public bool StronglyTypedIdValue_Long_AlwaysPositive(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var value = randomizer.StronglyTypedIdValue<long>();
        return value >= 1L;
    }

    [Property(MaxTest = 100)]
    public bool StronglyTypedIdValue_String_NeverEmpty(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var value = randomizer.StronglyTypedIdValue<string>();
        return !string.IsNullOrEmpty(value);
    }

    [Property(MaxTest = 100)]
    public bool StronglyTypedIdValue_Guid_NeverEmpty(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var value = randomizer.StronglyTypedIdValue<Guid>();
        return value != Guid.Empty;
    }

    #endregion

    #region Value Object Properties

    [Property(MaxTest = 100)]
    public bool QuantityValue_AlwaysNonNegative(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var value = randomizer.QuantityValue();
        return value >= 0 && value <= 1000;
    }

    [Property(MaxTest = 100)]
    public bool QuantityValue_WithRange_RespectsBounds(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var value = randomizer.QuantityValue(10, 50);
        return value >= 10 && value <= 50;
    }

    [Property(MaxTest = 100)]
    public bool PercentageValue_AlwaysBetweenZeroAndHundred(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var value = randomizer.PercentageValue();
        return value >= 0m && value <= 100m;
    }

    [Property(MaxTest = 100)]
    public bool PercentageValue_WithRange_RespectsBounds(PositiveInt seed)
    {
        var randomizer = new Randomizer(seed.Get);
        var value = randomizer.PercentageValue(25, 75);
        return value >= 25m && value <= 75m;
    }

    [Property(MaxTest = 100)]
    public bool DateRangeValue_EndAlwaysAfterOrEqualStart(PositiveInt seed)
    {
        var faker = new Faker { Random = new Randomizer(seed.Get) };
        var (start, end) = faker.Date.DateRangeValue();
        return end >= start;
    }

    [Property(MaxTest = 100)]
    public bool TimeRangeValue_EndAlwaysAfterStart(PositiveInt seed)
    {
        var faker = new Faker { Random = new Randomizer(seed.Get) };
        var (start, end) = faker.Date.TimeRangeValue();
        return end > start;
    }

    #endregion

    #region Reproducibility Properties

    [Property(MaxTest = 100)]
    public bool SameSeed_ProducesSameIntEntityId(PositiveInt seed)
    {
        var randomizer1 = new Randomizer(seed.Get);
        var randomizer2 = new Randomizer(seed.Get);

        var id1 = randomizer1.IntEntityId();
        var id2 = randomizer2.IntEntityId();

        return id1 == id2;
    }

    [Property(MaxTest = 100)]
    public bool SameSeed_ProducesSameGuidEntityId(PositiveInt seed)
    {
        var randomizer1 = new Randomizer(seed.Get);
        var randomizer2 = new Randomizer(seed.Get);

        var id1 = randomizer1.GuidEntityId();
        var id2 = randomizer2.GuidEntityId();

        return id1 == id2;
    }

    [Property(MaxTest = 100)]
    public bool SameSeed_ProducesSameQuantityValue(PositiveInt seed)
    {
        var randomizer1 = new Randomizer(seed.Get);
        var randomizer2 = new Randomizer(seed.Get);

        var value1 = randomizer1.QuantityValue();
        var value2 = randomizer2.QuantityValue();

        return value1 == value2;
    }

    [Property(MaxTest = 100)]
    public bool SameSeed_ProducesSamePercentageValue(PositiveInt seed)
    {
        var randomizer1 = new Randomizer(seed.Get);
        var randomizer2 = new Randomizer(seed.Get);

        var value1 = randomizer1.PercentageValue();
        var value2 = randomizer2.PercentageValue();

        return value1 == value2;
    }

    [Property(MaxTest = 100)]
    public bool DifferentSeeds_ProduceDifferentIntEntityIds(PositiveInt seed)
    {
        if (seed.Get >= int.MaxValue - 1) return true;

        var randomizer1 = new Randomizer(seed.Get);
        var randomizer2 = new Randomizer(seed.Get + 1);

        var id1 = randomizer1.IntEntityId();
        var id2 = randomizer2.IntEntityId();

        return id1 != id2;
    }

    [Property(MaxTest = 100)]
    public bool SameSeed_ProducesSameDateRange(PositiveInt seed)
    {
        var faker1 = new Faker { Random = new Randomizer(seed.Get) };
        var faker2 = new Faker { Random = new Randomizer(seed.Get) };

        var range1 = faker1.Date.DateRangeValue();
        var range2 = faker2.Date.DateRangeValue();

        return range1.Start == range2.Start && range1.End == range2.End;
    }

    [Property(MaxTest = 100)]
    public bool SameSeed_ProducesSameTimeRange(PositiveInt seed)
    {
        var faker1 = new Faker { Random = new Randomizer(seed.Get) };
        var faker2 = new Faker { Random = new Randomizer(seed.Get) };

        var range1 = faker1.Date.TimeRangeValue();
        var range2 = faker2.Date.TimeRangeValue();

        return range1.Start == range2.Start && range1.End == range2.End;
    }

    #endregion
}
