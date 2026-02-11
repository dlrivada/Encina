using System.Text.Json;
using Bogus;
using Bogus.DataSets;

namespace Encina.Testing.Bogus;

/// <summary>
/// Base faker class for Encina requests with common patterns and reproducibility.
/// </summary>
/// <typeparam name="T">The type to generate.</typeparam>
/// <remarks>
/// <para>
/// This class extends Bogus.Faker with Encina-specific conventions:
/// <list type="bullet">
/// <item><description>Reproducible seeds by default (seed: 12345)</description></item>
/// <item><description>Common Encina metadata patterns (CorrelationId, UserId, TenantId)</description></item>
/// <item><description>Fluent API for customization</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// var faker = new EncinaFaker&lt;CreateOrder&gt;()
///     .RuleFor(o => o.CustomerId, f => f.Random.AlphaNumeric(10))
///     .RuleFor(o => o.Amount, f => f.Finance.Amount());
///
/// var order = faker.Generate();
/// </code>
/// </para>
/// </remarks>
public class EncinaFaker<T> : Faker<T>
    where T : class
{
    /// <summary>
    /// The default seed used for reproducible test data generation.
    /// </summary>
    public const int DefaultSeed = 12345;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaFaker{T}"/> class.
    /// </summary>
    /// <param name="locale">
    /// The locale for generating localized data (default: "en").
    /// See <see href="https://github.com/bchavez/Bogus#locales">Bogus Locales</see> for supported values.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="locale"/> is null, empty, or whitespace.</exception>
    public EncinaFaker(string locale = "en")
        : base(ValidateLocale(locale))
    {
        UseSeed(DefaultSeed);
    }

    private static string ValidateLocale(string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        return locale;
    }

    /// <summary>
    /// Sets the seed for reproducible data generation.
    /// </summary>
    /// <param name="seed">The seed value.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public new EncinaFaker<T> UseSeed(int seed)
    {
        base.UseSeed(seed);
        return this;
    }

    /// <summary>
    /// Sets the locale for generating localized data.
    /// </summary>
    /// <param name="locale">
    /// The locale code (e.g., "en", "es", "fr", "de", "pt_BR").
    /// See <see href="https://github.com/bchavez/Bogus#locales">Bogus Locales</see> for the full list of supported locales.
    /// </param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="locale"/> is null, empty, or whitespace.</exception>
    public EncinaFaker<T> WithLocale(string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        Locale = locale;
        return this;
    }

    /// <summary>
    /// Configures the faker with strict mode, requiring all properties to have rules.
    /// </summary>
    /// <param name="ensureRulesForAllProperties">Whether to require rules for all properties.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public new EncinaFaker<T> StrictMode(bool ensureRulesForAllProperties)
    {
        base.StrictMode(ensureRulesForAllProperties);
        return this;
    }
}

/// <summary>
/// Extension methods for <see cref="Faker"/> to generate common Encina patterns.
/// </summary>
public static class EncinaFakerExtensions
{
    /// <summary>
    /// Generates a correlation ID in GUID format.
    /// </summary>
    /// <param name="faker">The Bogus randomizer.</param>
    /// <returns>A random GUID as a correlation ID.</returns>
    public static Guid CorrelationId(this Randomizer faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.Guid();
    }

    /// <summary>
    /// Generates a user ID with optional prefix.
    /// </summary>
    /// <param name="faker">The Bogus randomizer.</param>
    /// <param name="prefix">Optional prefix for the user ID.</param>
    /// <returns>A generated user ID string.</returns>
    public static string UserId(this Randomizer faker, string prefix = "user")
    {
        ArgumentNullException.ThrowIfNull(faker);
        return $"{prefix}_{faker.AlphaNumeric(8)}";
    }

    /// <summary>
    /// Generates a tenant ID with optional prefix.
    /// </summary>
    /// <param name="faker">The Bogus randomizer.</param>
    /// <param name="prefix">Optional prefix for the tenant ID.</param>
    /// <returns>A generated tenant ID string.</returns>
    public static string TenantId(this Randomizer faker, string prefix = "tenant")
    {
        ArgumentNullException.ThrowIfNull(faker);
        return $"{prefix}_{faker.AlphaNumeric(6)}";
    }

    /// <summary>
    /// Generates an idempotency key.
    /// </summary>
    /// <param name="faker">The Bogus randomizer.</param>
    /// <returns>A GUID string suitable for idempotency keys.</returns>
    public static string IdempotencyKey(this Randomizer faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.Guid().ToString();
    }

    /// <summary>
    /// Picks a random notification type from common domain event patterns.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <returns>A random notification type name.</returns>
    public static string NotificationType(this Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.PickRandom(
            "OrderCreated",
            "OrderCompleted",
            "OrderCancelled",
            "PaymentReceived",
            "PaymentFailed",
            "ShipmentDispatched",
            "CustomerRegistered",
            "InventoryUpdated");
    }

    /// <summary>
    /// Picks a random request type from common command patterns.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <returns>A random request type name.</returns>
    public static string RequestType(this Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.PickRandom(
            "CreateOrder",
            "UpdateOrder",
            "CancelOrder",
            "ProcessPayment",
            "RefundPayment",
            "RegisterCustomer",
            "UpdateInventory",
            "SendNotification");
    }

    /// <summary>
    /// Picks a random saga type from common saga patterns.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <returns>A random saga type name.</returns>
    public static string SagaType(this Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.PickRandom(
            "OrderFulfillmentSaga",
            "PaymentProcessingSaga",
            "CustomerOnboardingSaga",
            "InventoryReservationSaga",
            "ShippingCoordinationSaga");
    }

    /// <summary>
    /// Picks a random saga status.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <returns>A random saga status.</returns>
    public static string SagaStatus(this Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.PickRandom("Running", "Completed", "Compensating", "Failed");
    }

    /// <summary>
    /// Generates a recent UTC timestamp within the specified days.
    /// </summary>
    /// <param name="date">The Bogus date generator.</param>
    /// <param name="days">Number of days in the past (default: 7).</param>
    /// <returns>A UTC timestamp.</returns>
    public static DateTime RecentUtc(this Date date, int days = 7)
    {
        ArgumentNullException.ThrowIfNull(date);
        return DateTime.SpecifyKind(date.Recent(days), DateTimeKind.Utc);
    }

    /// <summary>
    /// Generates a future UTC timestamp within the specified days.
    /// </summary>
    /// <param name="date">The Bogus date generator.</param>
    /// <param name="days">Number of days in the future (default: 7).</param>
    /// <returns>A UTC timestamp.</returns>
    public static DateTime SoonUtc(this Date date, int days = 7)
    {
        ArgumentNullException.ThrowIfNull(date);
        return DateTime.SpecifyKind(date.Soon(days), DateTimeKind.Utc);
    }

    /// <summary>
    /// Generates JSON content with random key-value pairs.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="propertyCount">Number of properties to include (default: 3).</param>
    /// <returns>A valid JSON string with properly escaped keys and values.</returns>
    public static string JsonContent(this Faker faker, int propertyCount = 3)
    {
        ArgumentNullException.ThrowIfNull(faker);

        var dictionary = new Dictionary<string, string>(propertyCount);
        for (int i = 0; i < propertyCount; i++)
        {
            var key = faker.Lorem.Word();
            var value = faker.Lorem.Sentence();
            // Ensure unique keys by appending index if key already exists
            var uniqueKey = dictionary.ContainsKey(key) ? $"{key}_{i}" : key;
            dictionary[uniqueKey] = value;
        }

        return JsonSerializer.Serialize(dictionary);
    }

    #region Entity ID Generation

    /// <summary>
    /// Generates an entity ID of the specified type.
    /// </summary>
    /// <typeparam name="TId">The ID type (Guid, int, long, or string).</typeparam>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <returns>A generated ID value appropriate for the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when <typeparamref name="TId"/> is not a supported type.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var guidId = faker.Random.EntityId&lt;Guid&gt;();
    /// var intId = faker.Random.EntityId&lt;int&gt;();
    /// var longId = faker.Random.EntityId&lt;long&gt;();
    /// var stringId = faker.Random.EntityId&lt;string&gt;();
    /// </code>
    /// </example>
    public static TId EntityId<TId>(this Randomizer randomizer)
    {
        ArgumentNullException.ThrowIfNull(randomizer);

        return typeof(TId) switch
        {
            Type t when t == typeof(Guid) => (TId)(object)randomizer.Guid(),
            Type t when t == typeof(int) => (TId)(object)randomizer.Int(1, int.MaxValue),
            Type t when t == typeof(long) => (TId)(object)randomizer.Long(1, long.MaxValue),
            Type t when t == typeof(string) => (TId)(object)randomizer.AlphaNumeric(12),
            _ => throw new NotSupportedException($"Entity ID type '{typeof(TId).Name}' is not supported. Supported types: Guid, int, long, string.")
        };
    }

    /// <summary>
    /// Generates a GUID entity ID.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <returns>A random GUID suitable for entity identification.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var id = faker.Random.GuidEntityId();
    /// </code>
    /// </example>
    public static Guid GuidEntityId(this Randomizer randomizer)
    {
        ArgumentNullException.ThrowIfNull(randomizer);
        return randomizer.Guid();
    }

    /// <summary>
    /// Generates a positive integer entity ID.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <param name="min">Minimum value (default: 1).</param>
    /// <param name="max">Maximum value (default: int.MaxValue).</param>
    /// <returns>A positive integer suitable for entity identification.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var id = faker.Random.IntEntityId();
    /// var idInRange = faker.Random.IntEntityId(1000, 9999);
    /// </code>
    /// </example>
    public static int IntEntityId(this Randomizer randomizer, int min = 1, int max = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(randomizer);
        ArgumentOutOfRangeException.ThrowIfLessThan(min, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(max, min);
        return randomizer.Int(min, max);
    }

    /// <summary>
    /// Generates a positive long entity ID.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <param name="min">Minimum value (default: 1).</param>
    /// <param name="max">Maximum value (default: long.MaxValue).</param>
    /// <returns>A positive long suitable for entity identification.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var id = faker.Random.LongEntityId();
    /// var idInRange = faker.Random.LongEntityId(1_000_000, 9_999_999);
    /// </code>
    /// </example>
    public static long LongEntityId(this Randomizer randomizer, long min = 1, long max = long.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(randomizer);
        ArgumentOutOfRangeException.ThrowIfLessThan(min, 1L);
        ArgumentOutOfRangeException.ThrowIfLessThan(max, min);
        return randomizer.Long(min, max);
    }

    /// <summary>
    /// Generates a non-empty string entity ID.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <param name="length">Length of the generated string (default: 12).</param>
    /// <param name="prefix">Optional prefix for the ID.</param>
    /// <returns>A non-empty alphanumeric string suitable for entity identification.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var id = faker.Random.StringEntityId();              // "a1b2c3d4e5f6"
    /// var prefixedId = faker.Random.StringEntityId(8, "ORD"); // "ORD_a1b2c3d4"
    /// </code>
    /// </example>
    public static string StringEntityId(this Randomizer randomizer, int length = 12, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(randomizer);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);

        var id = randomizer.AlphaNumeric(length);
        return string.IsNullOrEmpty(prefix) ? id : $"{prefix}_{id}";
    }

    #endregion

    #region Strongly-Typed ID Generation

    /// <summary>
    /// Generates a value suitable for a strongly-typed ID with the specified underlying type.
    /// </summary>
    /// <typeparam name="TValue">The underlying value type (Guid, int, long, or string).</typeparam>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <returns>A generated value appropriate for the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when <typeparamref name="TValue"/> is not a supported type.</exception>
    /// <remarks>
    /// This method generates values compatible with StronglyTypedId&lt;TValue&gt; from Encina.DomainModeling:
    /// <list type="bullet">
    /// <item><description>Guid: Random GUID</description></item>
    /// <item><description>int: Positive integer (1 to int.MaxValue)</description></item>
    /// <item><description>long: Positive long (1 to long.MaxValue)</description></item>
    /// <item><description>string: Non-empty alphanumeric string (12 characters)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    ///
    /// // Generate values for strongly-typed IDs
    /// var guidValue = faker.Random.StronglyTypedIdValue&lt;Guid&gt;();
    /// var orderId = new OrderId(guidValue);
    ///
    /// var intValue = faker.Random.StronglyTypedIdValue&lt;int&gt;();
    /// var productId = new ProductId(intValue);
    /// </code>
    /// </example>
    public static TValue StronglyTypedIdValue<TValue>(this Randomizer randomizer)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(randomizer);

        return typeof(TValue) switch
        {
            Type t when t == typeof(Guid) => (TValue)(object)randomizer.Guid(),
            Type t when t == typeof(int) => (TValue)(object)randomizer.Int(1, int.MaxValue),
            Type t when t == typeof(long) => (TValue)(object)randomizer.Long(1, long.MaxValue),
            Type t when t == typeof(string) => (TValue)(object)randomizer.AlphaNumeric(12),
            _ => throw new NotSupportedException($"Strongly-typed ID value type '{typeof(TValue).Name}' is not supported. Supported types: Guid, int, long, string.")
        };
    }

    /// <summary>
    /// Generates a GUID value for a GuidStronglyTypedId.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <returns>A random GUID suitable for GuidStronglyTypedId.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var value = faker.Random.GuidStronglyTypedIdValue();
    /// var orderId = new OrderId(value);
    /// </code>
    /// </example>
    public static Guid GuidStronglyTypedIdValue(this Randomizer randomizer) =>
        GuidEntityId(randomizer);

    /// <summary>
    /// Generates a positive integer value for an IntStronglyTypedId.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <param name="min">Minimum value (default: 1).</param>
    /// <param name="max">Maximum value (default: int.MaxValue).</param>
    /// <returns>A positive integer suitable for IntStronglyTypedId.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var value = faker.Random.IntStronglyTypedIdValue();
    /// var productId = new ProductId(value);
    /// </code>
    /// </example>
    public static int IntStronglyTypedIdValue(this Randomizer randomizer, int min = 1, int max = int.MaxValue) =>
        IntEntityId(randomizer, min, max);

    /// <summary>
    /// Generates a positive long value for a LongStronglyTypedId.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <param name="min">Minimum value (default: 1).</param>
    /// <param name="max">Maximum value (default: long.MaxValue).</param>
    /// <returns>A positive long suitable for LongStronglyTypedId.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var value = faker.Random.LongStronglyTypedIdValue();
    /// var transactionId = new TransactionId(value);
    /// </code>
    /// </example>
    public static long LongStronglyTypedIdValue(this Randomizer randomizer, long min = 1, long max = long.MaxValue) =>
        LongEntityId(randomizer, min, max);

    /// <summary>
    /// Generates a non-empty string value for a StringStronglyTypedId.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <param name="length">Length of the generated string (default: 12).</param>
    /// <param name="prefix">Optional prefix for the ID value.</param>
    /// <returns>A non-empty string suitable for StringStronglyTypedId.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is less than 1.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var value = faker.Random.StringStronglyTypedIdValue();
    /// var sku = new Sku(value);
    ///
    /// var prefixedValue = faker.Random.StringStronglyTypedIdValue(8, "SKU");
    /// var prefixedSku = new Sku(prefixedValue); // "SKU_a1b2c3d4"
    /// </code>
    /// </example>
    public static string StringStronglyTypedIdValue(this Randomizer randomizer, int length = 12, string? prefix = null) =>
        StringEntityId(randomizer, length, prefix);

    #endregion

    #region Value Object Generation

    /// <summary>
    /// Generates a random non-negative quantity value.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <param name="min">Minimum value (default: 0).</param>
    /// <param name="max">Maximum value (default: 1000).</param>
    /// <returns>A non-negative integer suitable for Quantity from Encina.DomainModeling.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is negative.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var quantityValue = faker.Random.QuantityValue();
    /// var quantity = Quantity.From(quantityValue);
    ///
    /// var smallQuantity = faker.Random.QuantityValue(1, 10);
    /// </code>
    /// </example>
    public static int QuantityValue(this Randomizer randomizer, int min = 0, int max = 1000)
    {
        ArgumentNullException.ThrowIfNull(randomizer);
        ArgumentOutOfRangeException.ThrowIfNegative(min);
        ArgumentOutOfRangeException.ThrowIfLessThan(max, min);
        return randomizer.Int(min, max);
    }

    /// <summary>
    /// Generates a random percentage value between 0 and 100.
    /// </summary>
    /// <param name="randomizer">The Bogus randomizer.</param>
    /// <param name="min">Minimum value (default: 0).</param>
    /// <param name="max">Maximum value (default: 100).</param>
    /// <param name="decimals">Number of decimal places (default: 2).</param>
    /// <returns>A decimal value between 0 and 100 suitable for Percentage from Encina.DomainModeling.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="randomizer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is negative or <paramref name="max"/> exceeds 100.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var percentValue = faker.Random.PercentageValue();
    /// var percentage = Percentage.From(percentValue);
    ///
    /// var discountPercent = faker.Random.PercentageValue(5, 50);
    /// </code>
    /// </example>
    public static decimal PercentageValue(this Randomizer randomizer, decimal min = 0m, decimal max = 100m, int decimals = 2)
    {
        ArgumentNullException.ThrowIfNull(randomizer);
        ArgumentOutOfRangeException.ThrowIfNegative(min);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(max, 100m);
        ArgumentOutOfRangeException.ThrowIfLessThan(max, min);

        return Math.Round(randomizer.Decimal(min, max), decimals);
    }

    /// <summary>
    /// Generates a random date range.
    /// </summary>
    /// <param name="date">The Bogus date generator.</param>
    /// <param name="daysInPast">Maximum days in the past for start date (default: 30).</param>
    /// <param name="daysSpan">Maximum span between start and end dates (default: 30).</param>
    /// <returns>A tuple of (start, end) dates suitable for DateRange from Encina.DomainModeling.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="date"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="daysInPast"/> or <paramref name="daysSpan"/> is negative.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var (start, end) = faker.Date.DateRangeValue();
    /// var dateRange = DateRange.From(start, end);
    ///
    /// var shortRange = faker.Date.DateRangeValue(daysInPast: 7, daysSpan: 5);
    /// </code>
    /// </example>
    public static (DateOnly Start, DateOnly End) DateRangeValue(this Date date, int daysInPast = 30, int daysSpan = 30)
    {
        ArgumentNullException.ThrowIfNull(date);
        ArgumentOutOfRangeException.ThrowIfNegative(daysInPast);
        ArgumentOutOfRangeException.ThrowIfNegative(daysSpan);

        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);
        var startOffset = date.Random.Int(0, daysInPast);
        var spanDays = date.Random.Int(1, Math.Max(1, daysSpan));

        var start = today.AddDays(-startOffset);
        var end = start.AddDays(spanDays);

        return (start, end);
    }

    /// <summary>
    /// Generates a random time range within a day.
    /// </summary>
    /// <param name="date">The Bogus date generator.</param>
    /// <param name="minHourSpan">Minimum hours between start and end (default: 1).</param>
    /// <param name="maxHourSpan">Maximum hours between start and end (default: 8).</param>
    /// <returns>A tuple of (start, end) times suitable for TimeRange from Encina.DomainModeling.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="date"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters result in invalid time ranges.</exception>
    /// <example>
    /// <code>
    /// var faker = new Faker();
    /// var (start, end) = faker.Date.TimeRangeValue();
    /// var timeRange = TimeRange.From(start, end);
    ///
    /// var shortMeeting = faker.Date.TimeRangeValue(minHourSpan: 1, maxHourSpan: 2);
    /// </code>
    /// </example>
    public static (TimeOnly Start, TimeOnly End) TimeRangeValue(this Date date, int minHourSpan = 1, int maxHourSpan = 8)
    {
        ArgumentNullException.ThrowIfNull(date);
        ArgumentOutOfRangeException.ThrowIfLessThan(minHourSpan, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxHourSpan, 23);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxHourSpan, minHourSpan);

        // Generate start time ensuring room for the span
        var maxStartHour = 24 - maxHourSpan;
        var startHour = date.Random.Int(0, maxStartHour);
        var startMinute = date.Random.Int(0, 59);

        var hourSpan = date.Random.Int(minHourSpan, maxHourSpan);
        var minuteOffset = date.Random.Int(0, 59);

        var start = new TimeOnly(startHour, startMinute);
        var end = start.AddHours(hourSpan).AddMinutes(minuteOffset);

        // Ensure end doesn't wrap around midnight
        if (end < start)
        {
            end = new TimeOnly(23, 59);
        }

        return (start, end);
    }

    #endregion
}
