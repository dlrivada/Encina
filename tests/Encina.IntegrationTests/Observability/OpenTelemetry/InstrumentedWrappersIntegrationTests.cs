using Encina.DomainModeling;
using Encina.OpenTelemetry.BulkOperations;
using Encina.OpenTelemetry.UnitOfWork;
using LanguageExt;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Observability.OpenTelemetry;

/// <summary>
/// Integration tests for instrumented wrappers that verify end-to-end delegation
/// through the decorator chain (BulkOperations, UnitOfWork, MetricsInitializer).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "OpenTelemetry")]
public sealed class InstrumentedWrappersIntegrationTests
{
    [Fact]
    public async Task BulkOperationsMetricsInitializer_FullLifecycle_StartAndStop()
    {
        // Arrange
        var initializer = new BulkOperationsMetricsInitializer();

        // Act & Assert - full hosted service lifecycle
        await initializer.StartAsync(CancellationToken.None);
        await initializer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task InstrumentedUnitOfWork_TransactionLifecycle_DelegatesCorrectly()
    {
        // Arrange
        var inner = Substitute.For<IUnitOfWork>();
        inner.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));
        inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Right(2));
        inner.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var sut = new InstrumentedUnitOfWork(inner);

        // Act - full transaction lifecycle
        var beginResult = await sut.BeginTransactionAsync();
        var saveResult = await sut.SaveChangesAsync();
        var commitResult = await sut.CommitAsync();

        // Assert
        beginResult.ShouldBeSuccess();
        saveResult.ShouldBeSuccess().ShouldBe(2);
        commitResult.ShouldBeSuccess();

        await sut.DisposeAsync();
    }

    [Fact]
    public async Task InstrumentedUnitOfWork_RollbackAsync_DelegatesCorrectly()
    {
        // Arrange
        var inner = Substitute.For<IUnitOfWork>();
        inner.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));
        inner.RollbackAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new InstrumentedUnitOfWork(inner);

        // Act
        await sut.BeginTransactionAsync();
        await sut.RollbackAsync();

        // Assert
        await inner.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void InstrumentedUnitOfWork_HasActiveTransaction_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IUnitOfWork>();
        inner.HasActiveTransaction.Returns(true);

        var sut = new InstrumentedUnitOfWork(inner);

        // Act & Assert
        sut.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public void InstrumentedUnitOfWork_UpdateImmutable_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IUnitOfWork>();
        inner.UpdateImmutable(Arg.Any<InstrumentedWrapperTestEntity>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var sut = new InstrumentedUnitOfWork(inner);

        // Act
        var result = sut.UpdateImmutable(new InstrumentedWrapperTestEntity());

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task InstrumentedUnitOfWork_UpdateImmutableAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IUnitOfWork>();
        inner.UpdateImmutableAsync(Arg.Any<InstrumentedWrapperTestEntity>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var sut = new InstrumentedUnitOfWork(inner);

        // Act
        var result = await sut.UpdateImmutableAsync(new InstrumentedWrapperTestEntity());

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkInsertAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IBulkOperations<InstrumentedWrapperTestEntity>>();
        inner.BulkInsertAsync(Arg.Any<IEnumerable<InstrumentedWrapperTestEntity>>(), Arg.Any<BulkConfig?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Right(5));

        var sut = new InstrumentedBulkOperations<InstrumentedWrapperTestEntity>(inner, "EFCore");
        var entities = new List<InstrumentedWrapperTestEntity> { new(), new(), new(), new(), new() };

        // Act
        var result = await sut.BulkInsertAsync(entities);

        // Assert
        result.ShouldBeSuccess().ShouldBe(5);
        await inner.Received(1).BulkInsertAsync(entities, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkUpdateAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IBulkOperations<InstrumentedWrapperTestEntity>>();
        inner.BulkUpdateAsync(Arg.Any<IEnumerable<InstrumentedWrapperTestEntity>>(), Arg.Any<BulkConfig?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Right(3));

        var sut = new InstrumentedBulkOperations<InstrumentedWrapperTestEntity>(inner, "EFCore");
        var entities = new List<InstrumentedWrapperTestEntity> { new(), new(), new() };

        // Act
        var result = await sut.BulkUpdateAsync(entities);

        // Assert
        result.ShouldBeSuccess().ShouldBe(3);
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkDeleteAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IBulkOperations<InstrumentedWrapperTestEntity>>();
        inner.BulkDeleteAsync(Arg.Any<IEnumerable<InstrumentedWrapperTestEntity>>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Right(2));

        var sut = new InstrumentedBulkOperations<InstrumentedWrapperTestEntity>(inner, "EFCore");
        var entities = new List<InstrumentedWrapperTestEntity> { new(), new() };

        // Act
        var result = await sut.BulkDeleteAsync(entities);

        // Assert
        result.ShouldBeSuccess().ShouldBe(2);
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkMergeAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IBulkOperations<InstrumentedWrapperTestEntity>>();
        inner.BulkMergeAsync(Arg.Any<IEnumerable<InstrumentedWrapperTestEntity>>(), Arg.Any<BulkConfig?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Right(4));

        var sut = new InstrumentedBulkOperations<InstrumentedWrapperTestEntity>(inner, "EFCore");

        // Act
        var result = await sut.BulkMergeAsync(new List<InstrumentedWrapperTestEntity> { new(), new() });

        // Assert
        result.ShouldBeSuccess().ShouldBe(4);
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkReadAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IBulkOperations<InstrumentedWrapperTestEntity>>();
        var readResult = new List<InstrumentedWrapperTestEntity> { new(), new() };
        inner.BulkReadAsync(Arg.Any<IEnumerable<object>>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<InstrumentedWrapperTestEntity>>.Right(readResult));

        var sut = new InstrumentedBulkOperations<InstrumentedWrapperTestEntity>(inner, "EFCore");

        // Act
        var result = await sut.BulkReadAsync(new object[] { 1, 2 });

        // Assert
        result.ShouldBeSuccess().Count.ShouldBe(2);
    }

}

/// <summary>
/// Simple test entity for instrumented wrapper tests.
/// Must be public for NSubstitute proxy generation on generic interfaces.
/// </summary>
public sealed class InstrumentedWrapperTestEntity;
