using Encina.Messaging.Recoverability;
using Shouldly;

namespace Encina.GuardTests.Messaging.Recoverability;

/// <summary>
/// Guard clause tests for <see cref="RecoverabilityContext"/>.
/// </summary>
public class RecoverabilityContextGuardTests
{
    [Fact]
    public void CreateFailedMessage_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new RecoverabilityContext();

        var act = () => sut.CreateFailedMessage(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("request");
    }

    [Fact]
    public void CreateFailedMessage_ValidRequest_ReturnsMessageForTheRequest()
    {
        var sut = new RecoverabilityContext();
        var request = new object();

        var message = sut.CreateFailedMessage(request);

        message.Request.ShouldBeSameAs(request);
        message.Id.ShouldBe(sut.Id);
    }

    [Fact]
    public void AbsorbRedispatch_NullRedispatch_ThrowsArgumentNullException()
    {
        var sut = new RecoverabilityContext();

        var act = () => sut.AbsorbRedispatch(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("redispatch");
    }

    [Fact]
    public void Constructor_NullTimeProvider_CreatesInstance()
    {
        var sut = new RecoverabilityContext(timeProvider: null);

        sut.ShouldNotBeNull();
    }
}
