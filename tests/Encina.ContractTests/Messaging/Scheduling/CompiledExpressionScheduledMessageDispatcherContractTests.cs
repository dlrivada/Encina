using System.Diagnostics.CodeAnalysis;
using Encina.Messaging.Scheduling;

using LanguageExt;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Messaging.Scheduling;

/// <summary>
/// Contract tests verifying that <see cref="CompiledExpressionScheduledMessageDispatcher"/>
/// satisfies the <see cref="IScheduledMessageDispatcher"/> contract invariants.
/// </summary>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
    Justification = "NSubstitute setup calls return ValueTask for mock configuration, not for direct consumption.")]
public sealed class CompiledExpressionScheduledMessageDispatcherContractTests : IDisposable
{
    public CompiledExpressionScheduledMessageDispatcherContractTests()
    {
        CompiledExpressionScheduledMessageDispatcher.ClearCache();
    }

    public void Dispose()
    {
        CompiledExpressionScheduledMessageDispatcher.ClearCache();
    }

    private sealed record ContractCommand(int Value) : IRequest<int>;
    private sealed record ContractNotification(string Msg) : INotification;
    private sealed record NotAMessage(string Data);

    [Fact]
    public async Task DispatchAsync_KnownRequest_NeverThrows()
    {
        // Contract: DispatchAsync must NEVER throw for known shapes (IRequest<>/INotification).
        // Failures are signalled as Left, not as exceptions.
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<ContractCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(0)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        var act = async () => await sut.DispatchAsync(typeof(ContractCommand), new ContractCommand(1), CancellationToken.None);
        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_KnownNotification_NeverThrows()
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<ContractNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        var act = async () => await sut.DispatchAsync(typeof(ContractNotification), new ContractNotification("hi"), CancellationToken.None);
        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_UnknownShape_NeverThrows_ReturnsLeft()
    {
        // Contract: even unknown shapes must not throw — they return Left.
        var encina = Substitute.For<IEncina>();
        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);

        var act = async () => await sut.DispatchAsync(typeof(NotAMessage), new NotAMessage("x"), CancellationToken.None);
        await act.ShouldNotThrowAsync();

        var result = await sut.DispatchAsync(typeof(NotAMessage), new NotAMessage("x"), CancellationToken.None);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_PropagatesEitherFromEncina_Left()
    {
        // Contract: if IEncina returns Left, the dispatcher propagates it unchanged.
        var expectedError = EncinaError.New("encina failure");
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<ContractCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Left<EncinaError, int>(expectedError)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);
        var result = await sut.DispatchAsync(typeof(ContractCommand), new ContractCommand(1), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().Message.ShouldBe("encina failure");
    }

    [Fact]
    public async Task DispatchAsync_PropagatesEitherFromEncina_Right()
    {
        // Contract: if IEncina returns Right, the dispatcher returns Right(Unit).
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<ContractCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(42)));

        var sut = new CompiledExpressionScheduledMessageDispatcher(encina);
        var result = await sut.DispatchAsync(typeof(ContractCommand), new ContractCommand(1), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }
}
