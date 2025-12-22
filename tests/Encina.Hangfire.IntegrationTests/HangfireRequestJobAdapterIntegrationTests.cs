using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Encina.Hangfire;
using static LanguageExt.Prelude;

namespace Encina.Hangfire.IntegrationTests;

/// <summary>
/// Integration tests for HangfireRequestJobAdapter.
/// Tests end-to-end scenarios with DI container and real mediator.
/// </summary>
[Trait("Category", "Integration")]
public sealed class HangfireRequestJobAdapterIntegrationTests
{
    [Fact]
    public async Task Integration_ValidRequest_ShouldExecuteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();

        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(mediator, logger);
        var request = new TestRequest("integration-test");

        // Act
        var result = await adapter.ExecuteAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Left: _ => throw new InvalidOperationException("Expected Right"),
            Right: value => value.ShouldBe("Processed: integration-test"));
    }

    [Fact]
    public async Task Integration_ErrorFromHandler_ShouldReturnLeft()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, ErrorRequestHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();

        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(mediator, logger);
        var request = new TestRequest("error-test");

        // Act
        var result = await adapter.ExecuteAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Left: error => error.Message.ShouldBe("Handler error"),
            Right: _ => throw new InvalidOperationException("Expected Left"));
    }

    [Fact]
    public async Task Integration_CancellationToken_ShouldPropagate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, CancellableRequestHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();

        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(mediator, logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await adapter.ExecuteAsync(new TestRequest("cancel-test"), cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }
}

// Test handlers
public sealed record TestRequest(string Data) : IRequest<string>;

public sealed class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<MediatorError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<MediatorError, string>($"Processed: {request.Data}"));
    }
}

public sealed class ErrorRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<MediatorError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Left<MediatorError, string>(
            MediatorErrors.Create("handler.error", "Handler error")));
    }
}

public sealed class CancellableRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<MediatorError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Left<MediatorError, string>(
                MediatorErrors.Create("operation.cancelled", "Operation was cancelled")));
        }

        return Task.FromResult(Right<MediatorError, string>($"Processed: {request.Data}"));
    }
}
