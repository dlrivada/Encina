using Encina.DomainModeling;
using Encina.OpenTelemetry.BulkOperations;
using Encina.OpenTelemetry.UnitOfWork;
using LanguageExt;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Observability.OpenTelemetry;

/// <summary>
/// Integration tests that specifically exercise <see cref="BulkOperationsMetricsInitializer"/>,
/// <see cref="InstrumentedBulkOperations{TEntity}"/>, and <see cref="InstrumentedUnitOfWork"/>
/// to ensure these tagged files are covered by the integration test runner.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "OpenTelemetry")]
public sealed class BulkOperationsInstrumentationTests
{
    // --- BulkOperationsMetricsInitializer ---

    [Fact]
    public async Task BulkOperationsMetricsInitializer_StartAsync_InitializesMetrics()
    {
        // Arrange
        var sut = new BulkOperationsMetricsInitializer();

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert - no exception means metrics were initialized
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task BulkOperationsMetricsInitializer_StopAsync_WithoutStart_DoesNotThrow()
    {
        // Arrange
        var sut = new BulkOperationsMetricsInitializer();

        // Act & Assert
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task BulkOperationsMetricsInitializer_DoubleStart_DoesNotThrow()
    {
        var sut = new BulkOperationsMetricsInitializer();

        await sut.StartAsync(CancellationToken.None);
        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);
    }

    // --- InstrumentedBulkOperations error paths ---

    [Fact]
    public async Task InstrumentedBulkOperations_BulkInsertAsync_WhenInnerFails_ReturnsLeft()
    {
        // Arrange
        var inner = Substitute.For<IBulkOperations<BulkOpTestEntity>>();
        var error = EncinaErrors.Create("test.error", "bulk insert failed");
        inner.BulkInsertAsync(Arg.Any<IEnumerable<BulkOpTestEntity>>(), Arg.Any<BulkConfig?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Left(error));

        var sut = new InstrumentedBulkOperations<BulkOpTestEntity>(inner, "Dapper");

        // Act
        var result = await sut.BulkInsertAsync([new BulkOpTestEntity()]);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkUpdateAsync_WhenInnerFails_ReturnsLeft()
    {
        var inner = Substitute.For<IBulkOperations<BulkOpTestEntity>>();
        var error = EncinaErrors.Create("test.error", "bulk update failed");
        inner.BulkUpdateAsync(Arg.Any<IEnumerable<BulkOpTestEntity>>(), Arg.Any<BulkConfig?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Left(error));

        var sut = new InstrumentedBulkOperations<BulkOpTestEntity>(inner, "Dapper");

        var result = await sut.BulkUpdateAsync([new BulkOpTestEntity()]);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkDeleteAsync_WhenInnerFails_ReturnsLeft()
    {
        var inner = Substitute.For<IBulkOperations<BulkOpTestEntity>>();
        var error = EncinaErrors.Create("test.error", "bulk delete failed");
        inner.BulkDeleteAsync(Arg.Any<IEnumerable<BulkOpTestEntity>>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Left(error));

        var sut = new InstrumentedBulkOperations<BulkOpTestEntity>(inner, "ADO");

        var result = await sut.BulkDeleteAsync([new BulkOpTestEntity()]);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkMergeAsync_WhenInnerFails_ReturnsLeft()
    {
        var inner = Substitute.For<IBulkOperations<BulkOpTestEntity>>();
        var error = EncinaErrors.Create("test.error", "bulk merge failed");
        inner.BulkMergeAsync(Arg.Any<IEnumerable<BulkOpTestEntity>>(), Arg.Any<BulkConfig?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Left(error));

        var sut = new InstrumentedBulkOperations<BulkOpTestEntity>(inner, "ADO");

        var result = await sut.BulkMergeAsync([new BulkOpTestEntity()]);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InstrumentedBulkOperations_BulkReadAsync_WhenInnerFails_ReturnsLeft()
    {
        var inner = Substitute.For<IBulkOperations<BulkOpTestEntity>>();
        var error = EncinaErrors.Create("test.error", "bulk read failed");
        inner.BulkReadAsync(Arg.Any<IEnumerable<object>>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<BulkOpTestEntity>>.Left(error));

        var sut = new InstrumentedBulkOperations<BulkOpTestEntity>(inner, "EFCore");

        var result = await sut.BulkReadAsync(new object[] { 1 });

        result.IsLeft.ShouldBeTrue();
    }

    // --- InstrumentedUnitOfWork error paths ---

    [Fact]
    public async Task InstrumentedUnitOfWork_SaveChangesAsync_WhenInnerFails_ReturnsLeft()
    {
        var inner = Substitute.For<IUnitOfWork>();
        var error = EncinaErrors.Create("test.error", "save failed");
        inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Left(error));

        var sut = new InstrumentedUnitOfWork(inner);

        var result = await sut.SaveChangesAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InstrumentedUnitOfWork_BeginTransactionAsync_WhenInnerFails_ReturnsLeft()
    {
        var inner = Substitute.For<IUnitOfWork>();
        var error = EncinaErrors.Create("test.error", "begin failed");
        inner.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(error));

        var sut = new InstrumentedUnitOfWork(inner);

        var result = await sut.BeginTransactionAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InstrumentedUnitOfWork_CommitAsync_WhenInnerFails_ReturnsLeft()
    {
        var inner = Substitute.For<IUnitOfWork>();
        var error = EncinaErrors.Create("test.error", "commit failed");
        inner.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(error));

        var sut = new InstrumentedUnitOfWork(inner);

        var result = await sut.CommitAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void InstrumentedUnitOfWork_Repository_DelegatesToInner()
    {
        var inner = Substitute.For<IUnitOfWork>();
        var mockRepo = Substitute.For<IFunctionalRepository<BulkOpTestEntity, int>>();
        inner.Repository<BulkOpTestEntity, int>().Returns(mockRepo);

        var sut = new InstrumentedUnitOfWork(inner);

        var repo = sut.Repository<BulkOpTestEntity, int>();

        repo.ShouldBeSameAs(mockRepo);
    }
}

/// <summary>
/// Simple test entity for bulk operations instrumentation tests.
/// </summary>
public sealed class BulkOpTestEntity;
