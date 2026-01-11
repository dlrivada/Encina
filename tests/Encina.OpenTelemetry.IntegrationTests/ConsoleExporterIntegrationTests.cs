using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

namespace Encina.OpenTelemetry.Tests.Integration;

/// <summary>
/// Integration tests for Encina.OpenTelemetry with Console exporter.
/// </summary>
[Trait("Category", "Integration")]
public class ConsoleExporterIntegrationTests
{
    [Fact]
    public async Task Send_Request_Should_Export_Trace_To_Console()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddEncina(config => { });
        services.AddSingleton<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();

        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("Encina.Tests"))
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestRequest { Value = 42 };

        // Act
        var result = await Encina.Send(request, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: response =>
            {
                response.Result.ShouldBe(84);
                return true;
            },
            Left: error =>
            {
                error.Message.ShouldBeNull($"Expected Right, got Left: {error.Message}");
                return false;
            }
        );
    }

    [Fact]
    public async Task Publish_Notification_Should_Export_Trace_To_Console()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddEncina(config => { });
        services.AddSingleton<INotificationHandler<TestNotification>, TestNotificationHandler>();

        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("Encina.Tests"))
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var notification = new TestNotification { Message = "Hello" };

        // Act
        var result = await Encina.Publish(notification, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
    }

    // Test request and handler
    public record TestRequest : IRequest<TestResponse>
    {
        public int Value { get; init; }
    }

    public record TestResponse
    {
        public int Result { get; init; }
    }

    public class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public async Task<Either<EncinaError, TestResponse>> Handle(
            TestRequest request,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            var response = new TestResponse { Result = request.Value * 2 };
            return response;
        }
    }

    // Test notification and handler
    public record TestNotification : INotification
    {
        public string Message { get; init; } = string.Empty;
    }

    public class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public async Task<Either<EncinaError, Unit>> Handle(
            TestNotification notification,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return Unit.Default;
        }
    }
}
