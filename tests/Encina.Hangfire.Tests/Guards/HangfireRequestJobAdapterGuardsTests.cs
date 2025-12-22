using Encina.Hangfire;
using Microsoft.Extensions.Logging;

namespace Encina.Hangfire.Tests.Guards;

/// <summary>
/// Guard clause tests for HangfireRequestJobAdapter.
/// Verifies null parameter handling and defensive programming.
/// </summary>
public class HangfireRequestJobAdapterGuardsTests
{
    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator mediator = null!;
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<HangfireTestRequest, HangfireTestResponse>>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HangfireRequestJobAdapter<HangfireTestRequest, HangfireTestResponse>(mediator, logger));

        exception.ParamName.Should().Be("mediator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        ILogger<HangfireRequestJobAdapter<HangfireTestRequest, HangfireTestResponse>> logger = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HangfireRequestJobAdapter<HangfireTestRequest, HangfireTestResponse>(mediator, logger));

        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<HangfireTestRequest, HangfireTestResponse>>>();
        var adapter = new HangfireRequestJobAdapter<HangfireTestRequest, HangfireTestResponse>(mediator, logger);
        HangfireTestRequest request = null!;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            adapter.ExecuteAsync(request));

        exception.ParamName.Should().Be("request");
    }

}

// Test types
public sealed record HangfireTestRequest(string Data) : IRequest<HangfireTestResponse>;
public sealed record HangfireTestResponse(string Result);
