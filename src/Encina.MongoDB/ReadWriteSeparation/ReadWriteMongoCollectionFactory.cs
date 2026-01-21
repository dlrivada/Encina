using Encina.Messaging.ReadWriteSeparation;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.ReadWriteSeparation;

/// <summary>
/// MongoDB implementation of <see cref="IReadWriteMongoCollectionFactory"/> that routes
/// operations to primary or secondary members based on routing context.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates MongoDB collections configured with appropriate read preferences
/// based on the current <see cref="DatabaseRoutingContext"/>:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Write/ForceWrite intent</term>
///     <description>Uses Primary read preference</description>
///   </item>
///   <item>
///     <term>Read intent</term>
///     <description>Uses configured read preference (default: SecondaryPreferred)</description>
///   </item>
/// </list>
/// <para>
/// The factory uses <see cref="IMongoCollection{TDocument}.WithReadPreference(ReadPreference)"/>
/// to create collection instances with the appropriate read preference while sharing the
/// underlying connection pool.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaMongoDB(options =>
/// {
///     options.ConnectionString = "mongodb://localhost:27017/?replicaSet=rs0";
///     options.DatabaseName = "MyApp";
///     options.UseReadWriteSeparation = true;
///     options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
/// });
///
/// // Usage
/// public class OrderService(IReadWriteMongoCollectionFactory collectionFactory)
/// {
///     public async Task&lt;Order?&gt; GetOrderAsync(Guid id, CancellationToken ct)
///     {
///         // Uses current routing context to determine read preference
///         var collection = await collectionFactory.GetCollectionAsync&lt;Order&gt;("orders", ct);
///         return await collection.Find(o =&gt; o.Id == id).FirstOrDefaultAsync(ct);
///     }
/// }
/// </code>
/// </example>
public sealed class ReadWriteMongoCollectionFactory : IReadWriteMongoCollectionFactory
{
    private readonly IMongoClient _mongoClient;
    private readonly EncinaMongoDbOptions _mongoOptions;
    private readonly MongoReadWriteSeparationOptions _rwOptions;
    private readonly ReadPreference _readPreference;
    private readonly ReadConcern _readConcern;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteMongoCollectionFactory"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="mongoOptions">The MongoDB configuration options.</param>
    public ReadWriteMongoCollectionFactory(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> mongoOptions)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(mongoOptions);

        _mongoClient = mongoClient;
        _mongoOptions = mongoOptions.Value;
        _rwOptions = _mongoOptions.ReadWriteSeparationOptions;

        // Convert Encina read preference to MongoDB driver read preference
        _readPreference = ConvertReadPreference(_rwOptions.ReadPreference, _rwOptions.MaxStaleness);
        _readConcern = ConvertReadConcern(_rwOptions.ReadConcern);
    }

    /// <inheritdoc/>
    public ValueTask<IMongoCollection<TEntity>> GetWriteCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);
        cancellationToken.ThrowIfCancellationRequested();

        var database = _mongoClient.GetDatabase(_mongoOptions.DatabaseName);
        var collection = database.GetCollection<TEntity>(collectionName)
            .WithReadPreference(ReadPreference.Primary);

        return ValueTask.FromResult(collection);
    }

    /// <inheritdoc/>
    public ValueTask<IMongoCollection<TEntity>> GetReadCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);
        cancellationToken.ThrowIfCancellationRequested();

        var database = _mongoClient.GetDatabase(_mongoOptions.DatabaseName);
        var collection = database.GetCollection<TEntity>(collectionName)
            .WithReadPreference(_readPreference)
            .WithReadConcern(_readConcern);

        return ValueTask.FromResult(collection);
    }

    /// <inheritdoc/>
    public ValueTask<IMongoCollection<TEntity>> GetCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);
        cancellationToken.ThrowIfCancellationRequested();

        var database = _mongoClient.GetDatabase(_mongoOptions.DatabaseName);
        var baseCollection = database.GetCollection<TEntity>(collectionName);

        // Determine read preference based on routing context
        var intent = DatabaseRoutingContext.CurrentIntent;
        var collection = intent switch
        {
            DatabaseIntent.Read => baseCollection
                .WithReadPreference(_readPreference)
                .WithReadConcern(_readConcern),
            DatabaseIntent.Write or DatabaseIntent.ForceWrite => baseCollection
                .WithReadPreference(ReadPreference.Primary),
            _ => baseCollection.WithReadPreference(ReadPreference.Primary) // Safe default
        };

        return ValueTask.FromResult(collection);
    }

    /// <inheritdoc/>
    public ValueTask<string> GetDatabaseNameAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(_mongoOptions.DatabaseName))
        {
            throw new InvalidOperationException(
                "No database name configured. Set EncinaMongoDbOptions.DatabaseName in your configuration.");
        }

        return ValueTask.FromResult(_mongoOptions.DatabaseName);
    }

    /// <summary>
    /// Converts an Encina read preference to a MongoDB driver read preference.
    /// </summary>
    private static ReadPreference ConvertReadPreference(MongoReadPreference preference, TimeSpan? maxStaleness)
    {
        ReadPreference result = preference switch
        {
            MongoReadPreference.Primary => ReadPreference.Primary,
            MongoReadPreference.PrimaryPreferred => ReadPreference.PrimaryPreferred,
            MongoReadPreference.Secondary => ReadPreference.Secondary,
            MongoReadPreference.SecondaryPreferred => ReadPreference.SecondaryPreferred,
            MongoReadPreference.Nearest => ReadPreference.Nearest,
            _ => ReadPreference.SecondaryPreferred
        };

        // Apply max staleness if configured and applicable
        if (maxStaleness.HasValue && result.ReadPreferenceMode != ReadPreferenceMode.Primary)
        {
            result = result.With(maxStaleness: maxStaleness.Value);
        }

        return result;
    }

    /// <summary>
    /// Converts an Encina read concern to a MongoDB driver read concern.
    /// </summary>
    private static ReadConcern ConvertReadConcern(MongoReadConcern concern)
    {
        return concern switch
        {
            MongoReadConcern.Default => ReadConcern.Default,
            MongoReadConcern.Local => ReadConcern.Local,
            MongoReadConcern.Majority => ReadConcern.Majority,
            MongoReadConcern.Linearizable => ReadConcern.Linearizable,
            MongoReadConcern.Available => ReadConcern.Available,
            MongoReadConcern.Snapshot => ReadConcern.Snapshot,
            _ => ReadConcern.Majority
        };
    }
}
