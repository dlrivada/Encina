using Encina.DomainModeling;
using Encina.OpenTelemetry.UnitOfWork;
using Encina.Testing;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.UnitOfWork;

/// <summary>
/// Unit tests for <see cref="InstrumentedUnitOfWork"/>.
/// </summary>
public sealed class InstrumentedUnitOfWorkTests : IAsyncDisposable
{
    private sealed class TestEntity
    {
        public Guid Id { get; init; }
    }

    private readonly IUnitOfWork _inner;
    private readonly InstrumentedUnitOfWork _sut;

    public InstrumentedUnitOfWorkTests()
    {
        _inner = Substitute.For<IUnitOfWork>();
        _sut = new InstrumentedUnitOfWork(_inner);
    }

    public async ValueTask DisposeAsync()
    {
        await _sut.DisposeAsync();
    }

    [Fact]
    public void HasActiveTransaction_DelegatesToInner()
    {
        _inner.HasActiveTransaction.Returns(true);

        _sut.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public void HasActiveTransaction_WhenFalse_ReturnsFalse()
    {
        _inner.HasActiveTransaction.Returns(false);

        _sut.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToInner()
    {
        _inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Right(3));

        var result = await _sut.SaveChangesAsync();

        result.ShouldBeSuccess().ShouldBe(3);
    }

    [Fact]
    public async Task BeginTransactionAsync_DelegatesToInner()
    {
        _inner.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.BeginTransactionAsync();

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task CommitAsync_DelegatesToInner()
    {
        _inner.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.CommitAsync();

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task RollbackAsync_DelegatesToInner()
    {
        await _sut.RollbackAsync();

        await _inner.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UpdateImmutable_DelegatesToInner()
    {
        var entity = new TestEntity();
        _inner.UpdateImmutable(entity)
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = _sut.UpdateImmutable(entity);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task UpdateImmutableAsync_DelegatesToInner()
    {
        var entity = new TestEntity();
        _inner.UpdateImmutableAsync(entity, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.UpdateImmutableAsync(entity);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task DisposeAsync_DelegatesToInner()
    {
        await _sut.DisposeAsync();

        await _inner.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenInnerFails_ReturnsError()
    {
        var error = EncinaError.New("save failed");
        _inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Left(error));

        var result = await _sut.SaveChangesAsync();

        result.ShouldBeError();
    }
}
