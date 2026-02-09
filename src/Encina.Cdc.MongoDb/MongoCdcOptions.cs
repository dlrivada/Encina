using MongoDB.Driver;

namespace Encina.Cdc.MongoDb;

/// <summary>
/// Configuration options for the MongoDB Change Streams CDC connector.
/// </summary>
/// <remarks>
/// <para>
/// MongoDB Change Streams require a replica set or sharded cluster.
/// Standalone MongoDB instances do not support change streams.
/// </para>
/// <para>
/// For before-values on updates and deletes, MongoDB 6.0+ is required
/// with <c>changeStreamPreAndPostImages</c> enabled on the collection.
/// </para>
/// </remarks>
public sealed class MongoCdcOptions
{
    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name to watch for changes.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection names to watch.
    /// When empty, all collections in the database are watched.
    /// </summary>
    public string[] CollectionNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the full document option for change stream events.
    /// Controls whether the full document is included in change notifications.
    /// Default is <see cref="ChangeStreamFullDocumentOption.UpdateLookup"/>.
    /// </summary>
    public ChangeStreamFullDocumentOption FullDocument { get; set; } =
        ChangeStreamFullDocumentOption.UpdateLookup;

    /// <summary>
    /// Gets or sets whether to watch the entire database or individual collections.
    /// When <c>true</c>, uses <c>database.Watch()</c> instead of <c>collection.Watch()</c>.
    /// Default is <c>true</c>.
    /// </summary>
    public bool WatchDatabase { get; set; } = true;
}
