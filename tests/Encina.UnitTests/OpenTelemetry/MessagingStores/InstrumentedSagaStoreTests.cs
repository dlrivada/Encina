using Encina.Messaging.Sagas;
using Encina.OpenTelemetry.MessagingStores;
using Encina.Testing;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.MessagingStores;

/// <summary>
/// Unit tests for <see cref="InstrumentedSagaStore"/>.
/// </summary>
public sealed class InstrumentedSagaStoreTests
{
    private readonly ISagaStore _inner;
    private readonly InstrumentedSagaStore _sut;

    public InstrumentedSagaStoreTests()
    {
        _inner = Substitute.For<ISagaStore>();
        _sut = new InstrumentedSagaStore(_inner);
    }

    [Fact]
    public async Task GetAsync_DelegatesToInner()
    {
        var id = Guid.NewGuid();
        _inner.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Option<ISagaState>>.Right(Option<ISagaState>.None));

        var result = await _sut.GetAsync(id);

        result.ShouldBeSuccess();
        await _inner.Received(1).GetAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_DelegatesToInner()
    {
        var saga = Substitute.For<ISagaState>();
        saga.SagaId.Returns(Guid.NewGuid());
        saga.SagaType.Returns("TestSaga");
        _inner.AddAsync(saga, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.AddAsync(saga);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToInner()
    {
        var saga = Substitute.For<ISagaState>();
        saga.SagaId.Returns(Guid.NewGuid());
        saga.SagaType.Returns("TestSaga");
        saga.Status.Returns("Running");
        _inner.UpdateAsync(saga, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.UpdateAsync(saga);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task GetStuckSagasAsync_DelegatesToInner()
    {
        var sagas = new List<ISagaState>();
        _inner.GetStuckSagasAsync(TimeSpan.FromMinutes(5), 10, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(sagas));

        var result = await _sut.GetStuckSagasAsync(TimeSpan.FromMinutes(5), 10);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task GetExpiredSagasAsync_DelegatesToInner()
    {
        var sagas = new List<ISagaState>();
        _inner.GetExpiredSagasAsync(10, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(sagas));

        var result = await _sut.GetExpiredSagasAsync(10);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToInner()
    {
        _inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await _sut.SaveChangesAsync();

        result.ShouldBeSuccess();
    }
}
