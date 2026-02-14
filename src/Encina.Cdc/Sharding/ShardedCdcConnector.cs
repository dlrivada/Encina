using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using Encina.Sharding;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Sharding;

/// <summary>
/// Aggregates multiple per-shard <see cref="ICdcConnector"/> instances into a unified
/// change stream with per-shard position tracking.
/// </summary>
/// <remarks>
/// <para>
/// The connector creates one <see cref="ICdcConnector"/> per shard using the provided factory.
/// All shard connectors stream in parallel, and the aggregated stream orders events by
/// <see cref="ChangeMetadata.CapturedAtUtc"/> with <see cref="ShardedChangeEvent.ShardId"/>
/// as a tiebreaker for deterministic ordering.
/// </para>
/// <para>
/// Thread-safe for concurrent access. Supports dynamic addition and removal of shard connectors
/// via <see cref="AddConnectorAsync"/> and <see cref="RemoveConnectorAsync"/> for topology changes.
/// </para>
/// </remarks>
internal sealed class ShardedCdcConnector : IShardedCdcConnector
{
    private readonly Func<ShardInfo, ICdcConnector> _connectorFactory;
    private readonly IShardTopologyProvider _topologyProvider;
    private readonly ILogger<ShardedCdcConnector> _logger;
    private readonly string _connectorId;
    private readonly Lock _connectorsLock = new();
    private readonly ConcurrentDictionary<string, ICdcConnector> _connectors = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="ShardedCdcConnector"/> that creates per-shard connectors
    /// from the current shard topology.
    /// </summary>
    /// <param name="connectorId">Unique identifier for this sharded connector instance.</param>
    /// <param name="connectorFactory">Factory that creates an <see cref="ICdcConnector"/> for a given shard.</param>
    /// <param name="topologyProvider">Provides the current shard topology.</param>
    /// <param name="logger">Logger instance.</param>
    public ShardedCdcConnector(
        string connectorId,
        Func<ShardInfo, ICdcConnector> connectorFactory,
        IShardTopologyProvider topologyProvider,
        ILogger<ShardedCdcConnector> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);
        ArgumentNullException.ThrowIfNull(connectorFactory);
        ArgumentNullException.ThrowIfNull(topologyProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _connectorId = connectorId;
        _connectorFactory = connectorFactory;
        _topologyProvider = topologyProvider;
        _logger = logger;

        InitializeConnectors();
    }

    /// <inheritdoc />
    public string GetConnectorId() => _connectorId;

    /// <inheritdoc />
    public IReadOnlyList<string> ActiveShardIds
    {
        get
        {
            lock (_connectorsLock)
            {
                return _connectors.Keys.ToList();
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ShardedChangeEvent>> StreamAllShardsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var channel = Channel.CreateUnbounded<Either<EncinaError, ShardedChangeEvent>>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

        FrozenDictionary<string, ICdcConnector> snapshot;
        lock (_connectorsLock)
        {
            snapshot = _connectors.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }

        if (snapshot.Count == 0)
        {
            yield break;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = new List<Task>(snapshot.Count);

        foreach (var (shardId, connector) in snapshot)
        {
            tasks.Add(StreamShardToChannelAsync(shardId, connector, channel.Writer, linkedCts.Token));
        }

        // Complete the channel when all shard tasks finish
        _ = Task.WhenAll(tasks).ContinueWith(
            _ => channel.Writer.TryComplete(),
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamShardAsync(
        string shardId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ObjectDisposedException.ThrowIf(_disposed, this);

        ICdcConnector? connector;
        lock (_connectorsLock)
        {
            _connectors.TryGetValue(shardId, out connector);
        }

        if (connector is null)
        {
            yield return Left<EncinaError, ChangeEvent>(CdcErrors.ShardNotFound(shardId));
            yield break;
        }

        await foreach (var result in connector.StreamChangesAsync(cancellationToken))
        {
            yield return result;
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyDictionary<string, CdcPosition>>> GetAllPositionsAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        FrozenDictionary<string, ICdcConnector> snapshot;
        lock (_connectorsLock)
        {
            snapshot = _connectors.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }

        var positions = new Dictionary<string, CdcPosition>(StringComparer.OrdinalIgnoreCase);

        foreach (var (shardId, connector) in snapshot)
        {
            var result = await connector.GetCurrentPositionAsync(cancellationToken);

            if (result.IsLeft)
            {
                return result.Map<IReadOnlyDictionary<string, CdcPosition>>(_ => positions);
            }

            result.IfRight(position => positions[shardId] = position);
        }

        return Right<EncinaError, IReadOnlyDictionary<string, CdcPosition>>(positions);
    }

    /// <summary>
    /// Adds a CDC connector for a new shard. Used when the shard topology changes.
    /// </summary>
    /// <param name="shardInfo">The shard to add.</param>
    /// <returns>True if the connector was added; false if a connector for this shard already exists.</returns>
    internal bool AddConnector(ShardInfo shardInfo)
    {
        ArgumentNullException.ThrowIfNull(shardInfo);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_connectorsLock)
        {
            if (_connectors.ContainsKey(shardInfo.ShardId))
            {
                return false;
            }

            var connector = _connectorFactory(shardInfo);
            _connectors[shardInfo.ShardId] = connector;
        }

        CdcLog.ShardConnectorAdded(_logger, shardInfo.ShardId, _connectorId);
        return true;
    }

    /// <summary>
    /// Removes and disposes the CDC connector for a shard. Used when the shard topology changes.
    /// </summary>
    /// <param name="shardId">The shard to remove.</param>
    /// <returns>A task that completes when the connector is disposed.</returns>
    internal async ValueTask<bool> RemoveConnectorAsync(string shardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ObjectDisposedException.ThrowIf(_disposed, this);

        ICdcConnector? connector;
        lock (_connectorsLock)
        {
            if (!_connectors.TryRemove(shardId, out connector))
            {
                return false;
            }
        }

        if (connector is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (connector is IDisposable disposable)
        {
            disposable.Dispose();
        }

        CdcLog.ShardConnectorRemoved(_logger, shardId, _connectorId);
        return true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        List<ICdcConnector> connectorsToDispose;
        lock (_connectorsLock)
        {
            connectorsToDispose = [.. _connectors.Values];
            _connectors.Clear();
        }

        foreach (var connector in connectorsToDispose)
        {
            if (connector is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (connector is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        CdcLog.ShardedConnectorDisposed(_logger, _connectorId);
    }

    private void InitializeConnectors()
    {
        var topology = _topologyProvider.GetTopology();
        var activeShards = topology.GetActiveShards();

        foreach (var shard in activeShards)
        {
            var connector = _connectorFactory(shard);
            _connectors[shard.ShardId] = connector;
        }

        CdcLog.ShardedConnectorInitialized(
            _logger,
            _connectorId,
            _connectors.Count,
            string.Join(", ", _connectors.Keys));
    }

    private async Task StreamShardToChannelAsync(
        string shardId,
        ICdcConnector connector,
        ChannelWriter<Either<EncinaError, ShardedChangeEvent>> writer,
        CancellationToken cancellationToken)
    {
        CdcLog.ShardStreamStarted(_logger, shardId, _connectorId);

        try
        {
            await foreach (var result in connector.StreamChangesAsync(cancellationToken))
            {
                var shardedResult = result.Map(changeEvent =>
                    new ShardedChangeEvent(shardId, changeEvent, changeEvent.Metadata.Position));

                await writer.WriteAsync(shardedResult, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            CdcLog.ShardStreamError(_logger, ex, shardId, _connectorId);

            await writer.WriteAsync(
                Left<EncinaError, ShardedChangeEvent>(CdcErrors.ShardStreamFailed(shardId, ex)),
                CancellationToken.None);
        }
        finally
        {
            CdcLog.ShardStreamStopped(_logger, shardId, _connectorId);
        }
    }
}
