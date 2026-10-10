using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.GuardTests.Messaging.DeadLetter;

/// <summary>
/// Guard clause tests for <see cref="DeadLetterSourceCapture"/> and
/// <see cref="DeadLetterOrchestrator.AddSerializedAsync"/>.
/// </summary>
public class DeadLetterSourceCaptureGuardTests
{
    private static DeadLetterContext Context(string sourcePattern = "Outbox")
        => new(EncinaError.New("err"), null, sourcePattern, 3, DateTime.UtcNow, SourceMessageId: "m-1");

    #region DeadLetterSourceCapture Constructor

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterSourceCapture(null!, new DeadLetterOptions(), NullLogger<DeadLetterSourceCapture>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterSourceCapture(
            Substitute.For<IServiceScopeFactory>(), null!, NullLogger<DeadLetterSourceCapture>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterSourceCapture(Substitute.For<IServiceScopeFactory>(), new DeadLetterOptions(), null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region DeadLetterSourceCapture methods

    [Fact]
    public async Task CaptureAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = async () => await CreateCapture().CaptureAsync(null!, Context());

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task CaptureAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await CreateCapture().CaptureAsync("request", null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CaptureSerializedAsync_NullOrEmptyRequestType_ThrowsArgumentException(string? requestType)
    {
        var act = async () => await CreateCapture().CaptureSerializedAsync(requestType!, "{}", Context());

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("requestType");
    }

    [Fact]
    public async Task CaptureSerializedAsync_NullRequestContent_ThrowsArgumentNullException()
    {
        var act = async () => await CreateCapture().CaptureSerializedAsync("Type", null!, Context());

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("requestContent");
    }

    [Fact]
    public async Task CaptureSerializedAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await CreateCapture().CaptureSerializedAsync("Type", "{}", null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task CaptureFailedMessageAsync_NullFailedMessage_ThrowsArgumentNullException()
    {
        var act = async () => await CreateCapture().CaptureFailedMessageAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("failedMessage");
    }

    private static DeadLetterSourceCapture CreateCapture()
        => new(Substitute.For<IServiceScopeFactory>(), new DeadLetterOptions(), NullLogger<DeadLetterSourceCapture>.Instance);

    #endregion

    #region DeadLetterOrchestrator.AddSerializedAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AddSerializedAsync_NullOrEmptyRequestType_ThrowsArgumentException(string? requestType)
    {
        var act = async () => await CreateOrchestrator().AddSerializedAsync(requestType!, "{}", Context());

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("requestType");
    }

    [Fact]
    public async Task AddSerializedAsync_NullRequestContent_ThrowsArgumentNullException()
    {
        var act = async () => await CreateOrchestrator().AddSerializedAsync("Type", null!, Context());

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("requestContent");
    }

    [Fact]
    public async Task AddSerializedAsync_NullContext_ThrowsArgumentNullException()
    {
        var act = async () => await CreateOrchestrator().AddSerializedAsync("Type", "{}", null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task AddSerializedAsync_EmptySourcePattern_ThrowsArgumentException()
    {
        var act = async () => await CreateOrchestrator().AddSerializedAsync("Type", "{}", Context(sourcePattern: ""));

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("context.SourcePattern");
    }

    private static DeadLetterOrchestrator CreateOrchestrator()
        => new(
            Substitute.For<IDeadLetterStore>(),
            Substitute.For<IDeadLetterMessageFactory>(),
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            Substitute.For<IRequestContextAccessor>());

    #endregion
}
