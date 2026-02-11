using System.Runtime.CompilerServices;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.Cdc.MongoDb;

/// <summary>
/// CDC connector for MongoDB using Change Streams.
/// Streams database changes by watching a database or individual collections.
/// </summary>
/// <remarks>
/// <para>
/// MongoDB Change Streams require a replica set or sharded cluster.
/// The connector maps MongoDB operation types (insert, update, replace, delete)
/// to <see cref="ChangeOperation"/> values.
/// </para>
/// <para>
/// For before-values on updates and deletes, MongoDB 6.0+ is required with
/// <c>changeStreamPreAndPostImages</c> enabled on the collection.
/// </para>
/// </remarks>
internal sealed class MongoCdcConnector : ICdcConnector
{
    private readonly MongoCdcOptions _options;
    private readonly ICdcPositionStore _positionStore;
    private readonly ILogger<MongoCdcConnector> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoCdcConnector"/> class.
    /// </summary>
    /// <param name="options">MongoDB CDC options.</param>
    /// <param name="positionStore">Position store for tracking progress.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public MongoCdcConnector(
        MongoCdcOptions options,
        ICdcPositionStore positionStore,
        ILogger<MongoCdcConnector> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(positionStore);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _positionStore = positionStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string ConnectorId => "encina-cdc-mongodb";

    /// <inheritdoc />
    public async Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new MongoClient(_options.ConnectionString);
            var database = client.GetDatabase(_options.DatabaseName);

            // Ping the database to verify connectivity
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: cancellationToken).ConfigureAwait(false);

            // Try to get the last saved position
            var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
                .ConfigureAwait(false);

            if (positionResult.IsRight)
            {
                var optPosition = (LanguageExt.Option<CdcPosition>)positionResult;
                if (optPosition.IsSome)
                {
                    return Right<EncinaError, CdcPosition>((CdcPosition)optPosition);
                }
            }

            // Return a sentinel position indicating we start from the current time
            return Right<EncinaError, CdcPosition>(
                new MongoCdcPosition(new BsonDocument("_data", "start")));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.ConnectionFailed("Failed to connect to MongoDB", ex));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        BsonDocument? resumeToken = null;

        // Try to restore position
        var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
            .ConfigureAwait(false);

        if (positionResult.IsRight)
        {
            var optPosition = (LanguageExt.Option<CdcPosition>)positionResult;
            optPosition.IfSome(position =>
            {
                if (position is MongoCdcPosition mongoPosition)
                {
                    resumeToken = mongoPosition.ResumeToken;
                    CdcLog.PositionRestored(_logger, ConnectorId, position.ToString());
                }
            });
        }

        if (resumeToken is null)
        {
            CdcLog.NoSavedPosition(_logger, ConnectorId);
        }

        var client = new MongoClient(_options.ConnectionString);
        var database = client.GetDatabase(_options.DatabaseName);

        var pipeline = BuildPipeline();
        var options = BuildChangeStreamOptions(resumeToken);

        IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor;
        Either<EncinaError, ChangeEvent>? connectionError = null;

        try
        {
            cursor = await database.WatchAsync<ChangeStreamDocument<BsonDocument>>(
                pipeline, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            connectionError = Left(CdcErrors.ConnectionFailed("Failed to open MongoDB Change Stream", ex));
            cursor = null!;
        }

        if (connectionError is not null)
        {
            yield return connectionError.Value;
            yield break;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                bool hasMore;
                Either<EncinaError, ChangeEvent>? streamError = null;

                try
                {
                    hasMore = await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    streamError = Left(CdcErrors.StreamInterrupted(ex));
                    hasMore = false;
                }

                if (streamError is not null)
                {
                    yield return streamError.Value;
                    yield break;
                }

                if (!hasMore)
                {
                    continue;
                }

                foreach (var change in cursor.Current)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    yield return MapChangeEvent(change);
                }
            }
        }
        finally
        {
            cursor.Dispose();
        }
    }

    private PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> BuildPipeline()
    {
        if (_options.CollectionNames.Length == 0)
        {
            return new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
        }

        var filter = Builders<ChangeStreamDocument<BsonDocument>>.Filter.In(
            x => x.CollectionNamespace!.CollectionName,
            _options.CollectionNames);

        return new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match(filter);
    }

    private ChangeStreamOptions BuildChangeStreamOptions(BsonDocument? resumeToken)
    {
        var options = new ChangeStreamOptions
        {
            FullDocument = _options.FullDocument
        };

        if (resumeToken is not null &&
            resumeToken.Contains("_data") &&
            resumeToken["_data"].AsString != "start")
        {
            options.ResumeAfter = resumeToken;
        }

        return options;
    }

    private Either<EncinaError, ChangeEvent> MapChangeEvent(
        ChangeStreamDocument<BsonDocument> change)
    {
        try
        {
            var operation = MapOperationType(change.OperationType);
            var collectionName = change.CollectionNamespace?.CollectionName ?? "unknown";
            var databaseName = change.DatabaseNamespace?.DatabaseName;

            var position = new MongoCdcPosition(change.ResumeToken);
            var metadata = new ChangeMetadata(
                position,
                _timeProvider.GetUtcNow().UtcDateTime,
                TransactionId: null,
                SourceDatabase: databaseName,
                SourceSchema: null);

            object? before = change.FullDocumentBeforeChange?.ToJson();
            object? after = change.FullDocument?.ToJson();

            var changeEvent = new ChangeEvent(collectionName, operation, before, after, metadata);
            return Right<EncinaError, ChangeEvent>(changeEvent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.StreamInterrupted(ex));
        }
    }

    private static ChangeOperation MapOperationType(ChangeStreamOperationType operationType)
    {
        return operationType switch
        {
            ChangeStreamOperationType.Insert => ChangeOperation.Insert,
            ChangeStreamOperationType.Update => ChangeOperation.Update,
            ChangeStreamOperationType.Replace => ChangeOperation.Update,
            ChangeStreamOperationType.Delete => ChangeOperation.Delete,
            _ => ChangeOperation.Insert
        };
    }
}
