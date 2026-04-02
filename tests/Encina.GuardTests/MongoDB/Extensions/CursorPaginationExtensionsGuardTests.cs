using Encina.DomainModeling.Pagination;
using Encina.MongoDB.Extensions;
using Encina.MongoDB.Repository;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Extensions;

public class CursorPaginationExtensionsGuardTests
{
    private static readonly IMongoCollection<TestEntity> Collection = Substitute.For<IMongoCollection<TestEntity>>();
    private static readonly FilterDefinition<TestEntity> Filter = Builders<TestEntity>.Filter.Empty;
    private static readonly ICursorEncoder Encoder = Substitute.For<ICursorEncoder>();

    #region ToCursorPaginatedAsync (simple key)

    [Fact]
    public async Task ToCursorPaginatedAsync_NullCollection_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await CursorPaginationExtensions.ToCursorPaginatedAsync<TestEntity, Guid>(
                null!, Filter, null, 10, e => e.Id, Encoder));
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_NullFilter_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await Collection.ToCursorPaginatedAsync<TestEntity, Guid>(
                null!, null, 10, e => e.Id, Encoder));
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_NullKeySelector_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await Collection.ToCursorPaginatedAsync<TestEntity, Guid>(
                Filter, null, 10, null!, Encoder));
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_NullEncoder_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await Collection.ToCursorPaginatedAsync<TestEntity, Guid>(
                Filter, null, 10, e => e.Id, null!));
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_PageSizeZero_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await Collection.ToCursorPaginatedAsync<TestEntity, Guid>(
                Filter, null, 0, e => e.Id, Encoder));
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_PageSizeExceedsMax_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await Collection.ToCursorPaginatedAsync<TestEntity, Guid>(
                Filter, null, CursorPaginationOptions.MaxPageSize + 1, e => e.Id, Encoder));
    }

    #endregion

    #region ToCursorPaginatedAsync (with options)

    [Fact]
    public async Task ToCursorPaginatedAsync_Options_NullCollection_Throws()
    {
        var opts = new CursorPaginationOptions(null, 10);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await CursorPaginationExtensions.ToCursorPaginatedAsync<TestEntity, Guid>(
                null!, Filter, opts, e => e.Id, Encoder));
    }

    [Fact]
    public async Task ToCursorPaginatedAsync_Options_NullOptions_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await Collection.ToCursorPaginatedAsync<TestEntity, Guid>(
                Filter, (CursorPaginationOptions)null!, e => e.Id, Encoder));
    }

    #endregion

    #region ToCursorPaginatedCompositeAsync

    [Fact]
    public async Task ToCursorPaginatedCompositeAsync_NullCollection_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await CursorPaginationExtensions.ToCursorPaginatedCompositeAsync<TestEntity, Guid>(
                null!, Filter, null, 10, e => e.Id, Encoder, [false]));
    }

    [Fact]
    public async Task ToCursorPaginatedCompositeAsync_NullFilter_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await Collection.ToCursorPaginatedCompositeAsync<TestEntity, Guid>(
                null!, null, 10, e => e.Id, Encoder, [false]));
    }

    [Fact]
    public async Task ToCursorPaginatedCompositeAsync_NullKeySelector_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await Collection.ToCursorPaginatedCompositeAsync<TestEntity, Guid>(
                Filter, null, 10, null!, Encoder, [false]));
    }

    [Fact]
    public async Task ToCursorPaginatedCompositeAsync_NullEncoder_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await Collection.ToCursorPaginatedCompositeAsync<TestEntity, Guid>(
                Filter, null, 10, e => e.Id, null!, [false]));
    }

    [Fact]
    public async Task ToCursorPaginatedCompositeAsync_NullKeyDescending_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await Collection.ToCursorPaginatedCompositeAsync<TestEntity, Guid>(
                Filter, null, 10, e => e.Id, Encoder, null!));
    }

    [Fact]
    public async Task ToCursorPaginatedCompositeAsync_PageSizeZero_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await Collection.ToCursorPaginatedCompositeAsync<TestEntity, Guid>(
                Filter, null, 0, e => e.Id, Encoder, [false]));
    }

    #endregion

    public class TestEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
