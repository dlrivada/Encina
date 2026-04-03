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
}
