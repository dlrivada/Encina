# v0.12.0 - Database & Repository

> **Release Date**: In Progress
> **Milestone**: [v0.12.0 - Database & Repository](https://github.com/dlrivada/Encina/milestone/9)
> **Status**: In Progress (22 issues)

This document captures the detailed implementation history for v0.12.0 (February 2026).

## Milestone Overview

v0.12.0 focuses on database and repository patterns, completing the data access layer for production-ready applications.

### Issues in Milestone

| Issue | Feature | Status |
|-------|---------|--------|
| #279 | Generic Repository | Completado |
| #280 | Specification Pattern | Completado |
| #281 | Unit of Work | Completado |
| #282 | Multi-Tenancy | Completado |
| #283 | Read/Write Separation | Pendiente |
| #284 | Bulk Operations | Completado (v0.11.0) |
| #285 | Soft Delete | Pendiente |
| #286 | Audit Trail | Pendiente |
| #287 | Optimistic Concurrency | Completado |
| #288 | CDC Integration | Pendiente |
| #289 | Sharding | Pendiente |
| #290 | Connection Pool | Pendiente |
| #291 | Query Cache | Pendiente |
| #292 | Domain Entity Base | Completado |
| #293 | Pagination Abstractions | **Completado** |
| #294 | Cursor Pagination | Pendiente |
| #534 | Module Isolation | Completado |

---

## Week of February 5, 2026

### February 5 - Pagination Abstractions (#293)

**Issue**: [#293 - Pagination Abstractions](https://github.com/dlrivada/Encina/issues/293)

Implemented comprehensive pagination abstractions for data access, including pagination options, paged results, and specification-based pagination.

#### Phase 1-3: Core Implementation

**Files Created/Modified**:

**Encina.DomainModeling**:

- `src/Encina.DomainModeling/PaginationOptions.cs` - Core pagination records
  - `PaginationOptions(PageNumber, PageSize)` with computed `Skip` property
  - `SortedPaginationOptions` extending with `SortBy` and `SortDescending`
  - Fluent builder methods: `WithPage()`, `WithSize()`, `WithSort()`
  - Validation: page/size must be >= 1, sortBy cannot be null/whitespace

- `src/Encina.DomainModeling/PagedResult.cs` - Paged result record
  - `PagedResult<T>` with `Items`, `PageNumber`, `PageSize`, `TotalCount`
  - Computed properties: `TotalPages`, `HasPreviousPage`, `HasNextPage`
  - Navigation helpers: `FirstItemIndex`, `LastItemIndex`, `IsFirstPage`, `IsLastPage`
  - `Map<TDestination>()` for functional projections
  - `Empty()` factory for empty results

- `src/Encina.DomainModeling/IPagedSpecification.cs` - Specification interface
  - `IPagedSpecification<T>` with `Pagination` property
  - `IPagedSpecification<T, TResult>` with `Selector` for projections

- `src/Encina.DomainModeling/PagedQuerySpecification.cs` - Base class
  - `PagedQuerySpecification<T>` implementing both interfaces
  - Constructor with `PaginationOptions` (null-check validation)
  - Access to all `QuerySpecification<T>` methods

**Encina.EntityFrameworkCore**:

- `src/Encina.EntityFrameworkCore/Extensions/QueryablePagedExtensions.cs`
  - `ToPagedResultAsync<T>()` - Basic pagination
  - `ToPagedResultAsync<T, TResult>()` - With projection expression
  - Efficient: single count query + paginated data query

- `src/Encina.EntityFrameworkCore/Repository/FunctionalRepositoryEF.cs` (Modified)
  - `GetPagedAsync(PaginationOptions)` - Basic pagination
  - `GetPagedAsync(Specification, PaginationOptions)` - With filter
  - `GetPagedAsync(IPagedSpecification)` - Full specification-based
  - All return `Either<EncinaError, PagedResult<T>>`
  - Fixed double-pagination bug when using IPagedSpecification

#### Phase 4: Comprehensive Testing

**Unit Tests** (221 tests):

- `tests/Encina.UnitTests/DomainModeling/Pagination/PaginationOptionsTests.cs` (43 tests)
  - Constructor and record behavior
  - Skip calculation
  - WithPage/WithSize builder methods
  - Default singleton

- `tests/Encina.UnitTests/DomainModeling/Pagination/SortedPaginationOptionsTests.cs` (38 tests)
  - Inheritance from PaginationOptions
  - Sorting properties
  - WithSort builder method
  - Type preservation in builders

- `tests/Encina.UnitTests/DomainModeling/Pagination/PagedResultTests.cs` (59 tests)
  - Computed properties (TotalPages, HasPrevious/NextPage)
  - Navigation indices (FirstItemIndex, LastItemIndex)
  - Edge cases (empty results, single page, last page)
  - Map() projection functionality
  - Empty() factory method

- `tests/Encina.UnitTests/DomainModeling/Pagination/PagedQuerySpecificationTests.cs` (16 tests)
  - Constructor validation
  - Pagination property access
  - Integration with QuerySpecification base class

- `tests/Encina.UnitTests/EntityFrameworkCore/Extensions/QueryablePagedExtensionsTests.cs` (39 tests)
  - ToPagedResultAsync without projection
  - ToPagedResultAsync with projection
  - Page navigation
  - Empty dataset handling
  - Large dataset pagination

- `tests/Encina.UnitTests/EntityFrameworkCore/Repository/FunctionalRepositoryEFPaginationTests.cs` (20 tests)
  - GetPagedAsync(PaginationOptions) overload
  - GetPagedAsync with predicate overload
  - GetPagedAsync with IPagedSpecification overload
  - ROP integration (Either return types)

**Guard Tests** (25 tests):

- `tests/Encina.GuardTests/DomainModeling/PaginationGuardTests.cs` (14 tests)
  - PaginationOptions.WithPage() throws for values < 1
  - PaginationOptions.WithSize() throws for values < 1
  - SortedPaginationOptions.WithSort() throws for null/whitespace
  - PagedResult.Map() throws for null selector
  - PagedQuerySpecification constructor throws for null pagination

- `tests/Encina.GuardTests/Infrastructure/EntityFrameworkCore/QueryablePagedExtensionsGuardTests.cs` (5 tests)
  - ToPagedResultAsync() null query validation
  - ToPagedResultAsync() null pagination validation
  - ToPagedResultAsync() with projection null validations

**Bug Fix**: Fixed double-pagination bug in `FunctionalRepositoryEF.GetPagedAsync(IPagedSpecification)`:
- SpecificationEvaluator was applying Skip/Take, then ToPagedResultAsync applied them again
- Solution: Build base query with filtering only, let ToPagedResultAsync handle pagination
- Added `ApplyOrderingForPaging()` helper method for correct ordering

#### Code Examples

**Basic Pagination**:

```csharp
// Using PaginationOptions
var options = PaginationOptions.Default
    .WithPage(2)
    .WithSize(25);

var result = await repository.GetPagedAsync(options);
// result: Either<EncinaError, PagedResult<Entity>>

result.Match(
    Right: pagedResult =>
    {
        Console.WriteLine($"Page {pagedResult.PageNumber} of {pagedResult.TotalPages}");
        Console.WriteLine($"Showing items {pagedResult.FirstItemIndex}-{pagedResult.LastItemIndex}");
        foreach (var item in pagedResult.Items) { /* ... */ }
    },
    Left: error => Console.WriteLine(error.Message)
);
```

**With Sorting**:

```csharp
var options = SortedPaginationOptions.Default
    .WithSort("CreatedAtUtc", descending: true)
    .WithPage(1)
    .WithSize(50);
```

**Using Specifications**:

```csharp
public class ActiveOrdersSpec : PagedQuerySpecification<Order>
{
    public ActiveOrdersSpec(PaginationOptions pagination) : base(pagination)
    {
        AddCriteria(o => o.Status == OrderStatus.Active);
        ApplyOrderByDescending(o => o.CreatedAtUtc);
    }
}

var spec = new ActiveOrdersSpec(PaginationOptions.Default.WithSize(20));
var result = await repository.GetPagedAsync(spec);
```

**EF Core Extensions**:

```csharp
// Direct IQueryable usage
var pagedResult = await dbContext.Orders
    .Where(o => o.IsActive)
    .OrderByDescending(o => o.CreatedAtUtc)
    .ToPagedResultAsync(new PaginationOptions(1, 25));

// With projection
var pagedDtos = await dbContext.Orders
    .Where(o => o.IsActive)
    .ToPagedResultAsync(
        o => new OrderDto(o.Id, o.Total),
        new PaginationOptions(1, 25));
```

---

## Quality Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Pagination Unit Tests | 221 | - |
| Pagination Guard Tests | 25 | - |
| Build Warnings | 0 | 0 ✅ |
| Code Coverage | TBD | ≥85% |

---

## Next Steps

1. **Integration Tests**: Add real database tests for pagination (Docker/Testcontainers)
2. **Cursor Pagination** (#294): Research and implement keyset pagination
3. **API Documentation**: Add pagination examples to API docs
4. **ASP.NET Core Integration**: Add controller helpers for pagination
