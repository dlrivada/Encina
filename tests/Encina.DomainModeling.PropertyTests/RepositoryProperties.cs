using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for repository patterns.
/// </summary>
public class RepositoryProperties
{
    // === PagedResult Properties ===

    [Property(MaxTest = 100)]
    public bool PagedResult_TotalPages_CalculatedCorrectly(
        PositiveInt totalCount,
        PositiveInt pageSize)
    {
        var items = new List<string>();
        var result = new PagedResult<string>(items, 1, pageSize.Get, totalCount.Get);

        var expectedPages = (int)Math.Ceiling(totalCount.Get / (double)pageSize.Get);

        return result.TotalPages == expectedPages;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_HasPreviousPage_FalseOnFirstPage(
        PositiveInt totalCount,
        PositiveInt pageSize)
    {
        var items = new List<string>();
        var result = new PagedResult<string>(items, 1, pageSize.Get, totalCount.Get);

        return !result.HasPreviousPage;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_HasPreviousPage_TrueOnLaterPages(
        PositiveInt totalCount,
        PositiveInt pageSize)
    {
        if (totalCount.Get <= pageSize.Get) return true; // Skip if only one page

        var items = new List<string>();
        var result = new PagedResult<string>(items, 2, pageSize.Get, totalCount.Get);

        return result.HasPreviousPage;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_HasNextPage_TrueWhenNotOnLastPage(
        PositiveInt pageSize)
    {
        var items = new List<string>();
        var totalCount = pageSize.Get * 3; // 3 pages
        var result = new PagedResult<string>(items, 1, pageSize.Get, totalCount);

        return result.HasNextPage;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_HasNextPage_FalseOnLastPage(
        PositiveInt pageSize)
    {
        var items = new List<string>();
        var totalCount = pageSize.Get * 3; // 3 pages
        var result = new PagedResult<string>(items, 3, pageSize.Get, totalCount);

        return !result.HasNextPage;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_Empty_HasCorrectDefaults()
    {
        var result = PagedResult<string>.Empty();

        return result.Items.Count == 0
            && result.PageNumber == 1
            && result.PageSize == 10
            && result.TotalCount == 0
            && result.TotalPages == 0
            && !result.HasPreviousPage
            && !result.HasNextPage
            && result.IsEmpty;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_Empty_WithCustomParams(
        PositiveInt pageNumber,
        PositiveInt pageSize)
    {
        var result = PagedResult<string>.Empty(pageNumber.Get, pageSize.Get);

        return result.Items.Count == 0
            && result.PageNumber == pageNumber.Get
            && result.PageSize == pageSize.Get
            && result.TotalCount == 0;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_Map_TransformsItems(NonEmptyString item)
    {
        var items = new List<string> { item.Get };
        var result = new PagedResult<string>(items, 1, 10, 1);

        var mapped = result.Map(s => s.Length);

        return mapped.Items.Count == 1
            && mapped.Items[0] == item.Get.Length
            && mapped.PageNumber == result.PageNumber
            && mapped.PageSize == result.PageSize
            && mapped.TotalCount == result.TotalCount;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_IsEmpty_CorrectForEmptyItems()
    {
        var result = new PagedResult<string>([], 1, 10, 0);

        return result.IsEmpty;
    }

    [Property(MaxTest = 100)]
    public bool PagedResult_IsEmpty_FalseForNonEmptyItems(NonEmptyString item)
    {
        var result = new PagedResult<string>([item.Get], 1, 10, 1);

        return !result.IsEmpty;
    }

    // === RepositoryError Factory Methods ===

    [Property(MaxTest = 100)]
    public bool RepositoryError_NotFound_HasCorrectErrorCode(Guid entityId)
    {
        var error = RepositoryError.NotFound<TestEntity, Guid>(entityId);

        return error.ErrorCode == "REPOSITORY_NOT_FOUND"
            && error.EntityType == typeof(TestEntity)
            && error.EntityId!.Equals(entityId);
    }

    [Property(MaxTest = 100)]
    public bool RepositoryError_AlreadyExists_HasCorrectErrorCode(Guid entityId)
    {
        var error = RepositoryError.AlreadyExists<TestEntity, Guid>(entityId);

        return error.ErrorCode == "REPOSITORY_ALREADY_EXISTS"
            && error.EntityType == typeof(TestEntity)
            && error.EntityId!.Equals(entityId);
    }

    [Property(MaxTest = 100)]
    public bool RepositoryError_ConcurrencyConflict_HasCorrectErrorCode(Guid entityId)
    {
        var error = RepositoryError.ConcurrencyConflict<TestEntity, Guid>(entityId);

        return error.ErrorCode == "REPOSITORY_CONCURRENCY_CONFLICT"
            && error.EntityType == typeof(TestEntity);
    }

    [Property(MaxTest = 100)]
    public bool RepositoryError_InvalidPagination_HasCorrectErrorCode(int pageNumber, int pageSize)
    {
        var error = RepositoryError.InvalidPagination<TestEntity>(pageNumber, pageSize);

        return error.ErrorCode == "REPOSITORY_INVALID_PAGINATION"
            && error.EntityType == typeof(TestEntity);
    }

    [Property(MaxTest = 100)]
    public bool RepositoryError_OperationFailed_HasCorrectErrorCode(NonEmptyString operation)
    {
        var exception = new InvalidOperationException("Test error");
        var error = RepositoryError.OperationFailed<TestEntity>(operation.Get, exception);

        return error.ErrorCode == "REPOSITORY_OPERATION_FAILED"
            && error.EntityType == typeof(TestEntity)
            && error.InnerException == exception;
    }

    // === EntityNotFoundException ===

    [Property(MaxTest = 100)]
    public bool EntityNotFoundException_HasCorrectProperties(NonEmptyString entityId)
    {
        var exception = new EntityNotFoundException(typeof(TestEntity), entityId.Get);

        return exception.EntityType == typeof(TestEntity)
            && exception.EntityId == entityId.Get
            && exception.Message.Contains(typeof(TestEntity).Name)
            && exception.Message.Contains(entityId.Get);
    }

    [Property(MaxTest = 100)]
    public bool EntityNotFoundException_WithInnerException_PreservesIt(NonEmptyString entityId)
    {
        var inner = new InvalidOperationException("Inner");
        var exception = new EntityNotFoundException(typeof(TestEntity), entityId.Get, inner);

        return exception.InnerException == inner;
    }

    // Test entity for property tests
    private sealed class TestEntity(Guid id) : Entity<Guid>(id);
}
