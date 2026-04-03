using Encina.DomainModeling;
using Encina.OpenTelemetry.Repository;
using Encina.Testing;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.Repository;

/// <summary>
/// Unit tests for <see cref="InstrumentedFunctionalRepository{TEntity, TId}"/>.
/// </summary>
public sealed class InstrumentedFunctionalRepositoryTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Stub implementation for IFunctionalRepository since NSubstitute cannot proxy
    /// the contravariant TId generic parameter.
    /// </summary>
    private sealed class StubFunctionalRepository : IFunctionalRepository<TestEntity, Guid>
    {
        public Either<EncinaError, TestEntity>? GetByIdResult { get; set; }
        public Either<EncinaError, IReadOnlyList<TestEntity>>? ListResult { get; set; }
        public Either<EncinaError, TestEntity>? AddResult { get; set; }
        public Either<EncinaError, TestEntity>? UpdateResult { get; set; }
        public Either<EncinaError, Unit>? DeleteResult { get; set; }
        public Either<EncinaError, int>? CountResult { get; set; }
        public Either<EncinaError, bool>? AnyResult { get; set; }
        public Either<EncinaError, IReadOnlyList<TestEntity>>? AddRangeResult { get; set; }
        public Either<EncinaError, Unit>? UpdateRangeResult { get; set; }
        public Either<EncinaError, int>? DeleteRangeResult { get; set; }
        public Either<EncinaError, PagedResult<TestEntity>>? PagedResult { get; set; }
        public Either<EncinaError, Unit>? UpdateImmutableResult { get; set; }
        public int CallCount { get; private set; }

        public Task<Either<EncinaError, TestEntity>> GetByIdAsync(Guid id, CancellationToken ct = default) { CallCount++; return Task.FromResult(GetByIdResult!.Value); }
        public Task<Either<EncinaError, IReadOnlyList<TestEntity>>> ListAsync(CancellationToken ct = default) { CallCount++; return Task.FromResult(ListResult!.Value); }
        public Task<Either<EncinaError, IReadOnlyList<TestEntity>>> ListAsync(Specification<TestEntity> spec, CancellationToken ct = default) { CallCount++; return Task.FromResult(ListResult!.Value); }
        public Task<Either<EncinaError, TestEntity>> FirstOrDefaultAsync(Specification<TestEntity> spec, CancellationToken ct = default) { CallCount++; return Task.FromResult(GetByIdResult!.Value); }
        public Task<Either<EncinaError, int>> CountAsync(CancellationToken ct = default) { CallCount++; return Task.FromResult(CountResult!.Value); }
        public Task<Either<EncinaError, int>> CountAsync(Specification<TestEntity> spec, CancellationToken ct = default) { CallCount++; return Task.FromResult(CountResult!.Value); }
        public Task<Either<EncinaError, bool>> AnyAsync(Specification<TestEntity> spec, CancellationToken ct = default) { CallCount++; return Task.FromResult(AnyResult!.Value); }
        public Task<Either<EncinaError, bool>> AnyAsync(CancellationToken ct = default) { CallCount++; return Task.FromResult(AnyResult!.Value); }
        public Task<Either<EncinaError, PagedResult<TestEntity>>> GetPagedAsync(PaginationOptions pagination, CancellationToken ct = default) { CallCount++; return Task.FromResult(PagedResult!.Value); }
        public Task<Either<EncinaError, PagedResult<TestEntity>>> GetPagedAsync(Specification<TestEntity> spec, PaginationOptions pagination, CancellationToken ct = default) { CallCount++; return Task.FromResult(PagedResult!.Value); }
        public Task<Either<EncinaError, PagedResult<TestEntity>>> GetPagedAsync(IPagedSpecification<TestEntity> spec, CancellationToken ct = default) { CallCount++; return Task.FromResult(PagedResult!.Value); }
        public Task<Either<EncinaError, TestEntity>> AddAsync(TestEntity entity, CancellationToken ct = default) { CallCount++; return Task.FromResult(AddResult!.Value); }
        public Task<Either<EncinaError, TestEntity>> UpdateAsync(TestEntity entity, CancellationToken ct = default) { CallCount++; return Task.FromResult(UpdateResult!.Value); }
        public Task<Either<EncinaError, Unit>> DeleteAsync(Guid id, CancellationToken ct = default) { CallCount++; return Task.FromResult(DeleteResult!.Value); }
        public Task<Either<EncinaError, Unit>> DeleteAsync(TestEntity entity, CancellationToken ct = default) { CallCount++; return Task.FromResult(DeleteResult!.Value); }
        public Task<Either<EncinaError, IReadOnlyList<TestEntity>>> AddRangeAsync(IEnumerable<TestEntity> entities, CancellationToken ct = default) { CallCount++; return Task.FromResult(AddRangeResult!.Value); }
        public Task<Either<EncinaError, Unit>> UpdateRangeAsync(IEnumerable<TestEntity> entities, CancellationToken ct = default) { CallCount++; return Task.FromResult(UpdateRangeResult!.Value); }
        public Task<Either<EncinaError, int>> DeleteRangeAsync(Specification<TestEntity> spec, CancellationToken ct = default) { CallCount++; return Task.FromResult(DeleteRangeResult!.Value); }
        public Task<Either<EncinaError, Unit>> UpdateImmutableAsync(TestEntity modified, CancellationToken ct = default) { CallCount++; return Task.FromResult(UpdateImmutableResult!.Value); }
    }

    [Fact]
    public async Task GetByIdAsync_DelegatesToInner()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var inner = new StubFunctionalRepository { GetByIdResult = entity };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.GetByIdAsync(entity.Id);

        result.ShouldBeSuccess().ShouldBe(entity);
        inner.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListAsync_DelegatesToInner()
    {
        var entities = new List<TestEntity> { new() };
        var inner = new StubFunctionalRepository { ListResult = Either<EncinaError, IReadOnlyList<TestEntity>>.Right(entities) };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.ListAsync();

        result.ShouldBeSuccess().Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddAsync_DelegatesToInner()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var inner = new StubFunctionalRepository { AddResult = entity };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.AddAsync(entity);

        result.ShouldBeSuccess().ShouldBe(entity);
    }

    [Fact]
    public async Task CountAsync_DelegatesToInner()
    {
        var inner = new StubFunctionalRepository { CountResult = 5 };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.CountAsync();

        result.ShouldBeSuccess().ShouldBe(5);
    }

    [Fact]
    public async Task AnyAsync_DelegatesToInner()
    {
        var inner = new StubFunctionalRepository { AnyResult = true };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.AnyAsync();

        result.ShouldBeSuccess().ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToInner()
    {
        var inner = new StubFunctionalRepository { DeleteResult = Unit.Default };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.DeleteAsync(Guid.NewGuid());

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task GetByIdAsync_WhenInnerFails_ReturnsError()
    {
        var error = EncinaError.New("not found");
        var inner = new StubFunctionalRepository { GetByIdResult = Either<EncinaError, TestEntity>.Left(error) };
        var sut = new InstrumentedFunctionalRepository<TestEntity, Guid>(inner, "Test");

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeError();
    }
}
