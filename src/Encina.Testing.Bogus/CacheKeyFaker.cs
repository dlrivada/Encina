using Bogus;

namespace Encina.Testing.Bogus;

/// <summary>
/// Faker for generating realistic cache key test data.
/// </summary>
/// <remarks>
/// <para>
/// Generates cache keys following common caching patterns:
/// <list type="bullet">
/// <item><description>Simple keys: "user_abc123"</description></item>
/// <item><description>Hierarchical keys: "tenant:user:123:profile"</description></item>
/// <item><description>Pattern keys: "product:*:inventory"</description></item>
/// <item><description>Tagged keys: "[cache:v1]user:123"</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// var faker = new CacheKeyFaker();
/// var simpleKey = faker.Generate();
///
/// var hierarchicalKey = new CacheKeyFaker()
///     .WithSegments(3)
///     .Generate();
///
/// var patternKey = new CacheKeyFaker()
///     .AsPattern()
///     .Generate();
/// </code>
/// </para>
/// </remarks>
public sealed class CacheKeyFaker : Faker<string>
{
    private static readonly string[] CommonPrefixes =
    [
        "user", "product", "order", "customer", "inventory",
        "session", "cart", "catalog", "pricing", "config"
    ];

    private static readonly string[] CommonDomains =
    [
        "profile", "settings", "preferences", "permissions",
        "details", "summary", "list", "cache", "data"
    ];

    private string? _prefix;
    private int _segments = 2;
    private bool _asPattern;
    private string? _tag;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheKeyFaker"/> class.
    /// </summary>
    /// <param name="locale">The locale for generating localized data (default: "en").</param>
    public CacheKeyFaker(string locale = "en")
        : base(locale)
    {
        UseSeed(EncinaFaker<object>.DefaultSeed);

        CustomInstantiator(f =>
        {
            var prefix = _prefix ?? f.PickRandom(CommonPrefixes);
            var segments = GenerateSegments(f, _segments, prefix);
            var key = string.Join(":", segments);

            if (_asPattern)
            {
                key = ConvertToPattern(f, key);
            }

            if (!string.IsNullOrEmpty(_tag))
            {
                key = $"[{_tag}]{key}";
            }

            return key;
        });
    }

    /// <summary>
    /// Configures the faker to use a specific prefix for all keys.
    /// </summary>
    /// <param name="prefix">The prefix to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix"/> is null, empty, or whitespace.</exception>
    public CacheKeyFaker WithPrefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _prefix = prefix;
        return this;
    }

    /// <summary>
    /// Configures the number of segments in the key.
    /// </summary>
    /// <param name="segments">The number of segments (1-5).</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="segments"/> is not between 1 and 5.</exception>
    public CacheKeyFaker WithSegments(int segments)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(segments, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(segments, 5);
        _segments = segments;
        return this;
    }

    /// <summary>
    /// Configures the faker to generate pattern keys with wildcards.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public CacheKeyFaker AsPattern()
    {
        _asPattern = true;
        return this;
    }

    /// <summary>
    /// Configures the faker to add a tag prefix to the key.
    /// </summary>
    /// <param name="tag">The tag to add (e.g., "cache:v1").</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is null, empty, or whitespace.</exception>
    public CacheKeyFaker AsTagged(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _tag = tag;
        return this;
    }

    private static List<string> GenerateSegments(Faker f, int count, string prefix)
    {
        var segments = new List<string>(count) { prefix };

        for (int i = 1; i < count; i++)
        {
            // Alternate between ID-like values and domain names
            var segment = i % 2 == 1
                ? f.Random.AlphaNumeric(8)
                : f.PickRandom(CommonDomains);
            segments.Add(segment);
        }

        return segments;
    }

    private static string ConvertToPattern(Faker f, string key)
    {
        // Replace one or more segments with wildcards
        var parts = key.Split(':');
        if (parts.Length <= 1)
        {
            return key + ":*";
        }

        var indexToReplace = f.Random.Int(1, parts.Length - 1);
        parts[indexToReplace] = f.Random.Bool() ? "*" : "?";

        return string.Join(":", parts);
    }
}
