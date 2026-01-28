namespace Encina.Benchmarks.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Factory methods for generating test entities at various batch sizes.
/// </summary>
/// <remarks>
/// <para>
/// Provides deterministic test data generation for benchmark reproducibility.
/// All methods generate entities with realistic property values to simulate
/// production workloads.
/// </para>
/// <para>
/// Standard batch sizes (1, 10, 100, 1000) align with common BenchmarkDotNet
/// [Params] configurations for scalability testing.
/// </para>
/// </remarks>
public static class TestData
{
    /// <summary>
    /// Standard batch sizes for benchmark parameterization.
    /// </summary>
    public static readonly int[] StandardBatchSizes = [1, 10, 100, 1000];

    /// <summary>
    /// Sample categories for realistic filtering benchmarks.
    /// </summary>
    private static readonly string[] s_categories =
    [
        "Electronics",
        "Books",
        "Clothing",
        "Home",
        "Sports",
        "Toys",
        "Automotive",
        "Garden",
        "Health",
        "Food"
    ];

    /// <summary>
    /// Sample name prefixes for realistic entity names.
    /// </summary>
    private static readonly string[] s_namePrefixes =
    [
        "Product",
        "Item",
        "Article",
        "Entry",
        "Record"
    ];

    /// <summary>
    /// Creates a single benchmark entity with default values.
    /// </summary>
    /// <param name="index">Optional index for deterministic naming.</param>
    /// <returns>A new benchmark entity.</returns>
    public static BenchmarkEntity CreateEntity(int index = 0)
    {
        return new BenchmarkEntity
        {
            Id = Guid.NewGuid(),
            Name = $"{s_namePrefixes[index % s_namePrefixes.Length]}-{index:D6}",
            Value = CalculateValue(index),
            CreatedAtUtc = CalculateDate(index),
            Category = s_categories[index % s_categories.Length],
            IsActive = index % 10 != 0 // 90% active
        };
    }

    /// <summary>
    /// Creates a single benchmark entity with a specific ID.
    /// </summary>
    /// <param name="id">The entity ID to use.</param>
    /// <param name="index">Optional index for deterministic naming.</param>
    /// <returns>A new benchmark entity with the specified ID.</returns>
    public static BenchmarkEntity CreateEntityWithId(Guid id, int index = 0)
    {
        var entity = CreateEntity(index);
        entity.Id = id;
        return entity;
    }

    /// <summary>
    /// Creates a batch of benchmark entities.
    /// </summary>
    /// <param name="count">The number of entities to create.</param>
    /// <returns>A list of benchmark entities.</returns>
    public static List<BenchmarkEntity> CreateEntities(int count)
    {
        var entities = new List<BenchmarkEntity>(count);
        for (var i = 0; i < count; i++)
        {
            entities.Add(CreateEntity(i));
        }
        return entities;
    }

    /// <summary>
    /// Creates a batch of benchmark entities with pre-allocated IDs.
    /// </summary>
    /// <param name="ids">The IDs to use for the entities.</param>
    /// <returns>A list of benchmark entities with the specified IDs.</returns>
    public static List<BenchmarkEntity> CreateEntitiesWithIds(IEnumerable<Guid> ids)
    {
        return ids.Select((id, index) => CreateEntityWithId(id, index)).ToList();
    }

    /// <summary>
    /// Creates entities for a specific category.
    /// Useful for filter benchmark scenarios.
    /// </summary>
    /// <param name="category">The category to assign.</param>
    /// <param name="count">The number of entities to create.</param>
    /// <returns>A list of entities in the specified category.</returns>
    public static List<BenchmarkEntity> CreateEntitiesForCategory(string category, int count)
    {
        var entities = new List<BenchmarkEntity>(count);
        for (var i = 0; i < count; i++)
        {
            var entity = CreateEntity(i);
            entity.Category = category;
            entities.Add(entity);
        }
        return entities;
    }

    /// <summary>
    /// Creates entities distributed evenly across all categories.
    /// </summary>
    /// <param name="totalCount">The total number of entities to create.</param>
    /// <returns>A list of entities distributed across categories.</returns>
    public static List<BenchmarkEntity> CreateEntitiesAcrossCategories(int totalCount)
    {
        return CreateEntities(totalCount);
    }

    /// <summary>
    /// Creates inactive entities for testing IsActive filters.
    /// </summary>
    /// <param name="count">The number of inactive entities to create.</param>
    /// <returns>A list of inactive entities.</returns>
    public static List<BenchmarkEntity> CreateInactiveEntities(int count)
    {
        var entities = new List<BenchmarkEntity>(count);
        for (var i = 0; i < count; i++)
        {
            var entity = CreateEntity(i);
            entity.IsActive = false;
            entities.Add(entity);
        }
        return entities;
    }

    /// <summary>
    /// Creates entities within a specific value range.
    /// Useful for range query benchmarks.
    /// </summary>
    /// <param name="minValue">Minimum value (inclusive).</param>
    /// <param name="maxValue">Maximum value (inclusive).</param>
    /// <param name="count">The number of entities to create.</param>
    /// <returns>A list of entities with values in the specified range.</returns>
    public static List<BenchmarkEntity> CreateEntitiesInValueRange(decimal minValue, decimal maxValue, int count)
    {
        var entities = new List<BenchmarkEntity>(count);
        var step = (maxValue - minValue) / count;

        for (var i = 0; i < count; i++)
        {
            var entity = CreateEntity(i);
            entity.Value = minValue + (step * i);
            entities.Add(entity);
        }
        return entities;
    }

    /// <summary>
    /// Creates entities within a specific date range.
    /// Useful for date-based query benchmarks.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="count">The number of entities to create.</param>
    /// <returns>A list of entities with dates in the specified range.</returns>
    public static List<BenchmarkEntity> CreateEntitiesInDateRange(DateTime startDate, DateTime endDate, int count)
    {
        var entities = new List<BenchmarkEntity>(count);
        var totalTicks = endDate.Ticks - startDate.Ticks;
        var step = totalTicks / count;

        for (var i = 0; i < count; i++)
        {
            var entity = CreateEntity(i);
            entity.CreatedAtUtc = new DateTime(startDate.Ticks + (step * i), DateTimeKind.Utc);
            entities.Add(entity);
        }
        return entities;
    }

    /// <summary>
    /// Pre-generates IDs for batch operations.
    /// Useful when IDs need to be known before entity creation.
    /// </summary>
    /// <param name="count">The number of IDs to generate.</param>
    /// <returns>An array of GUIDs.</returns>
    public static Guid[] GenerateIds(int count)
    {
        var ids = new Guid[count];
        for (var i = 0; i < count; i++)
        {
            ids[i] = Guid.NewGuid();
        }
        return ids;
    }

    /// <summary>
    /// Gets all available category values.
    /// </summary>
    /// <returns>Array of category strings.</returns>
    public static string[] GetCategories() => s_categories;

    /// <summary>
    /// Calculates a deterministic value based on index.
    /// Provides realistic decimal values for benchmarking.
    /// </summary>
    private static decimal CalculateValue(int index)
    {
        // Generate values between 1.00 and 10000.00
        return Math.Round((decimal)((index * 7.31m % 9999) + 1), 2);
    }

    /// <summary>
    /// Calculates a deterministic date based on index.
    /// Spreads dates over the last year for realistic benchmarks.
    /// </summary>
    private static DateTime CalculateDate(int index)
    {
        // Base date: January 1, 2026 (for deterministic benchmarks)
        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // Spread over 365 days
        var daysOffset = index % 365;
        var hoursOffset = (index * 3) % 24;
        return baseDate.AddDays(-daysOffset).AddHours(hoursOffset);
    }
}
