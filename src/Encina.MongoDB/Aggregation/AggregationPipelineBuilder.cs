using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Aggregation;

/// <summary>
/// Builds MongoDB aggregation pipelines for distributed aggregation operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This builder creates aggregation pipelines using the MongoDB C# driver's fluent API.
/// The pipelines combine <c>$match</c> stages (for filtering) with <c>$group</c> stages
/// (for computing aggregates). They are designed for two-phase distributed aggregation
/// where each shard computes local aggregates that are later combined using
/// <c>AggregationCombiner</c>.
/// </para>
/// <para>
/// For predicate-to-filter translation, this builder uses
/// <see cref="Builders{TDocument}.Filter"/> which natively supports LINQ expressions.
/// </para>
/// <para>
/// For field name extraction from selector expressions, the builder walks the
/// <see cref="MemberExpression"/> tree to produce dot-notation field names
/// compatible with MongoDB's document model.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var builder = new AggregationPipelineBuilder&lt;Order&gt;();
///
/// // Build a count pipeline with filtering
/// var pipeline = builder.BuildCountPipeline(collection, o =&gt; o.Status == OrderStatus.Active);
/// var result = await pipeline.SingleOrDefaultAsync();
/// var count = result?["count"].AsInt64 ?? 0;
/// </code>
/// </example>
public sealed class AggregationPipelineBuilder<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Builds an aggregation pipeline that counts entities matching the predicate.
    /// </summary>
    /// <param name="collection">The MongoDB collection to aggregate against.</param>
    /// <param name="predicate">A filter expression to apply before counting.</param>
    /// <returns>
    /// An <see cref="IAggregateFluent{BsonDocument}"/> pipeline with <c>$match</c> and
    /// <c>$group</c> stages that produces a <c>count</c> field.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The pipeline produces a single document with the structure:
    /// <c>{ "_id": null, "count": &lt;number&gt; }</c>
    /// </para>
    /// <para>
    /// If no documents match the predicate, the pipeline returns an empty cursor.
    /// Callers should treat an empty result as a count of zero.
    /// </para>
    /// </remarks>
    public IAggregateFluent<BsonDocument> BuildCountPipeline(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);

        var groupStage = new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "count", new BsonDocument("$sum", 1) },
        });

        return collection.Aggregate()
            .Match(predicate)
            .AppendStage<BsonDocument>(groupStage);
    }

    /// <summary>
    /// Builds an aggregation pipeline that sums a numeric field for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TValue">The numeric type of the field to sum.</typeparam>
    /// <param name="collection">The MongoDB collection to aggregate against.</param>
    /// <param name="selector">An expression selecting the numeric field to sum.</param>
    /// <param name="predicate">An optional filter expression to apply before summing.</param>
    /// <returns>
    /// An <see cref="IAggregateFluent{BsonDocument}"/> pipeline with an optional <c>$match</c>
    /// and a <c>$group</c> stage with a <c>$sum</c> accumulator.
    /// </returns>
    /// <remarks>
    /// The pipeline produces a single document with the structure:
    /// <c>{ "_id": null, "sum": &lt;number&gt; }</c>
    /// </remarks>
    public IAggregateFluent<BsonDocument> BuildSumPipeline<TValue>(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(selector);

        var fieldName = GetFieldNameFromSelector(selector);

        var groupStage = new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "sum", new BsonDocument("$sum", $"${fieldName}") },
        });

        var pipeline = collection.Aggregate();

        if (predicate is not null)
        {
            pipeline = pipeline.Match(predicate);
        }

        return pipeline.AppendStage<BsonDocument>(groupStage);
    }

    /// <summary>
    /// Builds an aggregation pipeline that returns both sum and count for two-phase
    /// average computation across shards.
    /// </summary>
    /// <typeparam name="TValue">The numeric type of the field to average.</typeparam>
    /// <param name="collection">The MongoDB collection to aggregate against.</param>
    /// <param name="selector">An expression selecting the numeric field to average.</param>
    /// <param name="predicate">An optional filter expression to apply before aggregating.</param>
    /// <returns>
    /// An <see cref="IAggregateFluent{BsonDocument}"/> pipeline with an optional <c>$match</c>
    /// and a <c>$group</c> stage with both <c>$sum</c> and count accumulators.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The pipeline produces a single document with the structure:
    /// <c>{ "_id": null, "sum": &lt;number&gt;, "count": &lt;number&gt; }</c>
    /// </para>
    /// <para>
    /// This pipeline intentionally does NOT compute the average directly. Instead, it returns
    /// the raw sum and count so that the caller can combine partials from multiple shards
    /// using <c>totalSum / totalCount</c> to compute a correct global average. This avoids
    /// the "average of averages" error.
    /// </para>
    /// </remarks>
    public IAggregateFluent<BsonDocument> BuildAvgPartialPipeline<TValue>(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(selector);

        var fieldName = GetFieldNameFromSelector(selector);

        var groupStage = new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "sum", new BsonDocument("$sum", $"${fieldName}") },
            { "count", new BsonDocument("$sum", 1) },
        });

        var pipeline = collection.Aggregate();

        if (predicate is not null)
        {
            pipeline = pipeline.Match(predicate);
        }

        return pipeline.AppendStage<BsonDocument>(groupStage);
    }

    /// <summary>
    /// Builds an aggregation pipeline that finds the minimum value of a field
    /// for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TValue">The type of the field to find the minimum of.</typeparam>
    /// <param name="collection">The MongoDB collection to aggregate against.</param>
    /// <param name="selector">An expression selecting the field to find the minimum of.</param>
    /// <param name="predicate">An optional filter expression to apply before computing the minimum.</param>
    /// <returns>
    /// An <see cref="IAggregateFluent{BsonDocument}"/> pipeline with an optional <c>$match</c>
    /// and a <c>$group</c> stage with a <c>$min</c> accumulator.
    /// </returns>
    /// <remarks>
    /// The pipeline produces a single document with the structure:
    /// <c>{ "_id": null, "result": &lt;value&gt; }</c>
    /// If no documents match, the pipeline returns an empty cursor.
    /// </remarks>
    public IAggregateFluent<BsonDocument> BuildMinPipeline<TValue>(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(selector);

        return BuildMinMaxPipeline(collection, selector, "$min", predicate);
    }

    /// <summary>
    /// Builds an aggregation pipeline that finds the maximum value of a field
    /// for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TValue">The type of the field to find the maximum of.</typeparam>
    /// <param name="collection">The MongoDB collection to aggregate against.</param>
    /// <param name="selector">An expression selecting the field to find the maximum of.</param>
    /// <param name="predicate">An optional filter expression to apply before computing the maximum.</param>
    /// <returns>
    /// An <see cref="IAggregateFluent{BsonDocument}"/> pipeline with an optional <c>$match</c>
    /// and a <c>$group</c> stage with a <c>$max</c> accumulator.
    /// </returns>
    /// <remarks>
    /// The pipeline produces a single document with the structure:
    /// <c>{ "_id": null, "result": &lt;value&gt; }</c>
    /// If no documents match, the pipeline returns an empty cursor.
    /// </remarks>
    public IAggregateFluent<BsonDocument> BuildMaxPipeline<TValue>(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(selector);

        return BuildMinMaxPipeline(collection, selector, "$max", predicate);
    }

    /// <summary>
    /// Extracts the MongoDB field name from a property selector expression.
    /// </summary>
    /// <typeparam name="TValue">The type of the selected property.</typeparam>
    /// <param name="selector">An expression selecting a property from the entity.</param>
    /// <returns>
    /// The dot-notation field name for the selected property (e.g., <c>"Address.City"</c>
    /// for a nested property <c>x => x.Address.City</c>).
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the expression is not a simple member access expression.
    /// </exception>
    /// <remarks>
    /// This method supports:
    /// <list type="bullet">
    ///   <item><description>Simple properties: <c>x => x.Amount</c> produces <c>"Amount"</c></description></item>
    ///   <item><description>Nested properties: <c>x => x.Address.City</c> produces <c>"Address.City"</c></description></item>
    ///   <item><description>Expressions with Convert wrappers (boxing/unboxing)</description></item>
    /// </list>
    /// </remarks>
    internal static string GetFieldNameFromSelector<TValue>(Expression<Func<TEntity, TValue>> selector)
    {
        var body = selector.Body;

        // Unwrap Convert expression if present (boxing/unboxing or type conversions)
        if (body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var operand })
        {
            body = operand;
        }

        if (body is MemberExpression member)
        {
            return GetFieldName(member);
        }

        throw new NotSupportedException(
            $"Expression type '{body.NodeType}' is not supported for MongoDB field name extraction. " +
            $"Only simple property access expressions are supported (e.g., x => x.PropertyName).");
    }

    private static string GetFieldName(MemberExpression member)
    {
        var parts = new List<string>();
        Expression? current = member;

        while (current is MemberExpression memberExpr)
        {
            parts.Insert(0, memberExpr.Member.Name);
            current = memberExpr.Expression;
        }

        return string.Join(".", parts);
    }

    private static IAggregateFluent<BsonDocument> BuildMinMaxPipeline<TValue>(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, TValue>> selector,
        string accumulator,
        Expression<Func<TEntity, bool>>? predicate)
    {
        var fieldName = GetFieldNameFromSelector(selector);

        var groupStage = new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "result", new BsonDocument(accumulator, $"${fieldName}") },
        });

        var pipeline = collection.Aggregate();

        if (predicate is not null)
        {
            pipeline = pipeline.Match(predicate);
        }

        return pipeline.AppendStage<BsonDocument>(groupStage);
    }
}
