using System.Text.Json;
using Bogus;

namespace Encina.Testing.Bogus;

/// <summary>
/// Faker for generating realistic cache value test data.
/// </summary>
/// <remarks>
/// <para>
/// Generates typed cache values suitable for various caching scenarios:
/// <list type="bullet">
/// <item><description>Simple types: strings, integers, decimals</description></item>
/// <item><description>Complex objects: DTOs, domain entities</description></item>
/// <item><description>Collections: lists, arrays</description></item>
/// <item><description>JSON representations</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// var stringFaker = new CacheValueFaker&lt;string&gt;();
/// var stringValue = stringFaker.Generate();
///
/// var dtoFaker = new CacheValueFaker&lt;ProductDto&gt;()
///     .Configure(f => f.RuleFor(p => p.Name, fake => fake.Commerce.ProductName()));
/// var product = dtoFaker.Generate();
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="T">The type of cache value to generate.</typeparam>
public sealed class CacheValueFaker<T> : Faker<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheValueFaker{T}"/> class.
    /// </summary>
    /// <param name="locale">The locale for generating localized data (default: "en").</param>
    public CacheValueFaker(string locale = "en")
        : base(locale)
    {
        UseSeed(EncinaFaker<object>.DefaultSeed);
    }

    /// <summary>
    /// Configures the faker with custom rules.
    /// </summary>
    /// <param name="configure">Action to configure the faker rules.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    public CacheValueFaker<T> Configure(Action<CacheValueFaker<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(this);
        return this;
    }
}

/// <summary>
/// Extension methods for cache value generation.
/// </summary>
public static class CacheValueExtensions
{
    /// <summary>
    /// Generates a random string cache value.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="minLength">Minimum length (default: 10).</param>
    /// <param name="maxLength">Maximum length (default: 100).</param>
    /// <returns>A random string value.</returns>
    public static string CacheStringValue(this Faker faker, int minLength = 10, int maxLength = 100)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.Random.String2(faker.Random.Int(minLength, maxLength));
    }

    /// <summary>
    /// Generates a random integer cache value.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="min">Minimum value (default: 0).</param>
    /// <param name="max">Maximum value (default: int.MaxValue).</param>
    /// <returns>A random integer value.</returns>
    public static int CacheIntValue(this Faker faker, int min = 0, int max = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.Random.Int(min, max);
    }

    /// <summary>
    /// Generates a random decimal cache value.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="min">Minimum value (default: 0).</param>
    /// <param name="max">Maximum value (default: 10000).</param>
    /// <param name="decimals">Number of decimal places (default: 2).</param>
    /// <returns>A random decimal value.</returns>
    public static decimal CacheDecimalValue(this Faker faker, decimal min = 0m, decimal max = 10000m, int decimals = 2)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return Math.Round(faker.Random.Decimal(min, max), decimals);
    }

    /// <summary>
    /// Generates a random byte array cache value.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="length">Length of the byte array (default: 256).</param>
    /// <returns>A random byte array.</returns>
    public static byte[] CacheBytesValue(this Faker faker, int length = 256)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.Random.Bytes(length);
    }

    /// <summary>
    /// Generates a random JSON object cache value.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="propertyCount">Number of properties (default: 5).</param>
    /// <returns>A JSON string representing an object.</returns>
    public static string CacheJsonValue(this Faker faker, int propertyCount = 5)
    {
        ArgumentNullException.ThrowIfNull(faker);

        var dict = new Dictionary<string, object>(propertyCount);
        for (int i = 0; i < propertyCount; i++)
        {
            var key = faker.Lorem.Word();
            var uniqueKey = dict.ContainsKey(key) ? $"{key}_{i}" : key;

            // Generate varied value types
            dict[uniqueKey] = faker.Random.Int(0, 3) switch
            {
                0 => faker.Lorem.Sentence(),
                1 => faker.Random.Int(0, 1000),
                2 => faker.Random.Bool(),
                _ => faker.Random.Decimal(0, 100)
            };
        }

        return JsonSerializer.Serialize(dict);
    }

    /// <summary>
    /// Generates a random list of strings cache value.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="count">Number of items (default: 5).</param>
    /// <returns>A list of random strings.</returns>
    public static List<string> CacheStringListValue(this Faker faker, int count = 5)
    {
        ArgumentNullException.ThrowIfNull(faker);

        var list = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(faker.Lorem.Word());
        }

        return list;
    }

    /// <summary>
    /// Generates a TimeSpan suitable for cache expiration.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="minMinutes">Minimum minutes (default: 1).</param>
    /// <param name="maxMinutes">Maximum minutes (default: 60).</param>
    /// <returns>A TimeSpan for cache expiration.</returns>
    public static TimeSpan CacheExpiration(this Faker faker, int minMinutes = 1, int maxMinutes = 60)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return TimeSpan.FromMinutes(faker.Random.Int(minMinutes, maxMinutes));
    }

    /// <summary>
    /// Generates a sliding expiration TimeSpan.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="minMinutes">Minimum minutes (default: 5).</param>
    /// <param name="maxMinutes">Maximum minutes (default: 30).</param>
    /// <returns>A TimeSpan for sliding expiration.</returns>
    public static TimeSpan CacheSlidingExpiration(this Faker faker, int minMinutes = 5, int maxMinutes = 30)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return TimeSpan.FromMinutes(faker.Random.Int(minMinutes, maxMinutes));
    }

    /// <summary>
    /// Generates an absolute expiration TimeSpan (longer than sliding).
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="minHours">Minimum hours (default: 1).</param>
    /// <param name="maxHours">Maximum hours (default: 24).</param>
    /// <returns>A TimeSpan for absolute expiration.</returns>
    public static TimeSpan CacheAbsoluteExpiration(this Faker faker, int minHours = 1, int maxHours = 24)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return TimeSpan.FromHours(faker.Random.Int(minHours, maxHours));
    }
}
