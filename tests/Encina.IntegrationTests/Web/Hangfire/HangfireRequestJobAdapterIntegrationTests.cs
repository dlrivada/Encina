using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Web.Hangfire;

/// <summary>
/// Integration tests for HangfireRequestJobAdapter.
/// Tests end-to-end scenarios with DI container and real Encina.
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
        var Encina = provider.GetRequiredService<IEncina>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();

        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(Encina, logger);
        var request = new TestRequest("integration-test");

        // Act
        var result = await adapter.ExecuteAsync(request);

        // Assert
        result.ShouldBe("Processed: integration-test");
    }

    [Fact]
    public async Task Integration_ErrorFromHandler_ShouldThrowEncinaJobFailedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, ErrorRequestHandler>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();

        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(Encina, logger);
        var request = new TestRequest("error-test");

        // Act
        var exception = await Should.ThrowAsync<EncinaJobFailedException>(() =>
            adapter.ExecuteAsync(request));

        // Assert
        // Hangfire only marks a job Failed (and retries it) when the job method throws. The
        // exception carries the error code, never the error message (Hangfire persists it).
        exception.ErrorCode.ShouldBe("handler.error");
        exception.Message.ShouldNotContain("Handler error");
    }

    [Fact]
    public async Task Integration_ValidationFailureFromHandler_ShouldThrowEncinaJobPermanentFailureException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, ValidationFailingRequestHandler>();

        var provider = services.BuildServiceProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();

        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(encina, logger);

        // Act
        var exception = await Should.ThrowAsync<EncinaJobPermanentFailureException>(() =>
            adapter.ExecuteAsync(new TestRequest("invalid")));

        // Assert
        exception.ErrorCode.ShouldBe("order.validation_failed");
    }

    [Fact]
    public async Task Integration_CancellationToken_ShouldPropagate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, CancellableRequestHandler>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();

        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(Encina, logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // The handler observes the cancelled token, the dispatcher turns it into an
        // encina.request.cancelled Left, and the adapter rethrows it as OperationCanceledException
        // so Hangfire treats the job as interrupted (e.g. server shutdown), not as failed.
        var exception = await Should.ThrowAsync<OperationCanceledException>(() =>
            adapter.ExecuteAsync(new TestRequest("cancel-test"), cts.Token));
        exception.CancellationToken.ShouldBe(cts.Token);
    }
}

// Test handlers
public sealed record TestRequest(string Data) : IRequest<string>;

public sealed class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<EncinaError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<EncinaError, string>($"Processed: {request.Data}"));
    }
}

public sealed class ErrorRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<EncinaError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Left<EncinaError, string>(
            EncinaErrors.Create("handler.error", "Handler error")));
    }
}

public sealed class CancellableRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<EncinaError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Right<EncinaError, string>($"Processed: {request.Data}"));
    }
}

public sealed class ValidationFailingRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<EncinaError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Left<EncinaError, string>(
            EncinaErrors.Create("order.validation_failed", $"Order '{request.Data}' is invalid")));
    }
}
