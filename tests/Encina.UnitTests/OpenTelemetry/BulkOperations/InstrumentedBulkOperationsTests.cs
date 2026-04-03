using Encina.DomainModeling;
using Encina.OpenTelemetry.BulkOperations;
using Encina.Testing;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.BulkOperations;

/// <summary>
/// Unit tests for <see cref="InstrumentedBulkOperations{TEntity}"/>.
/// </summary>
public sealed class InstrumentedBulkOperationsTests
{
    private sealed class TestEntity
    {
        public Guid Id { get; init; }
    }

    private sealed class StubBulkOperations : IBulkOperations<TestEntity>
    {
        public Either<EncinaError, int>? IntResult { get; set; }
        public Either<EncinaError, IReadOnlyList<TestEntity>>? ReadResult { get; set; }
        public int CallCount { get; private set; }

        public Task<Either<EncinaError, int>> BulkInsertAsync(IEnumerable<TestEntity> entities, BulkConfig? config = null, CancellationToken ct = default) { CallCount++; return Task.FromResult(IntResult!.Value); }
        public Task<Either<EncinaError, int>> BulkUpdateAsync(IEnumerable<TestEntity> entities, BulkConfig? config = null, CancellationToken ct = default) { CallCount++; return Task.FromResult(IntResult!.Value); }
        public Task<Either<EncinaError, int>> BulkDeleteAsync(IEnumerable<TestEntity> entities, CancellationToken ct = default) { CallCount++; return Task.FromResult(IntResult!.Value); }
        public Task<Either<EncinaError, int>> BulkMergeAsync(IEnumerable<TestEntity> entities, BulkConfig? config = null, CancellationToken ct = default) { CallCount++; return Task.FromResult(IntResult!.Value); }
        public Task<Either<EncinaError, IReadOnlyList<TestEntity>>> BulkReadAsync(IEnumerable<object> ids, CancellationToken ct = default) { CallCount++; return Task.FromResult(ReadResult!.Value); }
    }

    [Fact]
    public async Task BulkInsertAsync_DelegatesToInner()
    {
        var inner = new StubBulkOperations { IntResult = 1 };
        var sut = new InstrumentedBulkOperations<TestEntity>(inner, "TestProvider");

        var result = await sut.BulkInsertAsync(new[] { new TestEntity() });

        result.ShouldBeSuccess().ShouldBe(1);
        inner.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task BulkUpdateAsync_DelegatesToInner()
    {
        var inner = new StubBulkOperations { IntResult = 1 };
        var sut = new InstrumentedBulkOperations<TestEntity>(inner, "TestProvider");

        var result = await sut.BulkUpdateAsync(new[] { new TestEntity() });

        result.ShouldBeSuccess().ShouldBe(1);
    }

    [Fact]
    public async Task BulkDeleteAsync_DelegatesToInner()
    {
        var inner = new StubBulkOperations { IntResult = 1 };
        var sut = new InstrumentedBulkOperations<TestEntity>(inner, "TestProvider");

        var result = await sut.BulkDeleteAsync(new[] { new TestEntity() });

        result.ShouldBeSuccess().ShouldBe(1);
    }

    [Fact]
    public async Task BulkMergeAsync_DelegatesToInner()
    {
        var inner = new StubBulkOperations { IntResult = 1 };
        var sut = new InstrumentedBulkOperations<TestEntity>(inner, "TestProvider");

        var result = await sut.BulkMergeAsync(new[] { new TestEntity() });

        result.ShouldBeSuccess().ShouldBe(1);
    }

    [Fact]
    public async Task BulkReadAsync_DelegatesToInner()
    {
        var entities = new List<TestEntity> { new() };
        var inner = new StubBulkOperations { ReadResult = Either<EncinaError, IReadOnlyList<TestEntity>>.Right(entities) };
        var sut = new InstrumentedBulkOperations<TestEntity>(inner, "TestProvider");

        var result = await sut.BulkReadAsync(new object[] { Guid.NewGuid() });

        result.ShouldBeSuccess().Count.ShouldBe(1);
    }

    [Fact]
    public async Task BulkInsertAsync_WhenInnerFails_ReturnsError()
    {
        var inner = new StubBulkOperations { IntResult = Either<EncinaError, int>.Left(EncinaError.New("failed")) };
        var sut = new InstrumentedBulkOperations<TestEntity>(inner, "TestProvider");

        var result = await sut.BulkInsertAsync(new[] { new TestEntity() });

        result.ShouldBeError();
    }
}
