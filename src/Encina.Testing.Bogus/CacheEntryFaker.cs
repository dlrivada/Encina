using Bogus;

namespace Encina.Testing.Bogus;

/// <summary>
/// Faker for generating complete cache entry test data (key + value + options).
/// </summary>
/// <remarks>
/// <para>
/// Generates cache entries with all associated metadata:
/// <list type="bullet">
/// <item><description>Key: Hierarchical cache key</description></item>
/// <item><description>Value: Typed cache value</description></item>
/// <item><description>Expiration: Absolute expiration time</description></item>
/// <item><description>SlidingExpiration: Optional sliding window</description></item>
/// <item><description>Tags: Cache entry tags for invalidation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// var faker = new CacheEntryFaker&lt;ProductDto&gt;();
/// var entry = faker.Generate();
///
/// var taggedEntry = new CacheEntryFaker&lt;ProductDto&gt;()
///     .WithTags("catalog", "products")
///     .WithExpiration(TimeSpan.FromMinutes(30))
///     .Generate();
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the cache value.</typeparam>
public sealed class CacheEntryFaker<T> : Faker<CacheEntry<T>>
    where T : class
{
    private TimeSpan? _expiration;
    private TimeSpan? _slidingExpiration;
    private TimeSpan? _absoluteExpiration;
    private string[]? _tags;
    private Func<Faker, T>? _valueGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntryFaker{T}"/> class.
    /// </summary>
    /// <param name="locale">The locale for generating localized data (default: "en").</param>
    public CacheEntryFaker(string locale = "en")
        : base(locale)
    {
        UseSeed(EncinaFaker<object>.DefaultSeed);
        CustomInstantiator(_ => new CacheEntry<T>());

        RuleFor(e => e.Key, f => GenerateKey(f));
        RuleFor(e => e.Value, f => _valueGenerator != null ? _valueGenerator(f) : default!);
        RuleFor(e => e.Expiration, f => _expiration ?? f.CacheExpiration());
        RuleFor(e => e.SlidingExpiration, _ => _slidingExpiration);
        RuleFor(e => e.AbsoluteExpiration, _ => _absoluteExpiration);
        RuleFor(e => e.Tags, _ => _tags ?? []);
        RuleFor(e => e.CreatedAtUtc, f => f.Date.RecentUtc(1));
    }

    /// <summary>
    /// Configures the expiration time for the cache entry.
    /// </summary>
    /// <param name="expiration">The expiration time.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public CacheEntryFaker<T> WithExpiration(TimeSpan expiration)
    {
        _expiration = expiration;
        return this;
    }

    /// <summary>
    /// Configures sliding expiration for the cache entry.
    /// </summary>
    /// <param name="slidingExpiration">The sliding expiration window.</param>
    /// <param name="absoluteExpiration">Optional absolute expiration limit.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public CacheEntryFaker<T> WithSlidingExpiration(TimeSpan slidingExpiration, TimeSpan? absoluteExpiration = null)
    {
        _slidingExpiration = slidingExpiration;
        _absoluteExpiration = absoluteExpiration;
        return this;
    }

    /// <summary>
    /// Configures tags for the cache entry.
    /// </summary>
    /// <param name="tags">The tags to associate with the entry.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public CacheEntryFaker<T> WithTags(params string[] tags)
    {
        _tags = tags;
        return this;
    }

    /// <summary>
    /// Configures a custom value generator for the cache entry.
    /// </summary>
    /// <param name="valueGenerator">The value generator function.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="valueGenerator"/> is null.</exception>
    public CacheEntryFaker<T> WithValue(Func<Faker, T> valueGenerator)
    {
        ArgumentNullException.ThrowIfNull(valueGenerator);
        _valueGenerator = valueGenerator;
        return this;
    }

    private static string GenerateKey(Faker _) // NOSONAR S1172: Faker parameter required for Bogus delegate signature
    {
        var keyFaker = new CacheKeyFaker();
        return keyFaker.Generate();
    }
}

/// <summary>
/// Represents a cache entry with all associated metadata.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public sealed class CacheEntry<T>
{
    /// <summary>
    /// Gets or sets the cache key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cached value.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration window.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration limit.
    /// </summary>
    public TimeSpan? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Gets or sets the cache entry tags.
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets when the entry was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Extension methods for pub/sub channel generation.
/// </summary>
public static class PubSubExtensions
{
    private static readonly string[] CommonChannels =
    [
        "cache:invalidate",
        "cache:refresh",
        "events:domain",
        "events:integration",
        "notifications:user",
        "notifications:system",
        "commands:async",
        "health:status"
    ];

    /// <summary>
    /// Generates a random pub/sub channel name.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <returns>A random channel name.</returns>
    public static string PubSubChannel(this Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return faker.PickRandom(CommonChannels);
    }

    /// <summary>
    /// Generates a pub/sub channel name with a specific prefix.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="prefix">The channel prefix.</param>
    /// <returns>A channel name with the given prefix.</returns>
    public static string PubSubChannel(this Faker faker, string prefix)
    {
        ArgumentNullException.ThrowIfNull(faker);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var suffix = faker.Random.AlphaNumeric(8);
        return $"{prefix}:{suffix}";
    }

    /// <summary>
    /// Generates a pub/sub pattern for subscriptions.
    /// </summary>
    /// <param name="faker">The Bogus faker instance.</param>
    /// <param name="prefix">The pattern prefix.</param>
    /// <returns>A pattern string with wildcard.</returns>
    public static string PubSubPattern(this Faker faker, string prefix)
    {
        ArgumentNullException.ThrowIfNull(faker);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        return $"{prefix}:*";
    }
}
