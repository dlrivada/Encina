using Encina.DomainModeling;
using Encina.OpenTelemetry.Repository;
using Encina.Testing;
using LanguageExt;
using Shouldly;

namespace Encina.ContractTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Contract tests for <see cref="InstrumentedFunctionalRepository{TEntity, TId}"/>
/// verifying it correctly implements <see cref="IFunctionalRepository{TEntity, TId}"/>
/// by delegating all calls to the inner repository.
/// </summary>
public sealed class InstrumentedFunctionalRepositoryContractTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private sealed class StubRepository : IFunctionalRepository<TestEntity, Guid>
    {
        public Either<EncinaError, TestEntity> GetByIdResult { get; set; }
        public Either<EncinaError, TestEntity> AddResult { get; set; }
        public int GetByIdCalls { get; private set; }
        public int AddCalls { get; private set; }

        public Task<Either<EncinaError, TestEntity>> GetByIdAsync(Guid id, CancellationToken ct = default) { GetByIdCalls++; return Task.FromResult(GetByIdResult); }
        public Task<Either<EncinaError, TestEntity>> AddAsync(TestEntity entity, CancellationToken ct = default) { AddCalls++; return Task.FromResult(AddResult); }
        public Task<Either<EncinaError, IReadOnlyList<TestEntity>>> ListAsync(CancellationToken ct = default) => Task.FromResult(Either<EncinaError, IReadOnlyList<TestEntity>>.Right(Array.Empty<TestEntity>()));
        public Task<Either<EncinaError, IReadOnlyList<TestEntity>>> ListAsync(Specification<TestEntity> spec, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, IReadOnlyList<TestEntity>>.Right(Array.Empty<TestEntity>()));
        public Task<Either<EncinaError, TestEntity>> FirstOrDefaultAsync(Specification<TestEntity> spec, CancellationToken ct = default) => Task.FromResult(GetByIdResult);
        public Task<Either<EncinaError, int>> CountAsync(CancellationToken ct = default) => Task.FromResult(Either<EncinaError, int>.Right(0));
        public Task<Either<EncinaError, int>> CountAsync(Specification<TestEntity> spec, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, int>.Right(0));
        public Task<Either<EncinaError, bool>> AnyAsync(Specification<TestEntity> spec, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, bool>.Right(false));
        public Task<Either<EncinaError, bool>> AnyAsync(CancellationToken ct = default) => Task.FromResult(Either<EncinaError, bool>.Right(false));
        public Task<Either<EncinaError, PagedResult<TestEntity>>> GetPagedAsync(PaginationOptions p, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Either<EncinaError, PagedResult<TestEntity>>> GetPagedAsync(Specification<TestEntity> spec, PaginationOptions p, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Either<EncinaError, PagedResult<TestEntity>>> GetPagedAsync(IPagedSpecification<TestEntity> spec, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Either<EncinaError, TestEntity>> UpdateAsync(TestEntity entity, CancellationToken ct = default) => Task.FromResult(AddResult);
        public Task<Either<EncinaError, Unit>> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
        public Task<Either<EncinaError, Unit>> DeleteAsync(TestEntity entity, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
        public Task<Either<EncinaError, IReadOnlyList<TestEntity>>> AddRangeAsync(IEnumerable<TestEntity> entities, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, IReadOnlyList<TestEntity>>.Right(Array.Empty<TestEntity>()));
        public Task<Either<EncinaError, Unit>> UpdateRangeAsync(IEnumerable<TestEntity> entities, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
        public Task<Either<EncinaError, int>> DeleteRangeAsync(Specification<TestEntity> spec, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, int>.Right(0));
        public Task<Either<EncinaError, Unit>> UpdateImmutableAsync(TestEntity modified, CancellationToken ct = default) => Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    [Fact]
    public void ImplementsIFunctionalRepository()
    {
        typeof(InstrumentedFunctionalRepository<TestEntity, Guid>)
            .GetInterfaces()
            .ShouldContain(typeof(IFunctionalRepository<TestEntity, Guid>));
    }

    [Fact]
    public async Task GetByIdAsync_DelegatesToInner()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "test" };
        var inner = new StubRepository { GetByIdResult = entity };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.GetByIdAsync(entity.Id);

        result.ShouldBeSuccess().ShouldBe(entity);
        inner.GetByIdCalls.ShouldBe(1);
    }

    [Fact]
    public async Task AddAsync_DelegatesToInner()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "new" };
        var inner = new StubRepository { AddResult = entity };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.AddAsync(entity);

        result.ShouldBeSuccess().ShouldBe(entity);
        inner.AddCalls.ShouldBe(1);
    }
}
