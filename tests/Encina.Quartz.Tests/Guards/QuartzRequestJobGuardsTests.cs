using Microsoft.Extensions.Logging;
using Quartz;
using Encina.Quartz;

namespace Encina.Quartz.Tests.Guards;

/// <summary>
/// Guard clause tests for QuartzRequestJob.
/// Verifies null parameter handling and defensive programming.
/// </summary>
public class QuartzRequestJobGuardsTests
{
    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator mediator = null!;
        var logger = Substitute.For<ILogger<QuartzRequestJob<QuartzTestRequest, QuartzTestResponse>>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new QuartzRequestJob<QuartzTestRequest, QuartzTestResponse>(mediator, logger));

        exception.ParamName.Should().Be("mediator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        ILogger<QuartzRequestJob<QuartzTestRequest, QuartzTestResponse>> logger = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new QuartzRequestJob<QuartzTestRequest, QuartzTestResponse>(mediator, logger));

        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task Execute_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzRequestJob<QuartzTestRequest, QuartzTestResponse>>>();
        var job = new QuartzRequestJob<QuartzTestRequest, QuartzTestResponse>(mediator, logger);
        IJobExecutionContext context = null!;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            job.Execute(context));

        exception.ParamName.Should().Be("context");
    }

}

// Test types
public sealed record QuartzTestRequest(string Data) : IRequest<QuartzTestResponse>;
public sealed record QuartzTestResponse(string Result);
