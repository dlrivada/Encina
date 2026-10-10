using Encina.Messaging.DeadLetter;
using Encina.OpenTelemetry.MessagingStores;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="InstrumentedDeadLetterStore"/> to verify null parameter handling.
/// </summary>
public sealed class InstrumentedDeadLetterStoreGuardTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedDeadLetterStore(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        var sut = new InstrumentedDeadLetterStore(Substitute.For<IDeadLetterStore>());

        var ex = await Should.ThrowAsync<ArgumentNullException>(() => sut.AddAsync(null!));

        ex.ParamName.ShouldBe("message");
    }

    [Fact]
    public async Task DeleteManyAsync_NullFilter_ThrowsArgumentNullException()
    {
        var sut = new InstrumentedDeadLetterStore(Substitute.For<IDeadLetterStore>());

        var ex = await Should.ThrowAsync<ArgumentNullException>(() => sut.DeleteManyAsync(null!));

        ex.ParamName.ShouldBe("filter");
    }
}
