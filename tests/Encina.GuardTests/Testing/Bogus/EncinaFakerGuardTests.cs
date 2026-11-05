using Bogus.DataSets;
using Encina.Testing.Bogus;

namespace Encina.GuardTests.Testing.Bogus;

/// <summary>
/// Guard tests for <see cref="EncinaFaker{T}"/> and <see cref="EncinaFakerExtensions"/>.
/// </summary>
public sealed class EncinaFakerGuardTests
{
    private static readonly Randomizer NullRandomizer = null!;
    private static readonly Faker NullFaker = null!;
    private static readonly Date NullDate = null!;

    private static readonly Randomizer Random = new Faker { Random = new Randomizer(1) }.Random;
    private static readonly Date DateSet = new Faker { Random = new Randomizer(1) }.Date;

    public static TheoryData<string, string, Action> NullReceiverCases() => new()
    {
        { "CorrelationId", "faker", () => NullRandomizer.CorrelationId() },
        { "UserId", "faker", () => NullRandomizer.UserId() },
        { "TenantId", "faker", () => NullRandomizer.TenantId() },
        { "IdempotencyKey", "faker", () => NullRandomizer.IdempotencyKey() },
        { "NotificationType", "faker", () => NullFaker.NotificationType() },
        { "RequestType", "faker", () => NullFaker.RequestType() },
        { "SagaType", "faker", () => NullFaker.SagaType() },
        { "SagaStatus", "faker", () => NullFaker.SagaStatus() },
        { "RecentUtc", "date", () => NullDate.RecentUtc() },
        { "SoonUtc", "date", () => NullDate.SoonUtc() },
        { "JsonContent", "faker", () => NullFaker.JsonContent() },
        { "EntityId", "randomizer", () => NullRandomizer.EntityId<Guid>() },
        { "GuidEntityId", "randomizer", () => NullRandomizer.GuidEntityId() },
        { "IntEntityId", "randomizer", () => NullRandomizer.IntEntityId() },
        { "LongEntityId", "randomizer", () => NullRandomizer.LongEntityId() },
        { "StringEntityId", "randomizer", () => NullRandomizer.StringEntityId() },
        { "StronglyTypedIdValue", "randomizer", () => NullRandomizer.StronglyTypedIdValue<Guid>() },
        { "GuidStronglyTypedIdValue", "randomizer", () => NullRandomizer.GuidStronglyTypedIdValue() },
        { "IntStronglyTypedIdValue", "randomizer", () => NullRandomizer.IntStronglyTypedIdValue() },
        { "LongStronglyTypedIdValue", "randomizer", () => NullRandomizer.LongStronglyTypedIdValue() },
        { "StringStronglyTypedIdValue", "randomizer", () => NullRandomizer.StringStronglyTypedIdValue() },
        { "QuantityValue", "randomizer", () => NullRandomizer.QuantityValue() },
        { "PercentageValue", "randomizer", () => NullRandomizer.PercentageValue() },
        { "DateRangeValue", "date", () => NullDate.DateRangeValue() },
        { "TimeRangeValue", "date", () => NullDate.TimeRangeValue() },
    };

    public static TheoryData<string, string, Action> OutOfRangeCases() => new()
    {
        { "IntEntityId min", "min", () => Random.IntEntityId(min: 0) },
        { "IntEntityId max", "max", () => Random.IntEntityId(min: 5, max: 4) },
        { "LongEntityId min", "min", () => Random.LongEntityId(min: 0) },
        { "LongEntityId max", "max", () => Random.LongEntityId(min: 5, max: 4) },
        { "StringEntityId length", "length", () => Random.StringEntityId(length: 0) },
        { "IntStronglyTypedIdValue min", "min", () => Random.IntStronglyTypedIdValue(min: 0) },
        { "IntStronglyTypedIdValue max", "max", () => Random.IntStronglyTypedIdValue(min: 5, max: 4) },
        { "LongStronglyTypedIdValue min", "min", () => Random.LongStronglyTypedIdValue(min: 0) },
        { "LongStronglyTypedIdValue max", "max", () => Random.LongStronglyTypedIdValue(min: 5, max: 4) },
        { "StringStronglyTypedIdValue length", "length", () => Random.StringStronglyTypedIdValue(length: 0) },
        { "QuantityValue min", "min", () => Random.QuantityValue(min: -1) },
        { "QuantityValue max", "max", () => Random.QuantityValue(min: 5, max: 4) },
        { "PercentageValue min", "min", () => Random.PercentageValue(min: -1m) },
        { "PercentageValue max above 100", "max", () => Random.PercentageValue(max: 100.5m) },
        { "PercentageValue max below min", "max", () => Random.PercentageValue(min: 50m, max: 40m) },
        { "DateRangeValue daysInPast", "daysInPast", () => DateSet.DateRangeValue(daysInPast: -1) },
        { "DateRangeValue daysSpan", "daysSpan", () => DateSet.DateRangeValue(daysSpan: -1) },
        { "TimeRangeValue minHourSpan", "minHourSpan", () => DateSet.TimeRangeValue(minHourSpan: 0) },
        { "TimeRangeValue maxHourSpan above 23", "maxHourSpan", () => DateSet.TimeRangeValue(maxHourSpan: 24) },
        { "TimeRangeValue maxHourSpan below min", "maxHourSpan", () => DateSet.TimeRangeValue(minHourSpan: 5, maxHourSpan: 4) },
    };

    [Theory]
    [MemberData(nameof(NullReceiverCases))]
    public void ExtensionMethod_NullReceiver_ThrowsArgumentNullExceptionNamingTheReceiver(
        string method, string paramName, Action call)
    {
        method.ShouldNotBeNullOrEmpty();
        BogusGuardAssert.Throws<ArgumentNullException>(call, paramName);
    }

    [Theory]
    [MemberData(nameof(OutOfRangeCases))]
    public void ExtensionMethod_OutOfRangeArgument_ThrowsArgumentOutOfRangeExceptionNamingTheArgument(
        string scenario, string paramName, Action call)
    {
        scenario.ShouldNotBeNullOrEmpty();
        BogusGuardAssert.Throws<ArgumentOutOfRangeException>(call, paramName);
    }

    [Fact]
    public void Constructor_InvalidLocale_ThrowsNamingLocale() =>
        BogusGuardAssert.RejectsNullEmptyAndWhitespace(
            locale => _ = new EncinaFaker<object>(locale), "locale");

    [Fact]
    public void WithLocale_InvalidLocale_ThrowsNamingLocale()
    {
        var faker = new EncinaFaker<object>().UseSeed(7);

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(locale => faker.WithLocale(locale), "locale");
    }

    [Fact]
    public void EntityId_UnsupportedType_ThrowsNotSupportedException()
    {
        var exception = Should.Throw<NotSupportedException>(() => Random.EntityId<decimal>());

        exception.GetType().ShouldBe(typeof(NotSupportedException));
        exception.Message.ShouldContain("Decimal");
    }

    [Fact]
    public void StronglyTypedIdValue_UnsupportedType_ThrowsNotSupportedException()
    {
        var exception = Should.Throw<NotSupportedException>(() => Random.StronglyTypedIdValue<decimal>());

        exception.GetType().ShouldBe(typeof(NotSupportedException));
        exception.Message.ShouldContain("Decimal");
    }
}
