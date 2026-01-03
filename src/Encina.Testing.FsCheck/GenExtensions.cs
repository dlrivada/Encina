using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using LanguageExt;

namespace Encina.Testing.FsCheck;

/// <summary>
/// Extension methods for FsCheck generators tailored for Encina types.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide convenient ways to create generators for
/// common patterns in Encina applications, such as Either types,
/// optional values, and domain-specific data.
/// </para>
/// </remarks>
public static class GenExtensions
{
    /// <summary>
    /// Creates a generator that produces Either values with the specified right generator.
    /// </summary>
    /// <typeparam name="T">The right value type.</typeparam>
    /// <param name="rightGen">Generator for right values.</param>
    /// <returns>A generator that produces Either values.</returns>
    public static Gen<Either<EncinaError, T>> ToEither<T>(this Gen<T> rightGen)
    {
        ArgumentNullException.ThrowIfNull(rightGen);

        var leftGen = Gen.Select(EncinaArbitraries.EncinaError().Generator, err => Either<EncinaError, T>.Left(err));
        var successGen = Gen.Select(rightGen, val => Either<EncinaError, T>.Right(val));

        return Gen.OneOf(leftGen, successGen);
    }

    /// <summary>
    /// Creates a generator that always produces successful Either values.
    /// </summary>
    /// <typeparam name="T">The right value type.</typeparam>
    /// <param name="rightGen">Generator for right values.</param>
    /// <returns>A generator that produces Right Either values.</returns>
    public static Gen<Either<EncinaError, T>> ToSuccess<T>(this Gen<T> rightGen)
    {
        ArgumentNullException.ThrowIfNull(rightGen);
        return Gen.Select(rightGen, val => Either<EncinaError, T>.Right(val));
    }

    /// <summary>
    /// Creates a generator that always produces failed Either values.
    /// </summary>
    /// <typeparam name="T">The right value type (not generated).</typeparam>
    /// <param name="errorGen">Generator for error values.</param>
    /// <returns>A generator that produces Left Either values.</returns>
    public static Gen<Either<EncinaError, T>> ToFailure<T>(this Gen<EncinaError> errorGen)
    {
        ArgumentNullException.ThrowIfNull(errorGen);
        return Gen.Select(errorGen, err => Either<EncinaError, T>.Left(err));
    }

    /// <summary>
    /// Creates a generator that produces nullable values with a probability of null.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="gen">The base generator.</param>
    /// <param name="nullProbability">Probability of generating null (0.0 to 1.0).</param>
    /// <returns>A generator that may produce null values.</returns>
    public static Gen<T?> OrNull<T>(this Gen<T> gen, double nullProbability = 0.2)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(gen);
        ArgumentOutOfRangeException.ThrowIfLessThan(nullProbability, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nullProbability, 1.0);

        var nullWeight = (int)(nullProbability * 100);
        var valueWeight = 100 - nullWeight;

        return Gen.Frequency(
            (nullWeight, Gen.Constant<T?>(null)),
            (valueWeight, Gen.Select(gen, v => (T?)v)));
    }

    /// <summary>
    /// Creates a generator that produces nullable value types with a probability of null.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="gen">The base generator.</param>
    /// <param name="nullProbability">Probability of generating null (0.0 to 1.0).</param>
    /// <returns>A generator that may produce null values.</returns>
    public static Gen<T?> OrNullValue<T>(this Gen<T> gen, double nullProbability = 0.2)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(gen);
        ArgumentOutOfRangeException.ThrowIfLessThan(nullProbability, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nullProbability, 1.0);

        var nullWeight = (int)(nullProbability * 100);
        var valueWeight = 100 - nullWeight;

        return Gen.Frequency(
            (nullWeight, Gen.Constant<T?>(null)),
            (valueWeight, Gen.Select(gen, v => (T?)v)));
    }

    /// <summary>
    /// Creates a generator that produces non-empty strings.
    /// </summary>
    /// <returns>A generator for non-empty strings.</returns>
    public static Gen<string> NonEmptyString()
    {
        return Gen.Select(ArbMap.Default.GeneratorFor<NonEmptyString>(), s => s.Get);
    }

    /// <summary>
    /// Creates a generator that produces alphanumeric strings of a specified length.
    /// </summary>
    /// <param name="minLength">Minimum string length.</param>
    /// <param name="maxLength">Maximum string length.</param>
    /// <returns>A generator for alphanumeric strings.</returns>
    public static Gen<string> AlphaNumericString(int minLength = 1, int maxLength = 50)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, minLength);

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return Gen.SelectMany(
            Gen.Choose(minLength, maxLength),
            length => Gen.Select(
                Gen.ArrayOf(Gen.Elements<char>(chars.ToCharArray()), length),
                arr => new string(arr)));
    }

    /// <summary>
    /// Creates a generator that produces valid JSON object strings.
    /// </summary>
    /// <param name="maxProperties">Maximum number of properties.</param>
    /// <returns>A generator for JSON object strings.</returns>
    public static Gen<string> JsonObject(int maxProperties = 5)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxProperties);

        return Gen.SelectMany(
            Gen.Choose(0, maxProperties),
            count =>
            {
                if (count == 0) return Gen.Constant("{}");

                return Gen.Select(
                    Gen.ArrayOf(JsonProperty(), count),
                    props => $"{{{string.Join(",", props)}}}");
            });
    }

    /// <summary>
    /// Creates a generator for a single JSON property.
    /// </summary>
    private static Gen<string> JsonProperty()
    {
        var keyGen = AlphaNumericString(1, 20);
        var valueGen = Gen.OneOf(
            Gen.Select(ArbMap.Default.GeneratorFor<int>(), i => i.ToString(CultureInfo.InvariantCulture)),
            Gen.Select(ArbMap.Default.GeneratorFor<bool>(), b => b.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()),
            Gen.Select(AlphaNumericString(1, 30), s => $"\"{s}\""),
            Gen.Constant("null"));

        return Gen.SelectMany(
            keyGen,
            key => Gen.Select(
                valueGen,
                value => $"\"{key}\":{value}"));
    }

    /// <summary>
    /// Creates a generator that produces UTC DateTime values within a range.
    /// </summary>
    /// <param name="daysFromNow">Number of days in either direction from now.</param>
    /// <returns>A generator for UTC DateTime values.</returns>
    public static Gen<DateTime> UtcDateTime(int daysFromNow = 365)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(daysFromNow);

        return Gen.Select(
            Gen.Choose(-daysFromNow, daysFromNow),
            days => DateTime.UtcNow.AddDays(days));
    }

    /// <summary>
    /// Creates a generator that produces past UTC DateTime values.
    /// </summary>
    /// <param name="maxDaysAgo">Maximum number of days in the past. When 0, returns current time.
    /// When greater than 0, generates strictly past DateTime values (1 to maxDaysAgo days ago).</param>
    /// <returns>A generator for past UTC DateTime values.</returns>
    public static Gen<DateTime> PastUtcDateTime(int maxDaysAgo = 365)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxDaysAgo);

        if (maxDaysAgo == 0)
        {
            return Gen.Constant(DateTime.UtcNow);
        }

        return Gen.Select(
            Gen.Choose(1, maxDaysAgo),
            days => DateTime.UtcNow.AddDays(-days));
    }

    /// <summary>
    /// Creates a generator that produces future UTC DateTime values.
    /// </summary>
    /// <param name="maxDaysAhead">Maximum number of days in the future (must be greater than 0).</param>
    /// <returns>A generator for future UTC DateTime values.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxDaysAhead is less than or equal to 0.</exception>
    public static Gen<DateTime> FutureUtcDateTime(int maxDaysAhead = 365)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDaysAhead);

        return Gen.Select(
            Gen.Choose(1, maxDaysAhead),
            days => DateTime.UtcNow.AddDays(days));
    }

    /// <summary>
    /// Creates a generator that produces valid cron expressions.
    /// </summary>
    /// <returns>A generator for cron expressions.</returns>
    public static Gen<string> CronExpression()
    {
        return Gen.Elements(
            "* * * * *",       // Every minute
            "0 * * * *",       // Every hour
            "0 0 * * *",       // Every day at midnight
            "0 0 * * 0",       // Every Sunday at midnight
            "0 0 1 * *",       // First day of every month
            "*/5 * * * *",     // Every 5 minutes
            "*/15 * * * *",    // Every 15 minutes
            "0 */2 * * *",     // Every 2 hours
            "0 9 * * 1-5",     // 9am on weekdays
            "0 0,12 * * *");   // Midnight and noon
    }

    /// <summary>
    /// Creates a generator that produces email-like strings.
    /// </summary>
    /// <returns>A generator for email addresses.</returns>
    public static Gen<string> EmailAddress()
    {
        var domains = new[] { "example.com", "test.org", "demo.net", "sample.io" };
        return Gen.SelectMany(
            AlphaNumericString(3, 15),
            local => Gen.Select(
                Gen.Elements(domains),
                domain => $"{local}@{domain}"));
    }

    /// <summary>
    /// Creates a generator that produces positive decimal values.
    /// </summary>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <returns>A generator for positive decimal values.</returns>
    public static Gen<decimal> PositiveDecimal(decimal min = 0.01m, decimal max = 10000m)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(min);
        ArgumentOutOfRangeException.ThrowIfLessThan(max, min);

        var minCents = (int)(min * 100);
        var maxCents = (int)(max * 100);

        return Gen.Select(Gen.Choose(minCents, maxCents), cents => cents / 100m);
    }

    /// <summary>
    /// Creates a generator that produces collections of generated values.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="elementGen">Generator for elements.</param>
    /// <param name="minCount">Minimum number of elements.</param>
    /// <param name="maxCount">Maximum number of elements.</param>
    /// <returns>A generator for collections.</returns>
    public static Gen<IReadOnlyList<T>> ListOf<T>(this Gen<T> elementGen, int minCount = 0, int maxCount = 10)
    {
        ArgumentNullException.ThrowIfNull(elementGen);
        ArgumentOutOfRangeException.ThrowIfNegative(minCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, minCount);

        return Gen.SelectMany(
            Gen.Choose(minCount, maxCount),
            count => Gen.Select(
                Gen.ListOf(elementGen, count),
                list => (IReadOnlyList<T>)list.ToList().AsReadOnly()));
    }

    /// <summary>
    /// Creates a generator that produces non-empty collections.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="elementGen">Generator for elements.</param>
    /// <param name="maxCount">Maximum number of elements.</param>
    /// <returns>A generator for non-empty collections.</returns>
    public static Gen<IReadOnlyList<T>> NonEmptyListOf<T>(this Gen<T> elementGen, int maxCount = 10)
    {
        return elementGen.ListOf(1, maxCount);
    }
}
