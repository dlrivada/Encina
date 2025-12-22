using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Encina.OpenTelemetry;
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
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new TestRequest { Value = 42 };

        // Act
        var result = await mediator.Send(request, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: response =>
            {
                response.Result.Should().Be(84);
                return true;
            },
            Left: error =>
            {
                Assert.Fail($"Expected Right, got Left: {error.Message}");
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
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var notification = new TestNotification { Message = "Hello" };

        // Act
        var result = await mediator.Publish(notification, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
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
        public async Task<Either<MediatorError, TestResponse>> Handle(
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
        public async Task<Either<MediatorError, Unit>> Handle(
            TestNotification notification,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return Unit.Default;
        }
    }
}
