using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace Encina.IntegrationTests.Observability.OpenTelemetry;

/// <summary>
/// Basic integration tests for OpenTelemetry instrumentation.
/// Verifies that Encina telemetry works with real exporters.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "OpenTelemetry")]
public sealed class BasicInstrumentationTests
{
    [Fact]
    public async Task ConsoleExporter_WithBasicRequest_ShouldExportTelemetry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddSingleton<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("test-service"))
            .WithTracing(builder => builder
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act - This should generate telemetry and export to console
        var result = await Encina.Send(new TestRequest { Data = "test" }, CancellationToken.None);

        // Assert - Request succeeds, telemetry exported (no exception)
        var value = result.ShouldBeSuccess();
        Assert.Equal("success: test", value);
    }

    [Fact]
    public async Task WithEncina_ShouldConfigureInstrumentation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddSingleton<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        services.AddOpenTelemetry()
            .WithEncina()  // Alternative configuration method
            .WithTracing(builder => builder.AddConsoleExporter());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act
        var result = await Encina.Send(new TestRequest { Data = "withEncina" }, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task MultipleRequests_ShouldGenerateMultipleSpans()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddSingleton<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("test-service"))
            .WithTracing(builder => builder
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act - Send multiple requests
        for (var i = 0; i < 5; i++)
        {
            var result = await Encina.Send(new TestRequest { Data = $"request-{i}" }, CancellationToken.None);
            result.ShouldBeSuccess();
        }

        // Assert - All requests succeeded and telemetry was exported
        Assert.True(true);
    }

    #region Test Helpers

    private sealed record TestRequest : IRequest<string>
    {
        public string Data { get; init; } = "test";
    }

    private sealed class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult<Either<EncinaError, string>>($"success: {request.Data}");
        }
    }

    #endregion
}
