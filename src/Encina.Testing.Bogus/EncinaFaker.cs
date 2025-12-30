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
}
