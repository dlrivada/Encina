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
        var value = result.ShouldBeSuccess();
        value.ShouldBe("Processed: integration-test");
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
        // Hangfire only marks a job Failed (and retries it) when the job method throws.
        exception.Message.ShouldContain("Handler error");
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
        // The handler reports cancellation as a domain-level Left, which now surfaces as a
        // thrown EncinaJobFailedException so Hangfire marks the job Failed.
        await Should.ThrowAsync<EncinaJobFailedException>(() =>
            adapter.ExecuteAsync(new TestRequest("cancel-test"), cts.Token));
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
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Left<EncinaError, string>(
                EncinaErrors.Create("operation.cancelled", "Operation was cancelled")));
        }

        return Task.FromResult(Right<EncinaError, string>($"Processed: {request.Data}"));
    }
}
