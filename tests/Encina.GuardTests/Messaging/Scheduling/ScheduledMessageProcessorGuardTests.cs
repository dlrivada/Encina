using Encina.Messaging.Scheduling;
using FluentAssertions;

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
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ScheduledMessageProcessor(_serviceProvider, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ScheduledMessageProcessor(_serviceProvider, _options, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullTimeProvider_Succeeds()
    {
        var act = () => new ScheduledMessageProcessor(_serviceProvider, _options, _logger, timeProvider: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        var act = () => new ScheduledMessageProcessor(_serviceProvider, _options, _logger);
        act.Should().NotThrow();
    }
}
