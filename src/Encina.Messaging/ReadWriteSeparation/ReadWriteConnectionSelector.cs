using Microsoft.Extensions.Options;

namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Default implementation of <see cref="IReadWriteConnectionSelector"/> that uses
/// an <see cref="IReplicaSelector"/> to select read replicas.
/// </summary>
/// <remarks>
/// <para>
/// This implementation reads configuration from <see cref="ReadWriteSeparationOptions"/>
/// and delegates replica selection to the configured <see cref="IReplicaSelector"/>.
/// </para>
/// <para>
/// <b>Fallback Behavior:</b>
/// When no read replicas are configured, <see cref="GetReadConnectionString"/> returns
/// the write connection string. This ensures the application continues to work even
/// without read/write separation configured.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In service configuration
/// services.AddSingleton&lt;IReplicaSelector&gt;(sp =>
/// {
///     var options = sp.GetRequiredService&lt;IOptions&lt;ReadWriteSeparationOptions&gt;&gt;().Value;
///     return ReplicaSelectorFactory.Create(options);
/// });
/// services.AddScoped&lt;IReadWriteConnectionSelector, ReadWriteConnectionSelector&gt;();
///
/// // In a factory
/// public class MyConnectionFactory
/// {
///     private readonly IReadWriteConnectionSelector _selector;
///
///     public MyConnectionFactory(IReadWriteConnectionSelector selector)
///     {
///         _selector = selector;
///     }
///
///     public IDbConnection Create()
///     {
///         return new SqlConnection(_selector.GetConnectionString());
///     }
/// }
/// </code>
/// </example>
public sealed class ReadWriteConnectionSelector : IReadWriteConnectionSelector
{
    private readonly ReadWriteSeparationOptions _options;
    private readonly IReplicaSelector? _replicaSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteConnectionSelector"/> class.
    /// </summary>
    /// <param name="options">The read/write separation configuration options.</param>
    /// <param name="replicaSelector">
    /// The replica selector to use for selecting read replicas. Can be <see langword="null"/>
    /// if no read replicas are configured.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public ReadWriteConnectionSelector(
        IOptions<ReadWriteSeparationOptions> options,
        IReplicaSelector? replicaSelector = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _replicaSelector = replicaSelector;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteConnectionSelector"/> class
    /// using the options directly (for testing or manual construction).
    /// </summary>
    /// <param name="options">The read/write separation configuration options.</param>
    /// <param name="replicaSelector">
    /// The replica selector to use for selecting read replicas. Can be <see langword="null"/>
    /// if no read replicas are configured.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public ReadWriteConnectionSelector(
        ReadWriteSeparationOptions options,
        IReplicaSelector? replicaSelector = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _replicaSelector = replicaSelector;
    }

    /// <inheritdoc />
    public bool HasReadReplicas =>
        _options.ReadConnectionStrings is not null &&
        _options.ReadConnectionStrings.Count > 0;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ReadWriteSeparationOptions.WriteConnectionString"/> is null or empty.
    /// </exception>
    public string GetWriteConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_options.WriteConnectionString))
        {
            throw new InvalidOperationException(
                "Write connection string has not been configured. " +
                "Set ReadWriteSeparationOptions.WriteConnectionString before using read/write separation.");
        }

        return _options.WriteConnectionString;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This implementation returns:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       A replica connection string from the <see cref="IReplicaSelector"/> if replicas are configured
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       The write connection string as a fallback if no replicas are configured
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public string GetReadConnectionString()
    {
        // If we have a replica selector and replicas are configured, use it
        if (_replicaSelector is not null && HasReadReplicas)
        {
            return _replicaSelector.SelectReplica();
        }

        // Fallback to write connection when no replicas configured
        return GetWriteConnectionString();
    }

    /// <inheritdoc />
    public string GetConnectionString()
    {
        // If routing is not enabled, always use write connection
        if (!DatabaseRoutingContext.IsEnabled)
        {
            return GetWriteConnectionString();
        }

        // Check the current intent
        return DatabaseRoutingContext.IsReadIntent
            ? GetReadConnectionString()
            : GetWriteConnectionString();
    }
}
