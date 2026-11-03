using MongoDB.Driver;

namespace Encina.MongoDB.Transactions;

/// <summary>
/// Collection operations that join the scope's client session when there is one, and run without a session
/// otherwise. The messaging stores call these so each operation chooses the driver overload in one place.
/// </summary>
/// <remarks>
/// The driver rejects a <see langword="null"/> session, so every helper picks the session overload only when
/// <c>session</c> is not null.
/// </remarks>
internal static class MongoCollectionSessionExtensions
{
    internal static Task InsertOneInSessionAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        TDocument document,
        CancellationToken cancellationToken)
        => session is null
            ? collection.InsertOneAsync(document, cancellationToken: cancellationToken)
            : collection.InsertOneAsync(session, document, cancellationToken: cancellationToken);

    internal static Task<UpdateResult> UpdateOneInSessionAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        CancellationToken cancellationToken)
        => session is null
            ? collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
            : collection.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken);

    internal static Task<UpdateResult> UpdateManyInSessionAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        CancellationToken cancellationToken)
        => session is null
            ? collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken)
            : collection.UpdateManyAsync(session, filter, update, cancellationToken: cancellationToken);

    internal static Task<DeleteResult> DeleteOneInSessionAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken)
        => session is null
            ? collection.DeleteOneAsync(filter, cancellationToken)
            : collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken);

    internal static Task<DeleteResult> DeleteManyInSessionAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken)
        => session is null
            ? collection.DeleteManyAsync(filter, cancellationToken)
            : collection.DeleteManyAsync(session, filter, cancellationToken: cancellationToken);

    internal static Task<long> CountInSessionAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken)
        => session is null
            ? collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            : collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken);

    internal static IFindFluent<TDocument, TDocument> FindInSession<TDocument>(
        this IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter)
        => session is null
            ? collection.Find(filter)
            : collection.Find(session, filter);
}
