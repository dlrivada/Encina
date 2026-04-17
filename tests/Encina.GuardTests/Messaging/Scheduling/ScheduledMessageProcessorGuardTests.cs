using Encina.Messaging.Scheduling;
using Shouldly;

namespace Encina.GuardTests.Messaging.Scheduling;

/// <summary>
/// Guard clause tests for <see cref="ScheduledMessageProcessor"/> constructor.
/// </summary>
public sealed class ScheduledMessageProcessorGuardTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly SchedulingOptions _options = new();
    private readonly ILogger<ScheduledMessageProcessor> _logger = NullLogger<ScheduledMessageProcessor>.Instance;

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new ScheduledMessageProcessor(null!, _options, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ScheduledMessageProcessor(_serviceProvider, null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ScheduledMessageProcessor(_serviceProvider, _options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullTimeProvider_Succeeds()
    {
        var act = () => new ScheduledMessageProcessor(_serviceProvider, _options, _logger, timeProvider: null);
        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        var act = () => new ScheduledMessageProcessor(_serviceProvider, _options, _logger);
        Should.NotThrow(act);
    }
}
