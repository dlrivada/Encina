using Bogus;
using FsCheck;

namespace Encina.TestInfrastructure.PropertyTests;

/// <summary>
/// Factory for creating FsCheck Arbitraries that use Bogus Faker for data generation.
/// This bridges FsCheck's property-based testing with Bogus's realistic data generation.
/// </summary>
/// <remarks>
/// <para>
/// The pattern allows using Bogus Fakers with FsCheck by:
/// <list type="bullet">
/// <item><description>Converting FsCheck's random seed to Bogus Randomizer</description></item>
/// <item><description>Using the Faker's generation logic with reproducible seeds</description></item>
/// <item><description>Generating realistic test data in property-based tests</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// // Use in a property test with PositiveInt seed
/// [Property(MaxTest = 100)]
/// public bool MyProperty(PositiveInt seed)
/// {
///     var data = BogusArbitrary.GenerateWith(seed.Get, faker => faker.Lorem.Word());
///     return !string.IsNullOrEmpty(data);
/// }
/// </code>
/// </para>
/// </remarks>
public static class BogusArbitrary
{
    /// <summary>
    /// Generates a value using a Bogus Faker with the specified seed.
    /// </summary>
    /// <typeparam name="T">The type to generate.</typeparam>
    /// <param name="seed">The seed for reproducible generation.</param>
    /// <param name="generator">A function that creates an instance using a Faker.</param>
    /// <returns>The generated instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator"/> is null.</exception>
    /// <example>
    /// <code>
    /// [Property(MaxTest = 100)]
    /// public bool MessageIdIsNeverEmpty(PositiveInt seed)
    /// {
    ///     var message = BogusArbitrary.GenerateWith(seed.Get, faker => new InboxMessage
    ///     {
    ///         MessageId = faker.Random.Guid().ToString(),
    ///         RequestType = faker.PickRandom("CreateOrder", "UpdateOrder")
    ///     });
    ///     return !string.IsNullOrEmpty(message.MessageId);
    /// }
    /// </code>
    /// </example>
    public static T GenerateWith<T>(int seed, Func<Faker, T> generator)
    {
        ArgumentNullException.ThrowIfNull(generator);

        var faker = new Faker { Random = new Randomizer(seed) };
        return generator(faker);
    }

    /// <summary>
    /// Generates a value using a Bogus Faker instance with the specified seed.
    /// </summary>
    /// <typeparam name="T">The type to generate.</typeparam>
    /// <param name="seed">The seed for reproducible generation.</param>
    /// <param name="fakerFactory">A factory function that creates a new Faker instance.</param>
    /// <returns>The generated instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fakerFactory"/> is null.</exception>
    /// <example>
    /// <code>
    /// [Property(MaxTest = 100)]
    /// public bool GeneratedMessagesAreValid(PositiveInt seed)
    /// {
    ///     var message = BogusArbitrary.GenerateFromFaker(seed.Get, () => new InboxMessageFaker());
    ///     return message.MessageId != null;
    /// }
    /// </code>
    /// </example>
    public static T GenerateFromFaker<T>(int seed, Func<Faker<T>> fakerFactory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(fakerFactory);

        var faker = fakerFactory();
        faker.UseSeed(seed);
        return faker.Generate();
    }

    /// <summary>
    /// Generates a list of values using a Bogus Faker with the specified seed.
    /// </summary>
    /// <typeparam name="T">The type to generate.</typeparam>
    /// <param name="seed">The seed for reproducible generation.</param>
    /// <param name="generator">A function that creates an instance using a Faker.</param>
    /// <param name="count">The number of items to generate.</param>
    /// <returns>A list of generated instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <example>
    /// <code>
    /// [Property(MaxTest = 100)]
    /// public bool AllMessagesHaveUniqueIds(PositiveInt seed)
    /// {
    ///     var messages = BogusArbitrary.GenerateListWith(seed.Get, faker => new InboxMessage
    ///     {
    ///         MessageId = faker.Random.Guid().ToString()
    ///     }, count: 10);
    ///     return messages.Select(m => m.MessageId).Distinct().Count() == 10;
    /// }
    /// </code>
    /// </example>
    public static IReadOnlyList<T> GenerateListWith<T>(int seed, Func<Faker, T> generator, int count)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var faker = new Faker { Random = new Randomizer(seed) };
        var items = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(generator(faker));
        }

        return items;
    }

    /// <summary>
    /// Creates a Bogus Randomizer from an FsCheck seed.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <returns>A Bogus Randomizer initialized with the seed.</returns>
    /// <example>
    /// <code>
    /// [Property(MaxTest = 100)]
    /// public bool RandomizerIsReproducible(PositiveInt seed)
    /// {
    ///     var randomizer1 = BogusArbitrary.CreateRandomizer(seed.Get);
    ///     var randomizer2 = BogusArbitrary.CreateRandomizer(seed.Get);
    ///     return randomizer1.Int() == randomizer2.Int();
    /// }
    /// </code>
    /// </example>
    public static Randomizer CreateRandomizer(int seed) => new(seed);

    /// <summary>
    /// Creates a Bogus Faker from an FsCheck seed.
    /// </summary>
    /// <param name="seed">The seed for random generation.</param>
    /// <param name="locale">The locale for generating localized data (default: "en").</param>
    /// <returns>A Bogus Faker initialized with the seed.</returns>
    /// <example>
    /// <code>
    /// [Property(MaxTest = 100)]
    /// public bool FakerIsReproducible(PositiveInt seed)
    /// {
    ///     var faker1 = BogusArbitrary.CreateFaker(seed.Get);
    ///     var faker2 = BogusArbitrary.CreateFaker(seed.Get);
    ///     return faker1.Name.FirstName() == faker2.Name.FirstName();
    /// }
    /// </code>
    /// </example>
    public static Faker CreateFaker(int seed, string locale = "en")
        => new(locale) { Random = new Randomizer(seed) };
}
